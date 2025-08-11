using App.Core.Base;
using App.Core.Constants;
using App.Core.Provider;
using App.DTOs.PaymentDTOs;
using App.Repositories.Models;
using App.Repositories.Models.User;
using App.Repositories.UoW;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace App.Services.Services
{
    public class WalletService : IWalletService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserProvider _currentUserProvider;
        private readonly UserManager<AppUser> _userManager;

        public WalletService(
            IUnitOfWork unitOfWork,
            ICurrentUserProvider currentUserProvider,
            UserManager<AppUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _currentUserProvider = currentUserProvider;
            _userManager = userManager;
        }

        #region Private Helpers
        private string ResolveUserId(string? userId = null)
        {
            if (!string.IsNullOrEmpty(userId))
                return userId;

            var currentUserId = _currentUserProvider.GetCurrentUserId();
            if (!string.IsNullOrEmpty(currentUserId))
                return currentUserId;

            throw new ErrorException(
                StatusCodes.Status401Unauthorized,
                ErrorCode.Unauthorized,
                "Không thể xác định người dùng hiện tại.");
        }

        private string GetCurrentActorId()
            => _currentUserProvider.GetCurrentUserId() ?? "system";
        #endregion
        
        public async Task<WalletResponse> GetWalletAsync(string? userId = null)
        {
            userId = ResolveUserId(userId);

            var wallet = await _unitOfWork.GetRepository<Wallet>()
                .ExistEntities()
                .Include(w => w.User)
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    $"Không tìm thấy ví cho người dùng có ID: {userId}");

            // Calculate available balance
            var availableBalance = await CalculateAvailableBalanceAsync(wallet.Id);

            return WalletResponse.FromEntity(wallet, availableBalance);
        }

        public async Task<WalletResponse> GetSystemWalletAsync()
        {
            var systemWallet = await _unitOfWork.GetRepository<Wallet>()
                .ExistEntities()
                .FirstOrDefaultAsync(w => w.Type == WalletType.System);

            if (systemWallet == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Không tìm thấy ví hệ thống");
            
            // System wallet's available balance is the same as its total balance
            return WalletResponse.FromEntity(systemWallet, systemWallet.Balance);
        }

        public async Task<bool> CreateWalletIfNotExistsAsync(string userId)
        {
            var existingWallet = await _unitOfWork.GetRepository<Wallet>()
                .ExistEntities()
                .AnyAsync(w => w.UserId == userId);

            if (existingWallet)
                return false; // Wallet already exists

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Không tìm thấy người dùng");

            var wallet = new Wallet
            {
                UserId = userId,
                Type = WalletType.Personal,
                Balance = 0,
                Currency = "VND",
                Status = WalletStatus.Active
            };

            wallet.TrackCreate(GetCurrentActorId());
            _unitOfWork.GetRepository<Wallet>().Insert(wallet);
            await _unitOfWork.SaveAsync();

            return true;
        }

        public async Task<bool> CreateWalletForAllUsersAsync()
        {
            // Get all users with Learner or Tutor role who don't have a wallet yet
            var learners = await _userManager.GetUsersInRoleAsync(Role.Learner.ToString());
            var tutors = await _userManager.GetUsersInRoleAsync(Role.Tutor.ToString());
            var userRoles = learners.Concat(tutors).ToList();
            
            var userIds = userRoles.Select(u => u.Id).Distinct().ToList();
            
            // Get users who already have wallets
            var existingWalletUserIds = await _unitOfWork.GetRepository<Wallet>()
                .ExistEntities()
                .Where(w => w.UserId != null)
                .Select(w => w.UserId)
                .ToListAsync();
            
            // Filter out users who already have wallets
            var usersWithoutWallets = userIds.Except(existingWalletUserIds).ToList();
            
            // Create wallets for users who don't have one
            foreach (var userId in usersWithoutWallets)
            {
                var wallet = new Wallet
                {
                    UserId = userId,
                    Type = WalletType.Personal,
                    Balance = 0,
                    Currency = "VND",
                    Status = WalletStatus.Active
                };
                
                wallet.TrackCreate(GetCurrentActorId());
                _unitOfWork.GetRepository<Wallet>().Insert(wallet);
            }
            
            // Create system wallet if it doesn't exist
            var systemWalletExists = await _unitOfWork.GetRepository<Wallet>()
                .ExistEntities()
                .AnyAsync(w => w.Type == WalletType.System);
                
            if (!systemWalletExists)
            {
                var systemWallet = new Wallet
                {
                    UserId = null,
                    Type = WalletType.System,
                    Balance = 0,
                    Currency = "VND",
                    Status = WalletStatus.Active
                };
                
                systemWallet.TrackCreate(GetCurrentActorId());
                _unitOfWork.GetRepository<Wallet>().Insert(systemWallet);
            }
            
            // Tạo ví escrow nếu chưa tồn tại
            var escrowWalletExists = await _unitOfWork.GetRepository<Wallet>()
                .ExistEntities()
                .AnyAsync(w => w.Type == WalletType.Escrow);
                
            if (!escrowWalletExists)
            {
                var escrowWallet = new Wallet
                {
                    UserId = null,
                    Type = WalletType.Escrow,
                    Balance = 0,
                    Currency = "VND",
                    Status = WalletStatus.Active
                };
                
                escrowWallet.TrackCreate(GetCurrentActorId());
                _unitOfWork.GetRepository<Wallet>().Insert(escrowWallet);
            }
            
            await _unitOfWork.SaveAsync();
            return true;
        }

        public async Task<BasePaginatedList<TransactionResponse>> GetTransactionsAsync(string? userId = null, int page = 1, int pageSize = 10)
        {
            userId = ResolveUserId(userId);
            
            // Get the wallet ID for the user
            var wallet = await _unitOfWork.GetRepository<Wallet>()
                .ExistEntities()
                .FirstOrDefaultAsync(w => w.UserId == userId);
                
            if (wallet == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    $"Không tìm thấy ví cho người dùng có ID: {userId}");
            
            // Get transactions for the wallet
            var query = _unitOfWork.GetRepository<Transaction>()
                .ExistEntities()
                .Where(t => t.SourceWalletId == wallet.Id || t.TargetWalletId == wallet.Id)
                .OrderByDescending(t => t.CreatedAt)
                .Include(t => t.SourceWallet)
                .ThenInclude(w => w!.User)
                .Include(t => t.TargetWallet)
                .ThenInclude(w => w!.User);
            
            // Get total count
            var totalCount = await query.CountAsync();
            
            // Get paginated results
            var transactions = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            // Map to DTOs
            var transactionResponses = transactions.Select(t => new TransactionResponse
            {
                Id = t.Id,
                SourceWalletId = t.SourceWalletId,
                TargetWalletId = t.TargetWalletId,
                Amount = t.Amount,
                Type = t.Type,
                Status = t.Status,
                Description = t.Description,
                CreatedAt = t.CreatedAt,
                SourceWalletOwner = t.SourceWallet?.User?.FullName ?? (t.SourceWallet?.Type == WalletType.System ? "Hệ thống" : null),
                TargetWalletOwner = t.TargetWallet?.User?.FullName ?? (t.TargetWallet?.Type == WalletType.System ? "Hệ thống" : null)
            }).ToList();
            
            return new BasePaginatedList<TransactionResponse>(transactionResponses, totalCount, page, pageSize);
        }

        public async Task<decimal> CalculateAvailableBalanceAsync(string walletId)
        {
            var wallet = await _unitOfWork.GetRepository<Wallet>()
                .ExistEntities()
                .FirstOrDefaultAsync(w => w.Id == walletId);
                
            if (wallet == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Không tìm thấy ví");
            
            // Calculate pending withdrawal amount
            var pendingWithdrawals = await _unitOfWork.GetRepository<WithdrawalRequest>()
                .ExistEntities()
                .Where(w => w.UserId == wallet.UserId && w.Status == WithdrawalRequestStatus.Pending)
                .SumAsync(w => w.GrossAmount);
            
            // Available balance = Total balance - Pending withdrawals
            var availableBalance = wallet.Balance - pendingWithdrawals;
            
            // Ensure available balance is not negative
            return Math.Max(0, availableBalance);
        }

        public async Task<Wallet> GetEscrowWalletAsync()
        {
            var escrowWallet = await _unitOfWork.GetRepository<Wallet>()
                .ExistEntities()
                .FirstOrDefaultAsync(w => w.Type == WalletType.Escrow);
                
            if (escrowWallet == null)
                throw new ErrorException(
                    StatusCodes.Status500InternalServerError, 
                    ErrorCode.ServerError, 
                    "Không tìm thấy ví escrow");
                    
            return escrowWallet;
        }


        public async Task RefundHeldFundToLearnerAsync(string heldFundId)
        {
            // Get the held fund
            var heldFund = await _unitOfWork.GetRepository<HeldFund>()
                .ExistEntities()
                .Include(h => h.BookedSlot)
                .ThenInclude(bs => bs!.Booking)
                .ThenInclude(b => b!.Learner)
                .ThenInclude(l => l!.User)
                .FirstOrDefaultAsync(h => h.Id == heldFundId);

            if (heldFund == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Không tìm thấy khoản tiền tạm giữ");

            // Verify fund is in held or disputed status
            if (heldFund.Status != HeldFundStatus.Held && heldFund.Status != HeldFundStatus.Disputed)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Khoản tiền tạm giữ không ở trạng thái có thể hoàn tiền");

            // Get learner wallet
            var learnerId = heldFund.BookedSlot?.Booking?.LearnerId;
            if (string.IsNullOrEmpty(learnerId))
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Không tìm thấy thông tin học viên để hoàn tiền");

            var learnerWallet = await _unitOfWork.GetRepository<Wallet>()
                .ExistEntities()
                .FirstOrDefaultAsync(w => w.UserId == learnerId);

            if (learnerWallet == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Không tìm thấy ví của học viên");

            // Get escrow wallet
            var escrowWallet = await GetEscrowWalletAsync();
            
            // Create transaction for refund
            var transaction = Transaction.CreatePaymentTransaction(
                escrowWallet.Id,                         // Source wallet (escrow)
                learnerWallet.Id,                        // Target wallet (learner)
                heldFund.Amount,                         // Full amount
                heldFund.Id,                             // Reference to held fund
                $"Hoàn tiền từ tranh chấp BookedSlot ID: {heldFund.BookedSlotId}"
            );
            
            _unitOfWork.GetRepository<Transaction>().Insert(transaction);
            
            // Update wallet balances
            var escrowUpdate = escrowWallet.SubtractBalance(heldFund.Amount);
            _unitOfWork.GetRepository<Wallet>().UpdateFields(escrowWallet, escrowUpdate.ToArray());
            
            var learnerUpdate = learnerWallet.AddBalance(heldFund.Amount);
            _unitOfWork.GetRepository<Wallet>().UpdateFields(learnerWallet, learnerUpdate.ToArray());
            
            // Update held fund status
            var heldFundUpdate = heldFund.UpdateStatus(HeldFundStatus.RefundedToLearner);
            _unitOfWork.GetRepository<HeldFund>().UpdateFields(heldFund, heldFundUpdate.ToArray());
            
            await _unitOfWork.SaveAsync();
        }

        public async Task PartialRefundForDisputeAsync(string heldFundId, decimal tutorPercentage, string bookingId)
        {
            // Get the held fund
            var heldFund = await _unitOfWork.GetRepository<HeldFund>()
                .ExistEntities()
                .Include(h => h.BookedSlot)
                .ThenInclude(bs => bs!.Booking)
                .ThenInclude(b => b!.Learner)
                .ThenInclude(l => l!.User)
                .FirstOrDefaultAsync(h => h.Id == heldFundId);

            if (heldFund == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Không tìm thấy khoản tiền tạm giữ");

            // Verify fund is in held or disputed status
            if (heldFund.Status != HeldFundStatus.Held && heldFund.Status != HeldFundStatus.Disputed)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Khoản tiền tạm giữ không ở trạng thái có thể phân chia");

            // Get booking for tutor and learner info
            var booking = await _unitOfWork.GetRepository<Booking>()
                .ExistEntities()
                .Include(b => b.Tutor)
                .Include(b => b.Learner)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Không tìm thấy thông tin đặt chỗ");

            // Get learner wallet
            var learnerWallet = await _unitOfWork.GetRepository<Wallet>()
                .ExistEntities()
                .FirstOrDefaultAsync(w => w.UserId == booking.LearnerId);

            if (learnerWallet == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Không tìm thấy ví của học viên");

            // Get tutor wallet
            var tutorWallet = await _unitOfWork.GetRepository<Wallet>()
                .ExistEntities()
                .FirstOrDefaultAsync(w => w.UserId == booking.TutorId);

            if (tutorWallet == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Không tìm thấy ví của gia sư");

            // Get escrow wallet
            var escrowWallet = await GetEscrowWalletAsync();
            
            // Calculate amounts
            decimal totalAmount = heldFund.Amount;
            decimal tutorAmount = Math.Round(totalAmount * tutorPercentage, 2);
            decimal learnerAmount = totalAmount - tutorAmount;
            
            // Create transactions
            if (tutorAmount > 0)
            {
                var tutorTransaction = Transaction.CreatePaymentTransaction(
                    escrowWallet.Id,                         // Source wallet (escrow)
                    tutorWallet.Id,                          // Target wallet (tutor)
                    tutorAmount,                             // Tutor's percentage
                    heldFund.Id,                             // Reference to held fund
                    $"Thanh toán 5% cho gia sư từ tranh chấp BookedSlot ID: {heldFund.BookedSlotId}"
                );
                
                _unitOfWork.GetRepository<Transaction>().Insert(tutorTransaction);
                
                // Update tutor wallet balance
                var tutorUpdate = tutorWallet.AddBalance(tutorAmount);
                _unitOfWork.GetRepository<Wallet>().UpdateFields(tutorWallet, tutorUpdate.ToArray());
            }
            
            if (learnerAmount > 0)
            {
                var learnerTransaction = Transaction.CreatePaymentTransaction(
                    escrowWallet.Id,                         // Source wallet (escrow)
                    learnerWallet.Id,                        // Target wallet (learner)
                    learnerAmount,                           // Learner's percentage
                    heldFund.Id,                             // Reference to held fund
                    $"Hoàn 95% tiền cho học viên từ tranh chấp BookedSlot ID: {heldFund.BookedSlotId}"
                );
                
                _unitOfWork.GetRepository<Transaction>().Insert(learnerTransaction);
                
                // Update learner wallet balance
                var learnerUpdate = learnerWallet.AddBalance(learnerAmount);
                _unitOfWork.GetRepository<Wallet>().UpdateFields(learnerWallet, learnerUpdate.ToArray());
            }
            
            // Update escrow wallet balance
            var escrowUpdate = escrowWallet.SubtractBalance(totalAmount);
            _unitOfWork.GetRepository<Wallet>().UpdateFields(escrowWallet, escrowUpdate.ToArray());
            
            // Update held fund status
            var heldFundUpdate = heldFund.UpdateStatus(HeldFundStatus.ReleasedToTutor);
            _unitOfWork.GetRepository<HeldFund>().UpdateFields(heldFund, heldFundUpdate.ToArray());
            
            await _unitOfWork.SaveAsync();
        }
    }
}