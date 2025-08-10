using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace App.Services.Infras
{
    public interface IOtpService
    {
        Task<string> GenerateOtpAsync(string identifier, string useCase, TimeSpan? customExpiration = null);
        Task<bool> ValidateOtpAsync(string identifier, string useCase, string plainOtp);
        Task<bool> InvalidateOtpAsync(string identifier, string useCase);
        Task<bool> HasValidOtpAsync(string identifier, string useCase);
        int GetActiveOtpCount();
        void ClearExpiredOtps();
    }

    public class OtpService : IOtpService
    {
        private readonly ConcurrentDictionary<string, OtpEntry> _otpStore;
        private readonly ILogger<OtpService> _logger;
        private readonly TimeSpan _defaultExpiration;
        private const int DEFAULT_OTP_LENGTH = 6;

        public OtpService(ILogger<OtpService> logger)
        {
            _otpStore = new ConcurrentDictionary<string, OtpEntry>();
            _logger = logger;
            _defaultExpiration = TimeSpan.FromMinutes(5); // Default 5 minutes expiration
        }

        public Task<string> GenerateOtpAsync(string identifier, string useCase, TimeSpan? customExpiration = null)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                throw new ArgumentException("Identifier cannot be null or empty", nameof(identifier));
            
            if (string.IsNullOrWhiteSpace(useCase))
                throw new ArgumentException("Use case cannot be null or empty", nameof(useCase));

            // Generate OTP
            var plainOtp = GenerateNumericOtp(DEFAULT_OTP_LENGTH);
            var hashedOtp = HashOtp(plainOtp);
            var expiration = customExpiration ?? _defaultExpiration;
            var expiryTime = DateTime.UtcNow.Add(expiration);

            var key = GetOtpKey(identifier, useCase);
            var entry = new OtpEntry
            {
                HashedOtp = hashedOtp,
                ExpiryTime = expiryTime,
                UseCase = useCase,
                CreatedAt = DateTime.UtcNow
            };

            _otpStore.AddOrUpdate(key, entry, (k, v) => entry);

            _logger.LogInformation("OTP generated for identifier: {Identifier}, use case: {UseCase}, expires at: {ExpiryTime}",
                identifier, useCase, expiryTime);

            return Task.FromResult(plainOtp);
        }

        public Task<bool> ValidateOtpAsync(string identifier, string useCase, string plainOtp)
        {
            if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(useCase) || string.IsNullOrWhiteSpace(plainOtp))
                return Task.FromResult(false);

            var key = GetOtpKey(identifier, useCase);
            
            if (!_otpStore.TryGetValue(key, out var entry))
            {
                _logger.LogWarning("OTP validation failed: No OTP found for identifier: {Identifier}, use case: {UseCase}", 
                    identifier, useCase);
                return Task.FromResult(false);
            }

            // Check if expired
            if (DateTime.UtcNow > entry.ExpiryTime)
            {
                _otpStore.TryRemove(key, out _);
                _logger.LogWarning("OTP validation failed: Expired OTP for identifier: {Identifier}, use case: {UseCase}", 
                    identifier, useCase);
                return Task.FromResult(false);
            }

            // Validate OTP
            var hashedInput = HashOtp(plainOtp);
            var isValid = string.Equals(hashedInput, entry.HashedOtp, StringComparison.Ordinal);

            if (isValid)
            {
                // Remove OTP after successful validation (single use)
                _otpStore.TryRemove(key, out _);
                _logger.LogInformation("OTP validated successfully for identifier: {Identifier}, use case: {UseCase}", 
                    identifier, useCase);
            }
            else
            {
                _logger.LogWarning("OTP validation failed: Invalid OTP for identifier: {Identifier}, use case: {UseCase}", 
                    identifier, useCase);
            }

            return Task.FromResult(isValid);
        }

        public Task<bool> InvalidateOtpAsync(string identifier, string useCase)
        {
            var key = GetOtpKey(identifier, useCase);
            var removed = _otpStore.TryRemove(key, out _);
            
            if (removed)
            {
                _logger.LogInformation("OTP invalidated for identifier: {Identifier}, use case: {UseCase}", 
                    identifier, useCase);
            }

            return Task.FromResult(removed);
        }

        public Task<bool> HasValidOtpAsync(string identifier, string useCase)
        {
            var key = GetOtpKey(identifier, useCase);
            
            if (_otpStore.TryGetValue(key, out var entry))
            {
                if (DateTime.UtcNow <= entry.ExpiryTime)
                {
                    return Task.FromResult(true);
                }
                
                // Remove expired entry
                _otpStore.TryRemove(key, out _);
            }

            return Task.FromResult(false);
        }

        public int GetActiveOtpCount()
        {
            ClearExpiredOtps();
            return _otpStore.Count;
        }

        public void ClearExpiredOtps()
        {
            var now = DateTime.UtcNow;
            var expiredKeys = new List<string>();

            foreach (var kvp in _otpStore)
            {
                if (now > kvp.Value.ExpiryTime)
                {
                    expiredKeys.Add(kvp.Key);
                }
            }

            var removedCount = 0;
            foreach (var key in expiredKeys)
            {
                if (_otpStore.TryRemove(key, out _))
                {
                    removedCount++;
                }
            }

            if (removedCount > 0)
            {
                _logger.LogInformation("Cleaned up {RemovedCount} expired OTPs. Active OTPs: {ActiveCount}", 
                    removedCount, _otpStore.Count);
            }
        }

        private string GetOtpKey(string identifier, string useCase)
        {
            return $"{identifier}:{useCase}";
        }

        private string GenerateNumericOtp(int length)
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            var result = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                rng.GetBytes(bytes);
                var randomValue = BitConverter.ToUInt32(bytes, 0);
                result.Append(randomValue % 10);
            }

            return result.ToString();
        }

        private string HashOtp(string otp)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(otp));
            return Convert.ToBase64String(hashedBytes);
        }

        private class OtpEntry
        {
            public string HashedOtp { get; set; } = string.Empty;
            public DateTime ExpiryTime { get; set; }
            public string UseCase { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
        }
    }

    // Background service to clean up expired OTPs periodically
    public class OtpCleanupService : BackgroundService
    {
        private readonly IOtpService _otpService;
        private readonly ILogger<OtpCleanupService> _logger;
        private readonly TimeSpan _cleanupInterval;

        public OtpCleanupService(IOtpService otpService, ILogger<OtpCleanupService> logger)
        {
            _otpService = otpService;
            _logger = logger;
            _cleanupInterval = TimeSpan.FromMinutes(2); // Run cleanup every 2 minutes
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OTP cleanup service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _otpService.ClearExpiredOtps();
                    await Task.Delay(_cleanupInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("OTP cleanup service is stopping");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in OTP cleanup service");
                    // Continue running even if there's an error
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }
        }
    }
}