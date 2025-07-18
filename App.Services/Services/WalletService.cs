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


    }
}