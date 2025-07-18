using App.Core.Base;
using App.Core.Constants;
using App.Core.Provider;
using App.DTOs.PaymentDTOs;
using App.Repositories.Models;
using App.Repositories.Models.User;
using App.Repositories.UoW;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Services.Services
{
    public class DepositService : IDepositService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserProvider _currentUserProvider;
        private readonly IPayosService _payosService;
        private readonly IWalletService _walletService;
        private readonly ILogger<DepositService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DepositService(
            IUnitOfWork unitOfWork,
            ICurrentUserProvider currentUserProvider,
            IPayosService payosService,
            IWalletService walletService,
            ILogger<DepositService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _currentUserProvider = currentUserProvider;
            _payosService = payosService;
            _walletService = walletService;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<DepositRequestResponse> CreateDepositRequestAsync(decimal amount)
        {
            // Validate amount
            if (amount <= 0)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Số tiền nạp phải lớn hơn 0");

            // Get current user
            var userId = _currentUserProvider.GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                throw new ErrorException(
                    StatusCodes.Status401Unauthorized,
                    ErrorCode.Unauthorized,
                    "Không thể xác định người dùng hiện tại");

            // Ensure user has a wallet
            await _walletService.CreateWalletIfNotExistsAsync(userId);

            // Create deposit request
            var depositRequest = new DepositRequest
            {
                UserId = userId,
                Amount = amount,
                PaymentGateway = "PayOS",
                Status = DepositRequestStatus.Pending
            };

            depositRequest.TrackCreate(userId);
            _unitOfWork.GetRepository<DepositRequest>().Insert(depositRequest);
            await _unitOfWork.SaveAsync();

            // Return response
            return await MapToDepositRequestResponseAsync(depositRequest);
        }

        public async Task<DepositRequestResponse> GetDepositRequestByIdAsync(string requestId)
        {
            var userId = _currentUserProvider.GetCurrentUserId();
            
            var depositRequest = await _unitOfWork.GetRepository<DepositRequest>()
                .ExistEntities()
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == requestId);

            if (depositRequest == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Không tìm thấy yêu cầu nạp tiền");

            // Only allow users to see their own deposit requests unless they're admin
            if (depositRequest.UserId != userId && !_currentUserProvider.IsInRole(Role.Admin.ToStringRole()) && !_currentUserProvider.IsInRole(Role.Staff.ToStringRole()))
                throw new ErrorException(
                    StatusCodes.Status403Forbidden,
                    ErrorCode.Forbidden,
                    "Bạn không có quyền xem yêu cầu nạp tiền này");

            return await MapToDepositRequestResponseAsync(depositRequest);
        }

        public async Task<PayosPaymentResponse> GeneratePayosPaymentAsync(string requestId)
        {
            var depositRequest = await _unitOfWork.GetRepository<DepositRequest>()
                .ExistEntities()
                .FirstOrDefaultAsync(d => d.Id == requestId);

            if (depositRequest == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Không tìm thấy yêu cầu nạp tiền");

            if (depositRequest.Status != DepositRequestStatus.Pending)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Yêu cầu nạp tiền này không còn ở trạng thái chờ xử lý");

            // Generate PayOS payment URL
            var request = _httpContextAccessor.HttpContext?.Request;
            var baseUrl = $"{request?.Scheme}://{request?.Host}";
            var returnUrl = $"{baseUrl}/api/deposit/callback"; // URL đầy đủ

            var payosRequest = new PayosPaymentRequest
            {
                OrderCode = depositRequest.Id, // Use deposit request ID as order code
                Amount = depositRequest.Amount,
                Description = $"Nạp {depositRequest.Amount} VND vào ví",
                ReturnUrl = returnUrl,
                CancelUrl = returnUrl // Hoặc một trang hủy giao dịch khác
            };
            var paymentResponse = await _payosService.CreatePaymentRequestAsync(payosRequest);

            // Update deposit request with PayOS information
            depositRequest.PayosOrderUrl = paymentResponse.OrderUrl;
            depositRequest.PayosOrderToken = paymentResponse.OrderToken;
            depositRequest.PayosQrCode = paymentResponse.QrCode;

            await _unitOfWork.SaveAsync();

            return paymentResponse;
        }

        public async Task<bool> ProcessPayosCallbackAsync(Dictionary<string, string> payosParams)
        {
            try
            {
                // Verify callback signature
                var isValid = await _payosService.VerifyCallbackAsync(payosParams);
                if (!isValid)
                {
                    _logger.LogWarning("Invalid PayOS callback signature");
                    return false;
                }

                // Extract order code (deposit request ID)
                if (!payosParams.TryGetValue("orderCode", out var requestId) || string.IsNullOrEmpty(requestId))
                {
                    _logger.LogWarning("PayOS callback missing orderCode");
                    return false;
                }

                // Extract transaction status
                if (!payosParams.TryGetValue("status", out var status) || string.IsNullOrEmpty(status))
                {
                    _logger.LogWarning("PayOS callback missing status");
                    return false;
                }

                // Extract transaction ID
                if (!payosParams.TryGetValue("transactionId", out var transactionId) || string.IsNullOrEmpty(transactionId))
                {
                    _logger.LogWarning("PayOS callback missing transactionId");
                    return false;
                }

                // Get deposit request
                var depositRequest = await _unitOfWork.GetRepository<DepositRequest>()
                    .ExistEntities()
                    .FirstOrDefaultAsync(d => d.Id == requestId);

                if (depositRequest == null)
                {
                    _logger.LogWarning("Deposit request not found: {RequestId}", requestId);
                    return false;
                }

                // Process based on status
                if (status.Equals("PAID", StringComparison.OrdinalIgnoreCase) || 
                    status.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase))
                {
                    // Update deposit request status
                    var updateFields = depositRequest.Complete(transactionId);
                    _unitOfWork.GetRepository<DepositRequest>().UpdateFields(depositRequest, updateFields);

                    // Get user's wallet
                    var wallet = await _unitOfWork.GetRepository<Wallet>()
                        .ExistEntities()
                        .FirstOrDefaultAsync(w => w.UserId == depositRequest.UserId);

                    if (wallet == null)
                    {
                        _logger.LogError("Wallet not found for user: {UserId}", depositRequest.UserId);
                        return false;
                    }

                    // Create transaction record
                    var transaction = Transaction.CreateDepositTransaction(
                        wallet.Id,
                        depositRequest.Amount,
                        depositRequest.Id
                    );

                    _unitOfWork.GetRepository<Transaction>().Insert(transaction);

                    // Update wallet balance
                    var newBalance = wallet.Balance + depositRequest.Amount;
                    var walletUpdateFields = wallet.UpdateBalance(newBalance);
                    _unitOfWork.GetRepository<Wallet>().UpdateFields(wallet, walletUpdateFields);

                    await _unitOfWork.SaveAsync();
                    return true;
                }
                else if (status.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase) || 
                    status.Equals("FAILED", StringComparison.OrdinalIgnoreCase))
                {
                    // Mark deposit as failed
                    var updateFields = depositRequest.MarkAsFailed();
                    _unitOfWork.GetRepository<DepositRequest>().UpdateFields(depositRequest, updateFields);
                    await _unitOfWork.SaveAsync();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PayOS callback");
                return false;
            }
        }

        public async Task<BasePaginatedList<DepositRequestResponse>> GetUserDepositHistoryAsync(int page = 1, int pageSize = 10)
        {
            var userId = _currentUserProvider.GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                throw new ErrorException(
                    StatusCodes.Status401Unauthorized,
                    ErrorCode.Unauthorized,
                    "Không thể xác định người dùng hiện tại");

            return await GetDepositHistoryAsync(userId, page, pageSize);
        }

        public async Task<BasePaginatedList<DepositRequestResponse>> GetAllDepositHistoryAsync(int page = 1, int pageSize = 10)
        {
            if (!_currentUserProvider.IsInRole(Role.Admin.ToStringRole()) && !_currentUserProvider.IsInRole(Role.Staff.ToStringRole()))
                throw new ErrorException(
                    StatusCodes.Status403Forbidden,
                    ErrorCode.Forbidden,
                    "Bạn không có quyền xem tất cả lịch sử nạp tiền");

            return await GetDepositHistoryAsync(null, page, pageSize);
        }

        public async Task<bool> CheckAndUpdateDepositStatusAsync(string requestId)
        {
            var depositRequest = await _unitOfWork.GetRepository<DepositRequest>()
                .ExistEntities()
                .FirstOrDefaultAsync(d => d.Id == requestId);

            if (depositRequest == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Không tìm thấy yêu cầu nạp tiền");

            // Only check pending deposits
            if (depositRequest.Status != DepositRequestStatus.Pending)
                return false;

            try
            {
                // Check payment status with PayOS
                var statusResponse = await _payosService.CheckOrderStatusAsync(depositRequest.Id);

                // Process based on status
                if (statusResponse.Status.Equals("PAID", StringComparison.OrdinalIgnoreCase) || 
                    statusResponse.Status.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase))
                {
                    // Create dictionary for callback processing
                    var callbackParams = new Dictionary<string, string>
                    {
                        { "orderCode", depositRequest.Id },
                        { "status", statusResponse.Status },
                        { "transactionId", statusResponse.TransactionId ?? "MANUAL_CHECK" }
                    };

                    // Process as if it was a callback
                    return await ProcessPayosCallbackAsync(callbackParams);
                }
                else if (statusResponse.Status.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase) || 
                        statusResponse.Status.Equals("FAILED", StringComparison.OrdinalIgnoreCase))
                {
                    // Mark as failed
                    var updateFields = depositRequest.MarkAsFailed();
                    _unitOfWork.GetRepository<DepositRequest>().UpdateFields(depositRequest, updateFields);
                    await _unitOfWork.SaveAsync();
                    return true;
                }

                // No change needed
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking deposit status with PayOS");
                return false;
            }
        }

        #region Private Helpers
        private async Task<BasePaginatedList<DepositRequestResponse>> GetDepositHistoryAsync(string? userId, int page, int pageSize)
        {
            var query = _unitOfWork.GetRepository<DepositRequest>()
                .ExistEntities()
                .Include(d => d.User)
                .AsQueryable();

            // Filter by user if specified
            if (!string.IsNullOrEmpty(userId))
                query = query.Where(d => d.UserId == userId);

            // Order by creation time, newest first
            query = query.OrderByDescending(d => d.CreatedTime);

            // Get total count
            var totalCount = await query.CountAsync();

            // Get paginated results
            var deposits = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Map to DTOs
            var depositResponses = new List<DepositRequestResponse>();
            foreach (var deposit in deposits)
            {
                depositResponses.Add(await MapToDepositRequestResponseAsync(deposit));
            }

            return new BasePaginatedList<DepositRequestResponse>(depositResponses, totalCount, page, pageSize);
        }

        private async Task<DepositRequestResponse> MapToDepositRequestResponseAsync(DepositRequest deposit)
        {
            // Ensure user is loaded
            if (deposit.User == null && !string.IsNullOrEmpty(deposit.UserId))
            {
                deposit.User = await _unitOfWork.GetRepository<App.Repositories.Models.User.AppUser>()
                    .ExistEntities()
                    .FirstOrDefaultAsync(u => u.Id == deposit.UserId);
            }

            return new DepositRequestResponse
            {
                Id = deposit.Id,
                UserId = deposit.UserId,
                Amount = deposit.Amount,
                PaymentGateway = deposit.PaymentGateway,
                Status = deposit.Status,
                CreatedTime = deposit.CreatedTime.DateTime,
                CompletedAt = deposit.CompletedAt,
                PayosOrderUrl = deposit.PayosOrderUrl,
                PayosQrCode = deposit.PayosQrCode,
                UserFullName = deposit.User?.FullName ?? string.Empty
            };
        }
        #endregion
    }
}