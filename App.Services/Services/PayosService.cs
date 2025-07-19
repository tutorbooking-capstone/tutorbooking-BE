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
            ? "https://api-merchant.payos.vn" 
            : "https://sandbox-api-merchant.payos.vn";
        
        public PayosService(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<PayosService> logger)
        {
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient("PayOS");
            _logger = logger;
            
            // Tăng timeout cho HttpClient để xử lý kết nối chậm
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }
        
        public async Task<PayosPaymentResponse> CreatePaymentRequestAsync(PayosPaymentRequest request)
        {
            try
            {
                ValidateConfiguration();
                _logger.LogInformation("Creating payment request for order {OrderCode}, amount {Amount}", 
                    request.OrderCode, request.Amount);
                
                // Tạo mã đơn hàng số nguyên từ UUID
                long numericOrderCode = Math.Abs(request.OrderCode.GetHashCode()) % 1000000000;
                
                // Chuẩn bị dữ liệu cho API PayOS
                var requestData = new
                {
                    orderCode = numericOrderCode, // Chuyển đổi thành số nguyên
                    amount = (int)request.Amount, // PayOS yêu cầu số tiền nguyên
                    description = request.Description,
                    cancelUrl = request.CancelUrl ?? request.ReturnUrl,
                    returnUrl = request.ReturnUrl,
                    signature = GenerateSignature(numericOrderCode.ToString(), request.Amount, request.ReturnUrl, request.CancelUrl ?? request.ReturnUrl, request.Description)
                };
                
                _logger.LogInformation("PayOS request data: {RequestData}", System.Text.Json.JsonSerializer.Serialize(requestData));
                
                // Thiết lập headers
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("x-client-id", ClientId);
                _httpClient.DefaultRequestHeaders.Add("x-api-key", ApiKey);
                
                _logger.LogInformation("Calling PayOS API: {Url}", $"{BaseApiUrl}/v2/payment-requests");
                
                // Gọi API PayOS
                var response = await _httpClient.PostAsJsonAsync($"{BaseApiUrl}/v2/payment-requests", requestData);
                
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("PayOS API response: {StatusCode}, Content: {Content}", 
                    response.StatusCode, responseContent);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("PayOS API error: {StatusCode}, {ErrorContent}", 
                        response.StatusCode, responseContent);
                    throw new ErrorException(
                        StatusCodes.Status502BadGateway,
                        ErrorCode.ServerError,
                        $"Lỗi khi gọi API PayOS: {response.StatusCode}");
                }
                
                var responseData = System.Text.Json.JsonSerializer.Deserialize<PayosApiResponse>(responseContent);
                
                if (responseData?.Code != "00")
                {
                    _logger.LogError("PayOS API returned error: {ErrorCode} - {ErrorMessage}", 
                        responseData?.Code, responseData?.Message);
                    throw new ErrorException(
                        StatusCodes.Status502BadGateway,
                        ErrorCode.ServerError,
                        $"Lỗi từ PayOS: {responseData?.Message}");
                }
                
                _logger.LogInformation("PayOS payment request created successfully: {OrderUrl}", 
                    responseData.Data.CheckoutUrl);
                
                return new PayosPaymentResponse
                {
                    OrderUrl = responseData.Data.CheckoutUrl,
                    OrderToken = responseData.Data.PaymentLinkId,
                    QrCode = responseData.Data.QrCode,
                    NumericOrderCode = numericOrderCode
                };
            }
            catch (CoreException)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Lỗi kết nối khi gọi API PayOS: {Message}", ex.Message);
                throw new ErrorException(
                    StatusCodes.Status503ServiceUnavailable,
                    ErrorCode.ServerError,
                    $"Không thể kết nối đến PayOS: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi không xác định khi tạo yêu cầu thanh toán PayOS");
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
                _logger.LogInformation("Verifying PayOS callback with params: {Params}", 
                    string.Join(", ", callbackParams.Select(kv => $"{kv.Key}={kv.Value}")));
                
                if (!callbackParams.TryGetValue("signature", out var signature) || string.IsNullOrEmpty(signature))
                {
                    _logger.LogWarning("PayOS callback missing signature");
                    return Task.FromResult(false);
                }

                // Xử lý dữ liệu để tính toán chữ ký
                Dictionary<string, string> paramsToVerify = new Dictionary<string, string>();
                foreach (var kv in callbackParams)
                {
                    if (kv.Key != "signature")
                    {
                        paramsToVerify[kv.Key] = kv.Value;
                    }
                }
                
                // Log giá trị ChecksumKey (chỉ 4 ký tự đầu để bảo mật)
                string maskedKey = !string.IsNullOrEmpty(ChecksumKey) && ChecksumKey.Length > 4 
                    ? ChecksumKey.Substring(0, 4) + "..." 
                    : "null or empty";
                _logger.LogInformation("Using ChecksumKey starting with: {MaskedKey}, Environment: {Environment}", 
                    maskedKey, Environment);
                    
                // Tạo chuỗi dữ liệu để kiểm tra chữ ký theo thứ tự alphabet của key
                var dataToVerify = string.Join("", paramsToVerify
                    .OrderBy(kv => kv.Key)
                    .Select(kv => $"{kv.Key}{kv.Value}"));
                
                _logger.LogDebug("Data string for signature verification: {DataToVerify}", dataToVerify);
                
                // Tính HMAC-SHA256
                var calculatedSignature = CalculateHMACSHA256(dataToVerify, ChecksumKey);
                _logger.LogDebug("Calculated signature: {CalculatedSignature}", calculatedSignature);
                _logger.LogDebug("Received signature: {ReceivedSignature}", signature);
                
                // So sánh chữ ký
                var isValid = string.Equals(calculatedSignature, signature, StringComparison.OrdinalIgnoreCase);
                _logger.LogInformation("Signature verification result: {IsValid}", isValid);
                
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
                _logger.LogInformation("Checking order status for {OrderCode}", orderCode);
                
                // Thiết lập headers
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("x-client-id", ClientId);
                _httpClient.DefaultRequestHeaders.Add("x-api-key", ApiKey);
                
                string requestUrl = $"{BaseApiUrl}/v2/payment-requests/{orderCode}";
                _logger.LogInformation("Calling PayOS API: {Url}", requestUrl);
                
                // Gọi API PayOS
                var response = await _httpClient.GetAsync(requestUrl);
                
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("PayOS API response: {StatusCode}, Content: {Content}", 
                    response.StatusCode, responseContent);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("PayOS API error: {StatusCode}, {ErrorContent}", 
                        response.StatusCode, responseContent);
                    throw new ErrorException(
                        StatusCodes.Status502BadGateway,
                        ErrorCode.ServerError,
                        $"Lỗi khi gọi API PayOS: {response.StatusCode}");
                }
                
                var responseData = System.Text.Json.JsonSerializer.Deserialize<PayosApiResponse>(responseContent);
                
                if (responseData?.Code != "00")
                {
                    _logger.LogError("PayOS API returned error: {ErrorCode} - {ErrorMessage}", 
                        responseData?.Code, responseData?.Message);
                    throw new ErrorException(
                        StatusCodes.Status502BadGateway,
                        ErrorCode.ServerError,
                        $"Lỗi từ PayOS: {responseData?.Message}");
                }
                
                _logger.LogInformation("PayOS order status check successful: {Status}", responseData.Data.Status);
                
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
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Lỗi kết nối khi kiểm tra trạng thái đơn hàng PayOS: {Message}", ex.Message);
                throw new ErrorException(
                    StatusCodes.Status503ServiceUnavailable,
                    ErrorCode.ServerError,
                    $"Không thể kết nối đến PayOS: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi không xác định khi kiểm tra trạng thái đơn hàng PayOS");
                throw new ErrorException(
                    StatusCodes.Status500InternalServerError,
                    ErrorCode.NotFound,
                    "Lỗi không xác định khi kiểm tra trạng thái đơn hàng");
            }
        }
        
        private void ValidateConfiguration()
        {
            if (string.IsNullOrEmpty(ClientId))
                throw new ErrorException(StatusCodes.Status500InternalServerError, ErrorCode.ServerError, "Thiếu cấu hình PayOS:ClientId");
                
            if (string.IsNullOrEmpty(ApiKey))
                throw new ErrorException(StatusCodes.Status500InternalServerError, ErrorCode.ServerError, "Thiếu cấu hình PayOS:ApiKey");
                
            if (string.IsNullOrEmpty(ChecksumKey))
                throw new ErrorException(StatusCodes.Status500InternalServerError, ErrorCode.ServerError, "Thiếu cấu hình PayOS:ChecksumKey");
        }
        
        private string GenerateSignature(string orderCode, decimal amount, string returnUrl, string cancelUrl, string description)
        {
            // Tạo chuỗi dữ liệu để tính chữ ký theo đúng định dạng của PayOS
            var dataToSign = $"amount={(int)amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}";
            _logger.LogDebug("Generating signature for data: {Data}", dataToSign);
            
            // Tính HMAC-SHA256
            var signature = CalculateHMACSHA256(dataToSign, ChecksumKey);
            _logger.LogDebug("Generated signature: {Signature}", signature);
            
            return signature;
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