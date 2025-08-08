using App.Core.Base;
using App.Core.Constants;
using App.Core.Provider;
using App.DTOs.PaymentDTOs;
using App.Repositories.Models;
using App.Repositories.Models.Payment;
using App.Repositories.Models.User;
using App.Repositories.UoW;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace App.Services.Services
{
    public class WithdrawalService : IWithdrawalService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserProvider _currentUserProvider;
        private readonly IFeeService _feeService;
        private readonly IWalletService _walletService;
        private readonly ILogger<WithdrawalService> _logger;
        private readonly INotificationService _notificationService;

        public WithdrawalService(
            IUnitOfWork unitOfWork,
            ICurrentUserProvider currentUserProvider,
            IFeeService feeService,
            IWalletService walletService,
            ILogger<WithdrawalService> logger,
            INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _currentUserProvider = currentUserProvider;
            _feeService = feeService;
            _walletService = walletService;
            _logger = logger;
            _notificationService = notificationService;
        }

        #region Private Helpers
        private string GetCurrentUserId()
        {
            var userId = _currentUserProvider.GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                throw new ErrorException(
                    StatusCodes.Status401Unauthorized,
                    ErrorCode.Unauthorized,
                    "Không thể xác định người dùng hiện tại");
            return userId;
        }

        private async Task<bool> ValidateBankAccountOwnershipAsync(string bankAccountId, string userId)
        {
            var bankAccount = await _unitOfWork.GetRepository<BankAccount>()
                .ExistEntities()
                .FirstOrDefaultAsync(b => b.Id == bankAccountId && b.UserId == userId);

            return bankAccount != null;
        }
        #endregion

        public async Task<WithdrawalRequestResponse> CreateWithdrawalRequestAsync(CreateWithdrawalRequest request)
        {
            var userId = GetCurrentUserId();

            var bankAccount = await _unitOfWork.GetRepository<BankAccount>()
                .ExistEntities()
                .FirstOrDefaultAsync(b => b.Id == request.BankAccountId && b.UserId == userId);

            if (bankAccount == null)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Tài khoản ngân hàng không hợp lệ hoặc không thuộc về bạn");

            var wallet = await _unitOfWork.GetRepository<Wallet>()
                .ExistEntities()
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Không tìm thấy ví của bạn");

            var availableBalance = await _walletService.CalculateAvailableBalanceAsync(wallet.Id);
            if (availableBalance < request.GrossAmount)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    $"Số dư khả dụng không đủ. Số dư khả dụng: {availableBalance}, Số tiền yêu cầu rút: {request.GrossAmount}");

            var withdrawalFee = await _feeService.CalculateFeeAsync(FeeCodes.WITHDRAWAL_FEE, request.GrossAmount);
            var netAmount = request.GrossAmount - withdrawalFee;

            var feeInfo = new Dictionary<string, decimal> { { "withdrawalFee", withdrawalFee } };
            var bankAccountInfo = new BankAccountInfo
            {
                BankName = bankAccount.BankName,
                AccountNumber = bankAccount.AccountNumber,
                AccountHolderName = bankAccount.AccountHolderName
            };

            var withdrawalRequest = new WithdrawalRequest
            {
                UserId = userId,
                BankAccountId = request.BankAccountId,
                BankAccountInfo = JsonSerializer.Serialize(bankAccountInfo),
                GrossAmount = request.GrossAmount,
                NetAmount = netAmount,
                Fees = JsonSerializer.Serialize(feeInfo),
                Status = WithdrawalRequestStatus.Pending
            };

            withdrawalRequest.TrackCreate(userId);
            _unitOfWork.GetRepository<WithdrawalRequest>().Insert(withdrawalRequest);
            await _unitOfWork.SaveAsync();  

            // Thay đổi từ đây: Tạo HeldFund và chuyển tiền từ ví người dùng
            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                try
                {
                    // Tạo HeldFund
                    var heldFund = HeldFund.CreateForWithdrawal(withdrawalRequest.Id, request.GrossAmount);
                    _unitOfWork.GetRepository<HeldFund>().Insert(heldFund);

                    // Trừ tiền từ ví người dùng
                    var walletUpdateFields = wallet.SubtractBalance(request.GrossAmount);
                    _unitOfWork.GetRepository<Wallet>().UpdateFields(wallet, walletUpdateFields);

                    // Tạo transaction ghi nhận việc chuyển tiền vào HeldFund
                    var transaction = new Transaction
                    {
                        SourceWalletId = wallet.Id,
                        TargetWalletId = null,  
                        Amount = request.GrossAmount,
                        Type = TransactionType.Withdrawal,
                        Status = TransactionStatus.Pending,
                        ReferenceId = withdrawalRequest.Id,
                        Description = $"Giữ tiền cho yêu cầu rút về tài khoản {bankAccount.BankName} - {bankAccount.AccountNumber}"
                    };
                    _unitOfWork.GetRepository<Transaction>().Insert(transaction);

                    await _unitOfWork.SaveAsync();

                    // Lấy thông tin đầy đủ để trả về
                    var completeRequest = await _unitOfWork.GetRepository<WithdrawalRequest>()
                        .ExistEntities()
                        .Include(w => w.User)
                        .FirstOrDefaultAsync(w => w.Id == withdrawalRequest.Id);

                    return WithdrawalRequestResponse.FromEntity(completeRequest!);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi tạo yêu cầu rút tiền và giữ tiền");
                    throw;
                }
            });
        }

        public async Task<BasePaginatedList<WithdrawalRequestResponse>> GetWithdrawalRequestsAsync(
            int page = 1,
            int pageSize = 10,
            WithdrawalRequestStatus? status = null)
        {
            // Check if user is admin/manager/staff
            var userId = GetCurrentUserId();
            var isAdminOrStaff = _currentUserProvider.IsInRole(Role.Admin.ToStringRole()) ||
                                _currentUserProvider.IsInRole(Role.Staff.ToStringRole()) ||
                                _currentUserProvider.IsInRole(Role.Manager.ToStringRole());

            // Build query
            var query = _unitOfWork.GetRepository<WithdrawalRequest>()
                .ExistEntities()
                .Include(w => w.User)
                .Include(w => w.BankAccount)
                .AsQueryable();

            // Filter by user if not admin/staff
            if (!isAdminOrStaff)
                query = query.Where(w => w.UserId == userId);

            // Filter by status if provided
            if (status.HasValue)
                query = query.Where(w => w.Status == status.Value);

            // Get total count
            var totalCount = await query.CountAsync();

            // Get paginated results
            var withdrawals = await query
                .OrderByDescending(w => w.CreatedTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Map to DTOs
            var withdrawalResponses = withdrawals
                .Select(WithdrawalRequestResponse.FromEntity)
                .ToList();

            return new BasePaginatedList<WithdrawalRequestResponse>(withdrawalResponses, totalCount, page, pageSize);
        }

        public async Task<WithdrawalRequestResponse> GetWithdrawalRequestByIdAsync(string requestId)
        {
            var userId = GetCurrentUserId();
            var isAdminOrStaff = _currentUserProvider.IsInRole(Role.Admin.ToStringRole()) ||
                                _currentUserProvider.IsInRole(Role.Staff.ToStringRole()) ||
                                _currentUserProvider.IsInRole(Role.Manager.ToStringRole());

            var withdrawal = await _unitOfWork.GetRepository<WithdrawalRequest>()
                .ExistEntities()
                .Include(w => w.User)
                .Include(w => w.BankAccount)
                .FirstOrDefaultAsync(w => w.Id == requestId && (isAdminOrStaff || w.UserId == userId));

            if (withdrawal == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Không tìm thấy yêu cầu rút tiền hoặc bạn không có quyền xem");

            return WithdrawalRequestResponse.FromEntity(withdrawal);
        }

        public async Task<WithdrawalRequestResponse> ProcessWithdrawalAsync(ProcessWithdrawalRequest request)
        {
            // Verify user is admin/staff/manager
            if (!_currentUserProvider.IsInRole(Role.Admin.ToStringRole()) && 
                !_currentUserProvider.IsInRole(Role.Staff.ToStringRole()) && 
                !_currentUserProvider.IsInRole(Role.Manager.ToStringRole()))
            {
                throw new ErrorException(
                    StatusCodes.Status403Forbidden,
                    ErrorCode.Forbidden,
                    "Bạn không có quyền thực hiện hành động này");
            }

            var currentUserId = GetCurrentUserId();

            var withdrawalRepo = _unitOfWork.GetRepository<WithdrawalRequest>();
            var withdrawal = await withdrawalRepo
                .ExistEntities()
                .Include(w => w.User)
                .FirstOrDefaultAsync(w => w.Id == request.WithdrawalId);

            if (withdrawal == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Không tìm thấy yêu cầu rút tiền");

            var heldFund = await _unitOfWork.GetRepository<HeldFund>()
                .ExistEntities()
                .FirstOrDefaultAsync(h => h.WithdrawalRequestId == withdrawal.Id);

            if (heldFund == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Không tìm thấy thông tin tiền giữ cho yêu cầu rút tiền này");

            var systemWallet = await _unitOfWork.GetRepository<Wallet>()
                .ExistEntities()
                .FirstOrDefaultAsync(w => w.Type == WalletType.System);

            if (systemWallet == null)
                throw new ErrorException(
                    StatusCodes.Status500InternalServerError,
                    ErrorCode.ServerError,
                    "Không tìm thấy ví hệ thống");

            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                try
                {
                    // Đánh dấu withdrawal request đang xử lý
                    var processFields = withdrawal.Process();
                    if (processFields.Any())
                    {
                        withdrawalRepo.UpdateFields(withdrawal, processFields);
                        await _unitOfWork.SaveAsync();
                    }

                    // Xử lý chuyển tiền
                    var feeAmount = withdrawal.GrossAmount - withdrawal.NetAmount;

                    // Cộng tiền phí vào ví hệ thống
                    var systemWalletUpdateFields = systemWallet.AddBalance(feeAmount);
                    _unitOfWork.GetRepository<Wallet>().UpdateFields(systemWallet, systemWalletUpdateFields);

                    // Cập nhật trạng thái HeldFund
                    var heldFundUpdateFields = heldFund.UpdateStatus(HeldFundStatus.ReleasedToTutorBank);
                    _unitOfWork.GetRepository<HeldFund>().UpdateFields(heldFund, heldFundUpdateFields);

                    // Tạo Transaction chuyển phí vào hệ thống
                    var feeTransaction = new Transaction
                    {
                        SourceWalletId = null, // Từ HeldFund 
                        TargetWalletId = systemWallet.Id,
                        Amount = feeAmount,
                        Type = TransactionType.Fee,
                        Status = TransactionStatus.Success,
                        ReferenceId = withdrawal.Id,
                        Description = $"Phí rút tiền cho yêu cầu {withdrawal.Id}"
                    };
                    _unitOfWork.GetRepository<Transaction>().Insert(feeTransaction);

                    // Tạo Transaction cho việc rút tiền ra ngoài
                    var bankInfo = string.IsNullOrEmpty(withdrawal.BankAccountInfo) || withdrawal.BankAccountInfo == "{}" ?
                        $"{withdrawal.BankAccount?.BankName} - {withdrawal.BankAccount?.AccountNumber}" :
                        withdrawal.BankAccountInfo;

                    var withdrawalTransaction = new Transaction
                    {
                        SourceWalletId = null, // Từ HeldFund (không phải wallet)
                        TargetWalletId = null, // Đến ngân hàng bên ngoài
                        Amount = withdrawal.NetAmount,
                        Type = TransactionType.Withdrawal,
                        Status = TransactionStatus.Success,
                        ReferenceId = withdrawal.Id,
                        Description = $"Rút tiền về tài khoản ngân hàng {bankInfo}"
                    };
                    _unitOfWork.GetRepository<Transaction>().Insert(withdrawalTransaction);

                    // Đánh dấu withdrawal request hoàn thành
                    var completeFields = withdrawal.Complete();
                    withdrawalRepo.UpdateFields(withdrawal, completeFields);

                    await _unitOfWork.SaveAsync();

                    _logger.LogInformation(
                        "Đã xử lý thành công yêu cầu rút tiền {WithdrawalId} cho người dùng {UserId}. Số tiền: {Amount}",
                        withdrawal.Id, withdrawal.UserId, withdrawal.GrossAmount);

                    // Send notification to user
                    await _notificationService.SendToUsersAsync(new()
                    {
                        Content = new()
                        {
                            NotificationPriority = Repositories.Models.Notifications.ENotificationPriority.Normal,
                            Title = "PUSH_ON_WITHDRAWAL_REQUEST_APPROVED",
                            Content = "PUSH_ON_WITHDRAWAL_REQUEST_APPROVED_BODY",
                            AdditionalData = JsonSerializer.Serialize(new
                            {
                                WithdrawalId = withdrawal.Id,
                                Amount = withdrawal.GrossAmount
                            }),
                        }, 
                        ReceiverUserIds = [withdrawal.UserId]
                    });

                    return WithdrawalRequestResponse.FromEntity(withdrawal);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi xử lý yêu cầu rút tiền {WithdrawalId}", withdrawal.Id);
                    throw;
                }
            });
        }

        public async Task<WithdrawalRequestResponse> RejectWithdrawalAsync(RejectWithdrawalRequest request)
        {
            // Verify user is admin/staff/manager
            if (!_currentUserProvider.IsInRole(Role.Admin.ToStringRole()) && 
                !_currentUserProvider.IsInRole(Role.Staff.ToStringRole()) && 
                !_currentUserProvider.IsInRole(Role.Manager.ToStringRole()))
            {
                throw new ErrorException(
                    StatusCodes.Status403Forbidden,
                    ErrorCode.Forbidden,
                    "Bạn không có quyền thực hiện hành động này");
            }

            var currentUserId = GetCurrentUserId();

            // Lấy withdrawal request
            var withdrawalRepo = _unitOfWork.GetRepository<WithdrawalRequest>();
            var withdrawal = await withdrawalRepo
                .ExistEntities()
                .Include(w => w.User)
                .FirstOrDefaultAsync(w => w.Id == request.WithdrawalId);

            if (withdrawal == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Không tìm thấy yêu cầu rút tiền");

            // Lấy HeldFund liên quan
            var heldFund = await _unitOfWork.GetRepository<HeldFund>()
                .ExistEntities()
                .FirstOrDefaultAsync(h => h.WithdrawalRequestId == withdrawal.Id);

            if (heldFund == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Không tìm thấy thông tin tiền giữ cho yêu cầu rút tiền này");

            // Lấy ví người dùng
            var userWallet = await _unitOfWork.GetRepository<Wallet>()
                .ExistEntities()
                .FirstOrDefaultAsync(w => w.UserId == withdrawal.UserId);

            if (userWallet == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Không tìm thấy ví người dùng");

            // Xử lý trong transaction
            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                try
                {
                    // Hoàn trả tiền vào ví người dùng
                    var walletUpdateFields = userWallet.AddBalance(withdrawal.GrossAmount);
                    _unitOfWork.GetRepository<Wallet>().UpdateFields(userWallet, walletUpdateFields);

                    // Cập nhật trạng thái HeldFund
                    var heldFundUpdateFields = heldFund.UpdateStatus(HeldFundStatus.RefundedToLearner);
                    _unitOfWork.GetRepository<HeldFund>().UpdateFields(heldFund, heldFundUpdateFields);

                    // Tạo Transaction ghi nhận việc hoàn trả
                    var refundTransaction = new Transaction
                    {
                        SourceWalletId = null, // Từ HeldFund
                        TargetWalletId = userWallet.Id,
                        Amount = withdrawal.GrossAmount,
                        Type = TransactionType.Refund,
                        Status = TransactionStatus.Success,
                        ReferenceId = withdrawal.Id,
                        Description = $"Hoàn tiền cho yêu cầu rút tiền bị từ chối. Lý do: {request.RejectionReason}"
                    };
                    _unitOfWork.GetRepository<Transaction>().Insert(refundTransaction);

                    // Đánh dấu withdrawal request bị từ chối
                    var rejectFields = withdrawal.Reject(request.RejectionReason);
                    if (rejectFields.Any())
                    {
                        withdrawalRepo.UpdateFields(withdrawal, rejectFields);
                    }

                    await _unitOfWork.SaveAsync();

                    _logger.LogInformation(
                        "Đã từ chối yêu cầu rút tiền {WithdrawalId} cho người dùng {UserId}. Lý do: {Reason}",
                        withdrawal.Id, withdrawal.UserId, request.RejectionReason);

                    // Send notification to user
                    await _notificationService.SendToUsersAsync(new()
                    {
                        Content = new()
                        {
                            NotificationPriority = Repositories.Models.Notifications.ENotificationPriority.Normal,
                            Title = "PUSH_ON_WITHDRAWAL_REQUEST_REJECTED",
                            Content = "PUSH_ON_WITHDRAWAL_REQUEST_REJECTED_BODY",
                            AdditionalData = JsonSerializer.Serialize(new
                            {
                                WithdrawalId = withdrawal.Id,
                                Amount = withdrawal.GrossAmount
                            }),
                        },
                        ReceiverUserIds = [withdrawal.UserId]
                    });

                    return WithdrawalRequestResponse.FromEntity(withdrawal);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi từ chối yêu cầu rút tiền {WithdrawalId}", withdrawal.Id);
                    throw;
                }
            });
        }

        public Task<Dictionary<string, object>> GetWithdrawalMetadataAsync()
        {
            var metadata = new Dictionary<string, object>();

            // Add WithdrawalRequestStatus enum values
            var withdrawalStatusValues = Enum.GetValues(typeof(WithdrawalRequestStatus))
                .Cast<WithdrawalRequestStatus>()
                .Select(s => new
                {
                    Name = s.ToString(),
                    Value = (int)s
                })
                .ToList();

            metadata.Add("WithdrawalRequestStatus", withdrawalStatusValues);

            // Add TransactionType enum values
            var transactionTypeValues = Enum.GetValues(typeof(TransactionType))
                .Cast<TransactionType>()
                .Select(t => new
                {
                    Name = t.ToString(),
                    Value = (int)t
                })
                .ToList();
            
            metadata.Add("TransactionType", transactionTypeValues);

            // Add TransactionStatus enum values
            var transactionStatusValues = Enum.GetValues(typeof(TransactionStatus))
                .Cast<TransactionStatus>()
                .Select(ts => new
                {
                    Name = ts.ToString(),
                    Value = (int)ts
                })
                .ToList();
            
            metadata.Add("TransactionStatus", transactionStatusValues);

            return Task.FromResult(metadata);
        }
    }
}