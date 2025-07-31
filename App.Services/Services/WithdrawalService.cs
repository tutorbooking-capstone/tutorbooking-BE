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

        public WithdrawalService(
            IUnitOfWork unitOfWork,
            ICurrentUserProvider currentUserProvider,
            IFeeService feeService,
            IWalletService walletService,
            ILogger<WithdrawalService> logger)
        {
            _unitOfWork = unitOfWork;
            _currentUserProvider = currentUserProvider;
            _feeService = feeService;
            _walletService = walletService;
            _logger = logger;
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

            // Validate bank account ownership
            var isValidBankAccount = await ValidateBankAccountOwnershipAsync(request.BankAccountId, userId);
            if (!isValidBankAccount)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Tài khoản ngân hàng không hợp lệ hoặc không thuộc về bạn");

            // Get user's wallet
            var wallet = await _unitOfWork.GetRepository<Wallet>()
                .ExistEntities()
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Không tìm thấy ví của bạn");

            // Calculate available balance
            var availableBalance = await _walletService.CalculateAvailableBalanceAsync(wallet.Id);
            if (availableBalance < request.GrossAmount)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    $"Số dư khả dụng không đủ. Số dư khả dụng: {availableBalance}, Số tiền yêu cầu rút: {request.GrossAmount}");

            // Calculate withdrawal fee
            var withdrawalFee = await _feeService.CalculateFeeAsync(FeeCodes.WITHDRAWAL_FEE, request.GrossAmount);
            var netAmount = request.GrossAmount - withdrawalFee;

            // Create fee info JSON
            var feeInfo = new Dictionary<string, decimal>
            {
                { "withdrawalFee", withdrawalFee }
            };

            // Create withdrawal request
            var withdrawalRequest = new WithdrawalRequest
            {
                UserId = userId,
                BankAccountId = request.BankAccountId,
                GrossAmount = request.GrossAmount,
                NetAmount = netAmount,
                Fees = JsonSerializer.Serialize(feeInfo),
                Status = WithdrawalRequestStatus.Pending
            };

            withdrawalRequest.TrackCreate(userId);
            _unitOfWork.GetRepository<WithdrawalRequest>().Insert(withdrawalRequest);
            await _unitOfWork.SaveAsync();

            // Get the complete entity with related data for response
            var completeRequest = await _unitOfWork.GetRepository<WithdrawalRequest>()
                .ExistEntities()
                .Include(w => w.User)
                .Include(w => w.BankAccount)
                .FirstOrDefaultAsync(w => w.Id == withdrawalRequest.Id);

            return WithdrawalRequestResponse.FromEntity(completeRequest!);
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

            // Get withdrawal request
            var withdrawalRepo = _unitOfWork.GetRepository<WithdrawalRequest>();
            var withdrawal = await withdrawalRepo
                .ExistEntities()
                .Include(w => w.User)
                .Include(w => w.BankAccount)
                .FirstOrDefaultAsync(w => w.Id == request.WithdrawalId);

            if (withdrawal == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Không tìm thấy yêu cầu rút tiền");

            // Get user's wallet
            var userWallet = await _unitOfWork.GetRepository<Wallet>()
                .ExistEntities()
                .FirstOrDefaultAsync(w => w.UserId == withdrawal.UserId);

            if (userWallet == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Không tìm thấy ví của người dùng");

            // Get system wallet
            var systemWallet = await _unitOfWork.GetRepository<Wallet>()
                .ExistEntities()
                .FirstOrDefaultAsync(w => w.Type == WalletType.System);

            if (systemWallet == null)
                throw new ErrorException(
                    StatusCodes.Status500InternalServerError,
                    ErrorCode.ServerError,
                    "Không tìm thấy ví hệ thống");

            // Process withdrawal in a transaction
            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                try
                {
                    // First mark the request as processing
                    var processFields = withdrawal.Process();
                    if (processFields.Any())
                    {
                        withdrawalRepo.UpdateFields(withdrawal, processFields);
                        await _unitOfWork.SaveAsync();
                    }

                    // Subtract from user's wallet
                    var userWalletUpdateFields = userWallet.SubtractBalance(withdrawal.GrossAmount);
                    _unitOfWork.GetRepository<Wallet>().UpdateFields(userWallet, userWalletUpdateFields);

                    // Add fee to system wallet
                    var feeAmount = withdrawal.GrossAmount - withdrawal.NetAmount;
                    var systemWalletUpdateFields = systemWallet.AddBalance(feeAmount);
                    _unitOfWork.GetRepository<Wallet>().UpdateFields(systemWallet, systemWalletUpdateFields);

                    // Create transaction records
                    var withdrawalTransaction = new Transaction
                    {
                        SourceWalletId = userWallet.Id,
                        TargetWalletId = null, // External bank account
                        Amount = withdrawal.NetAmount,
                        Type = TransactionType.Withdrawal,
                        Status = TransactionStatus.Success,
                        ReferenceId = withdrawal.Id,
                        Description = $"Rút tiền về tài khoản ngân hàng {withdrawal.BankAccount?.BankName} - {withdrawal.BankAccount?.AccountNumber}"
                    };
                    _unitOfWork.GetRepository<Transaction>().Insert(withdrawalTransaction);

                    var feeTransaction = new Transaction
                    {
                        SourceWalletId = userWallet.Id,
                        TargetWalletId = systemWallet.Id,
                        Amount = feeAmount,
                        Type = TransactionType.Fee,
                        Status = TransactionStatus.Success,
                        ReferenceId = withdrawal.Id,
                        Description = $"Phí rút tiền cho yêu cầu {withdrawal.Id}"
                    };
                    _unitOfWork.GetRepository<Transaction>().Insert(feeTransaction);

                    // Mark withdrawal as completed
                    var completeFields = withdrawal.Complete();
                    withdrawalRepo.UpdateFields(withdrawal, completeFields);

                    await _unitOfWork.SaveAsync();

                    _logger.LogInformation(
                        "Đã xử lý thành công yêu cầu rút tiền {WithdrawalId} cho người dùng {UserId}. Số tiền: {Amount}",
                        withdrawal.Id, withdrawal.UserId, withdrawal.GrossAmount);

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

            // Get withdrawal request
            var withdrawalRepo = _unitOfWork.GetRepository<WithdrawalRequest>();
            var withdrawal = await withdrawalRepo
                .ExistEntities()
                .Include(w => w.User)
                .Include(w => w.BankAccount)
                .FirstOrDefaultAsync(w => w.Id == request.WithdrawalId);

            if (withdrawal == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Không tìm thấy yêu cầu rút tiền");

            // Reject withdrawal
            var rejectFields = withdrawal.Reject(request.RejectionReason);
            if (!rejectFields.Any())
                return WithdrawalRequestResponse.FromEntity(withdrawal);

            withdrawalRepo.UpdateFields(withdrawal, rejectFields);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation(
                "Đã từ chối yêu cầu rút tiền {WithdrawalId} cho người dùng {UserId}. Lý do: {Reason}",
                withdrawal.Id, withdrawal.UserId, request.RejectionReason);

            return WithdrawalRequestResponse.FromEntity(withdrawal);
        }

        public Task<Dictionary<string, object>> GetWithdrawalMetadataAsync()
        {
            var metadata = new Dictionary<string, object>();

            // Add WithdrawalRequestStatus enum values
            var statusValues = Enum.GetValues(typeof(WithdrawalRequestStatus))
                .Cast<WithdrawalRequestStatus>()
                .Select(s => new
                {
                    Name = s.ToString(),
                    Value = (int)s
                })
                .ToList();

            metadata.Add("WithdrawalRequestStatus", statusValues);

            return Task.FromResult(metadata);
        }
    }
}