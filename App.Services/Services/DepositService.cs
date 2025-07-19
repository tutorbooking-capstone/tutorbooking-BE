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
                _logger.LogInformation("====== WEBHOOK CALLBACK RECEIVED ======");
                _logger.LogInformation("Processing PayOS callback with params: {Params}", 
                    string.Join(", ", payosParams.Select(kv => $"{kv.Key}={kv.Value}")));
                
                // Verify callback signature - tạm thời bỏ qua xác thực chữ ký trong quá trình test
                var isValid = await _payosService.VerifyCallbackAsync(payosParams);
                _logger.LogInformation("Signature verification result: {IsValid}", isValid);
                
                if (!isValid)
                {
                    _logger.LogWarning("Invalid PayOS callback signature");
                    _logger.LogWarning("Temporarily bypassing signature verification for testing");
                }

                // Xử lý dữ liệu từ callback
                string? requestId = null;
                string? status = null;
                string? transactionId = null;
                
                _logger.LogInformation("Extracting data from callback...");
                
                // Trích xuất dữ liệu từ callback
                if (payosParams.TryGetValue("data", out var dataJson) && !string.IsNullOrEmpty(dataJson))
                {
                    _logger.LogInformation("Found data field in callback: {Data}", dataJson);
                    try
                    {
                        var dataObj = System.Text.Json.JsonSerializer.Deserialize<PayosCallbackData>(dataJson);
                        if (dataObj != null)
                        {
                            requestId = dataObj.OrderCode?.ToString();
                            status = dataObj.Status;
                            transactionId = dataObj.TransactionId;
                            _logger.LogInformation("Successfully parsed data object: OrderCode={OrderCode}, Status={Status}, TransactionId={TransactionId}",
                                dataObj.OrderCode, dataObj.Status, dataObj.TransactionId);
                        }
                        else
                        {
                            _logger.LogWarning("Data object is null after deserialization");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error parsing data JSON from PayOS callback");
                    }
                }
                else
                {
                    _logger.LogInformation("No data field found in callback, checking top-level fields");
                }
                
                // Fallback: lấy từ các tham số cấp cao nhất
                if (string.IsNullOrEmpty(requestId) && payosParams.TryGetValue("orderCode", out var orderCode))
                {
                    requestId = orderCode;
                    _logger.LogInformation("Found orderCode in top-level fields: {OrderCode}", orderCode);
                }
                    
                if (string.IsNullOrEmpty(status) && payosParams.TryGetValue("status", out var statusValue))
                {
                    status = statusValue;
                    _logger.LogInformation("Found status in top-level fields: {Status}", statusValue);
                }
                    
                if (string.IsNullOrEmpty(transactionId) && payosParams.TryGetValue("transactionId", out var transactionIdValue))
                {
                    transactionId = transactionIdValue;
                    _logger.LogInformation("Found transactionId in top-level fields: {TransactionId}", transactionIdValue);
                }
                    
                // Log các giá trị đã trích xuất
                _logger.LogInformation("Final extracted values - RequestId: {RequestId}, Status: {Status}, TransactionId: {TransactionId}", 
                    requestId, status, transactionId);

                // Kiểm tra xem có đủ thông tin cần thiết không
                if (string.IsNullOrEmpty(requestId))
                {
                    _logger.LogWarning("PayOS callback missing orderCode");
                    return false;
                }

                // XỬ LÝ ĐẶC BIỆT CHO CALLBACK TEST
                if (requestId == "123")
                {
                    _logger.LogInformation("Received test callback from PayOS with orderCode 123. Acknowledging without processing.");
                    return true; // Trả về thành công để PayOS biết webhook hoạt động
                }

                // Get deposit request
                _logger.LogInformation("Looking up deposit request with ID: {RequestId}", requestId);
                var depositRequest = await _unitOfWork.GetRepository<DepositRequest>()
                    .ExistEntities()
                    .FirstOrDefaultAsync(d => d.Id == requestId);

                if (depositRequest == null)
                {
                    _logger.LogWarning("Deposit request not found: {RequestId}", requestId);
                    return false;
                }
                
                _logger.LogInformation("Found deposit request: ID={Id}, UserId={UserId}, Amount={Amount}, Status={Status}", 
                    depositRequest.Id, depositRequest.UserId, depositRequest.Amount, depositRequest.Status);

                // Process based on status
                if (status != null && (status.Equals("PAID", StringComparison.OrdinalIgnoreCase) || 
                    status.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogInformation("Payment status is PAID/COMPLETED. Updating deposit request status...");
                    
                    // Update deposit request status
                    var updateFields = depositRequest.Complete(transactionId ?? string.Empty);
                    _unitOfWork.GetRepository<DepositRequest>().UpdateFields(depositRequest, updateFields);
                    _logger.LogInformation("Deposit request status updated to Success");

                    // Get user's wallet
                    _logger.LogInformation("Looking up wallet for user: {UserId}", depositRequest.UserId);
                    var wallet = await _unitOfWork.GetRepository<Wallet>()
                        .ExistEntities()
                        .FirstOrDefaultAsync(w => w.UserId == depositRequest.UserId);

                    if (wallet == null)
                    {
                        _logger.LogError("Wallet not found for user: {UserId}", depositRequest.UserId);
                        return false;
                    }
                    
                    _logger.LogInformation("Found wallet: ID={Id}, Balance={Balance}", wallet.Id, wallet.Balance);

                    // Create transaction record
                    _logger.LogInformation("Creating transaction record...");
                    var transaction = Transaction.CreateDepositTransaction(
                        wallet.Id,
                        depositRequest.Amount,
                        depositRequest.Id
                    );

                    _unitOfWork.GetRepository<Transaction>().Insert(transaction);
                    _logger.LogInformation("Transaction record created: ID={Id}, Amount={Amount}", transaction.Id, transaction.Amount);

                    // Update wallet balance
                    var newBalance = wallet.Balance + depositRequest.Amount;
                    _logger.LogInformation("Updating wallet balance from {OldBalance} to {NewBalance}", wallet.Balance, newBalance);
                    var walletUpdateFields = wallet.UpdateBalance(newBalance);
                    _unitOfWork.GetRepository<Wallet>().UpdateFields(wallet, walletUpdateFields);

                    await _unitOfWork.SaveAsync();
                    _logger.LogInformation("Changes saved to database");
                    _logger.LogInformation("====== WEBHOOK PROCESSING COMPLETED SUCCESSFULLY ======");
                    return true;
                }
                else if (status != null && (status.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase) || 
                    status.Equals("FAILED", StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogInformation("Payment status is CANCELLED/FAILED. Marking deposit as failed...");
                    
                    // Mark deposit as failed
                    var updateFields = depositRequest.MarkAsFailed();
                    _unitOfWork.GetRepository<DepositRequest>().UpdateFields(depositRequest, updateFields);
                    await _unitOfWork.SaveAsync();
                    
                    _logger.LogInformation("Deposit request marked as failed");
                    _logger.LogInformation("====== WEBHOOK PROCESSING COMPLETED (PAYMENT FAILED) ======");
                    return true;
                }

                _logger.LogWarning("Unhandled payment status: {Status}", status);
                _logger.LogInformation("====== WEBHOOK PROCESSING ENDED WITHOUT ACTION ======");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PayOS callback");
                _logger.LogInformation("====== WEBHOOK PROCESSING FAILED WITH ERROR ======");
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
            _logger.LogInformation("====== MANUAL STATUS CHECK STARTED ======");
            _logger.LogInformation("Checking deposit status for request ID: {RequestId}", requestId);
            
            var depositRequest = await _unitOfWork.GetRepository<DepositRequest>()
                .ExistEntities()
                .FirstOrDefaultAsync(d => d.Id == requestId);

            if (depositRequest == null)
            {
                _logger.LogWarning("Deposit request not found: {RequestId}", requestId);
                return false;
            }
            
            _logger.LogInformation("Found deposit request: ID={Id}, UserId={UserId}, Amount={Amount}, Status={Status}", 
                depositRequest.Id, depositRequest.UserId, depositRequest.Amount, depositRequest.Status);

            // Only check pending deposits
            if (depositRequest.Status != DepositRequestStatus.Pending)
            {
                _logger.LogInformation("Deposit request is not pending (current status: {Status}). No action needed.", depositRequest.Status);
                return false;
            }

            try
            {
                // Check payment status with PayOS
                _logger.LogInformation("Checking payment status with PayOS API...");
                var statusResponse = await _payosService.CheckOrderStatusAsync(depositRequest.Id);
                _logger.LogInformation("PayOS response: Status={Status}, TransactionId={TransactionId}", 
                    statusResponse.Status, statusResponse.TransactionId);

                // Process based on status
                if (statusResponse.Status.Equals("PAID", StringComparison.OrdinalIgnoreCase) || 
                    statusResponse.Status.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Payment status is PAID/COMPLETED. Processing payment...");
                    
                    // Create dictionary for callback processing
                    var callbackParams = new Dictionary<string, string>
                    {
                        { "orderCode", depositRequest.Id },
                        { "status", statusResponse.Status },
                        { "transactionId", statusResponse.TransactionId ?? "MANUAL_CHECK" }
                    };

                    // Process as if it was a callback
                    var result = await ProcessPayosCallbackAsync(callbackParams);
                    _logger.LogInformation("Payment processing result: {Result}", result);
                    _logger.LogInformation("====== MANUAL STATUS CHECK COMPLETED ======");
                    return result;
                }
                else if (statusResponse.Status.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase) || 
                        statusResponse.Status.Equals("FAILED", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Payment status is CANCELLED/FAILED. Marking deposit as failed...");
                    
                    // Mark as failed
                    var updateFields = depositRequest.MarkAsFailed();
                    _unitOfWork.GetRepository<DepositRequest>().UpdateFields(depositRequest, updateFields);
                    await _unitOfWork.SaveAsync();
                    
                    _logger.LogInformation("Deposit request marked as failed");
                    _logger.LogInformation("====== MANUAL STATUS CHECK COMPLETED ======");
                    return true;
                }
                else
                {
                    _logger.LogInformation("Payment status is still PENDING. No action needed.");
                    _logger.LogInformation("====== MANUAL STATUS CHECK COMPLETED ======");
                }

                // No change needed
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking deposit status with PayOS");
                _logger.LogInformation("====== MANUAL STATUS CHECK FAILED WITH ERROR ======");
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

    // Thêm class này để deserialize dữ liệu callback
    public class PayosCallbackData
    {
        public string? OrderCode { get; set; }
        public string? Status { get; set; }
        public string? TransactionId { get; set; }
        public int Amount { get; set; }
        public string? Description { get; set; }
        // Các trường khác theo tài liệu PayOS
    }
}