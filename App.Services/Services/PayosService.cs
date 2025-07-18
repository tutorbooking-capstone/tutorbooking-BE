using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using App.Core.Base;
using App.Core.Constants;
using App.DTOs.PaymentDTOs;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace App.Services.Services
{
    public class PayosService : IPayosService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<PayosService> _logger;
        
        private string ClientId => _configuration["PayOS:ClientId"] ?? string.Empty;
        private string ApiKey => _configuration["PayOS:ApiKey"] ?? string.Empty;
        private string ChecksumKey => _configuration["PayOS:ChecksumKey"] ?? string.Empty;
        private string Environment => _configuration["PayOS:Environment"] ?? "Sandbox";
        private string BaseApiUrl => Environment == "Production" 
            ? "https://api.payos.vn" 
            : "https://api-sandbox.payos.vn";  
        
        public PayosService(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<PayosService> logger)
        {
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient("PayOS");
            _logger = logger;
        }
        
        public async Task<PayosPaymentResponse> CreatePaymentRequestAsync(PayosPaymentRequest request)
        {
            try
            {
                ValidateConfiguration();
                
                // Chuẩn bị dữ liệu cho API PayOS
                var requestData = new
                {
                    orderCode = request.OrderCode,
                    amount = (int)(request.Amount * 100), // PayOS yêu cầu số tiền * 100
                    description = request.Description,
                    returnUrl = request.ReturnUrl,
                    cancelUrl = request.CancelUrl ?? request.ReturnUrl,
                    signature = GenerateSignature(request.OrderCode, request.Amount)
                };
                
                // Thiết lập headers
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("x-client-id", ClientId);
                _httpClient.DefaultRequestHeaders.Add("x-api-key", ApiKey);
                
                // Gọi API PayOS
                var response = await _httpClient.PostAsJsonAsync($"{BaseApiUrl}/v2/payment-requests", requestData);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("PayOS API error: {ErrorContent}", errorContent);
                    throw new ErrorException(
                        StatusCodes.Status502BadGateway,
                        ErrorCode.ServerError,
                        "Lỗi khi gọi API PayOS");
                }
                
                var responseData = await response.Content.ReadFromJsonAsync<PayosApiResponse>();
                
                if (responseData?.Code != "00")
                {
                    _logger.LogError("PayOS API returned error: {ErrorCode} - {ErrorMessage}", responseData?.Code, responseData?.Message);
                    throw new ErrorException(
                        StatusCodes.Status502BadGateway,
                        ErrorCode.ServerError,
                        $"Lỗi từ PayOS: {responseData?.Message}");
                }
                
                return new PayosPaymentResponse
                {
                    OrderUrl = responseData.Data.CheckoutUrl,
                    OrderToken = responseData.Data.PaymentLinkId,
                    QrCode = responseData.Data.QrCode
                };
            }
            catch (CoreException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo yêu cầu thanh toán PayOS");
                throw new ErrorException(
                    StatusCodes.Status500InternalServerError,
                    ErrorCode.ServerError,
                    "Lỗi không xác định khi tạo yêu cầu thanh toán");
            }
        }
        
        public Task<bool> VerifyCallbackAsync(Dictionary<string, string> callbackParams)
        {
            try
            {
                ValidateConfiguration();
                
                if (!callbackParams.TryGetValue("signature", out var signature) || string.IsNullOrEmpty(signature))
                {
                    _logger.LogWarning("PayOS callback missing signature");
                    return Task.FromResult(false);
                }
                
                // Tạo chuỗi dữ liệu để kiểm tra chữ ký
                var dataToVerify = string.Join("", callbackParams
                    .Where(kv => kv.Key != "signature")
                    .OrderBy(kv => kv.Key)
                    .Select(kv => $"{kv.Key}{kv.Value}"));
                
                // Tính HMAC-SHA256
                var calculatedSignature = CalculateHMACSHA256(dataToVerify, ChecksumKey);
                
                // So sánh chữ ký
                var isValid = string.Equals(calculatedSignature, signature, StringComparison.OrdinalIgnoreCase);
                return Task.FromResult(isValid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xác minh callback PayOS");
                return Task.FromResult(false);
            }
        }
        
        public async Task<PayosOrderStatusResponse> CheckOrderStatusAsync(string orderCode)
        {
            try
            {
                ValidateConfiguration();
                
                // Thiết lập headers
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("x-client-id", ClientId);
                _httpClient.DefaultRequestHeaders.Add("x-api-key", ApiKey);
                
                // Gọi API PayOS
                var response = await _httpClient.GetAsync($"{BaseApiUrl}/v2/payment-requests/{orderCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("PayOS API error: {ErrorContent}", errorContent);
                    throw new ErrorException(
                        StatusCodes.Status502BadGateway,
                        ErrorCode.ServerError,
                        "Lỗi khi gọi API PayOS");
                }
                
                var responseData = await response.Content.ReadFromJsonAsync<PayosApiResponse>();
                
                if (responseData?.Code != "00")
                {
                    _logger.LogError("PayOS API returned error: {ErrorCode} - {ErrorMessage}", responseData?.Code, responseData?.Message);
                    throw new ErrorException(
                        StatusCodes.Status502BadGateway,
                        ErrorCode.ServerError,
                        $"Lỗi từ PayOS: {responseData?.Message}");
                }
                
                return new PayosOrderStatusResponse
                {
                    OrderCode = responseData.Data.OrderCode,
                    Status = responseData.Data.Status,
                    Amount = responseData.Data.Amount / 100m, // Chia cho 100 để chuyển về VND
                    TransactionId = responseData.Data.TransactionId,
                    PaymentTime = responseData.Data.PaymentTime
                };
            }
            catch (CoreException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi kiểm tra trạng thái đơn hàng PayOS");
                throw new ErrorException(
                    StatusCodes.Status500InternalServerError,
                    ErrorCode.NotFound,
                    "Lỗi không xác định khi kiểm tra trạng thái đơn hàng");
            }
        }
        
        private void ValidateConfiguration()
        {
            if (string.IsNullOrEmpty(ClientId) || string.IsNullOrEmpty(ApiKey) || string.IsNullOrEmpty(ChecksumKey))
            {
                throw new ErrorException(
                    StatusCodes.Status500InternalServerError,
                    ErrorCode.ServerError,
                    "Thiếu cấu hình PayOS trong appsettings.json");
            }
        }
        
        private string GenerateSignature(string orderCode, decimal amount)
        {
            // Tạo chuỗi dữ liệu để tính chữ ký
            var dataToSign = $"amount={amount * 100}&orderCode={orderCode}";
            
            // Tính HMAC-SHA256
            return CalculateHMACSHA256(dataToSign, ChecksumKey);
        }
        
        private string CalculateHMACSHA256(string data, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var dataBytes = Encoding.UTF8.GetBytes(data);
            
            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(dataBytes);
            
            // Chuyển đổi byte array thành chuỗi hex
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
}