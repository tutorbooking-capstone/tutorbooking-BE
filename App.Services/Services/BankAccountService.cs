using App.Core.Base;
using App.Core.Constants;
using App.Core.Provider;
using App.DTOs.PaymentDTOs;
using App.Repositories.Models;
using App.Repositories.UoW;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace App.Services.Services
{
    public class BankAccountService : IBankAccountService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserProvider _currentUserProvider;

        public BankAccountService(
            IUnitOfWork unitOfWork,
            ICurrentUserProvider currentUserProvider)
        {
            _unitOfWork = unitOfWork;
            _currentUserProvider = currentUserProvider;
        }

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

        public async Task<BankAccountResponse> CreateBankAccountAsync(BankAccountRequest request)
        {
            var userId = GetCurrentUserId();

            var existingAccount = await _unitOfWork.GetRepository<BankAccount>()
                .ExistEntities()
                .FirstOrDefaultAsync(b => b.UserId == userId && b.AccountNumber == request.AccountNumber);

            if (existingAccount != null)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Số tài khoản này đã được đăng ký");

            var bankAccount = request.ToEntity(userId);
            bankAccount.TrackCreate(userId);
            _unitOfWork.GetRepository<BankAccount>().Insert(bankAccount);
            await _unitOfWork.SaveAsync();

            return BankAccountResponse.FromEntity(bankAccount);
        }

        public async Task<List<BankAccountResponse>> GetUserBankAccountsAsync()
        {
            var userId = GetCurrentUserId();

            var bankAccounts = await _unitOfWork.GetRepository<BankAccount>()
                .ExistEntities()
                .Where(b => b.UserId == userId)
                .ToListAsync();

            return bankAccounts.Select(BankAccountResponse.FromEntity).ToList();
        }

        public async Task<BankAccountResponse> GetBankAccountByIdAsync(string id)
        {
            var userId = GetCurrentUserId();

            var bankAccount = await _unitOfWork.GetRepository<BankAccount>()
                .ExistEntities()
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (bankAccount == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Không tìm thấy tài khoản ngân hàng hoặc bạn không có quyền xem");

            return BankAccountResponse.FromEntity(bankAccount);
        }

        public async Task<BankAccountResponse> UpdateBankAccountAsync(string id, BankAccountRequest request)
        {
            var userId = GetCurrentUserId();
            var bankAccountRepo = _unitOfWork.GetRepository<BankAccount>();

            var bankAccount = await bankAccountRepo
                .ExistEntities()
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (bankAccount == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Không tìm thấy tài khoản ngân hàng hoặc bạn không có quyền cập nhật");

            // Validation logic remains in the service
            if (bankAccount.AccountNumber != request.AccountNumber)
            {
                var existingAccount = await _unitOfWork.GetRepository<BankAccount>()
                    .ExistEntities()
                    .FirstOrDefaultAsync(b => b.UserId == userId &&
                                            b.AccountNumber == request.AccountNumber &&
                                            b.Id != id);

                if (existingAccount != null)
                    throw new ErrorException(
                        StatusCodes.Status400BadRequest,
                        ErrorCode.BadRequest,
                        "Số tài khoản này đã được đăng ký với tài khoản ngân hàng khác");
            }

            // Call the entity's behavior method
            var updatedFields = bankAccount.UpdateDetails(
                request.BankName, 
                request.AccountNumber, 
                request.AccountHolderName, 
                userId);

            if (updatedFields.Any())
            {
                bankAccountRepo.UpdateFields(bankAccount, updatedFields);
                await _unitOfWork.SaveAsync();
            }

            return BankAccountResponse.FromEntity(bankAccount);
        }

        public async Task DeleteBankAccountAsync(string id)
        {
            var userId = GetCurrentUserId();

            var bankAccount = await _unitOfWork.GetRepository<BankAccount>()
                .ExistEntities()
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (bankAccount == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Không tìm thấy tài khoản ngân hàng hoặc bạn không có quyền xóa");

            // Kiểm tra xem tài khoản này có đang được sử dụng trong yêu cầu rút tiền nào không
            var hasWithdrawalRequests = await _unitOfWork.GetRepository<WithdrawalRequest>()
                .ExistEntities()
                .AnyAsync(w => w.BankAccountId == id &&
                                (w.Status == WithdrawalRequestStatus.Pending ||
                                w.Status == WithdrawalRequestStatus.Processing));

            if (hasWithdrawalRequests)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Không thể xóa tài khoản ngân hàng đang được sử dụng trong yêu cầu rút tiền đang xử lý");

            _unitOfWork.GetRepository<BankAccount>().Delete(bankAccount);
            await _unitOfWork.SaveAsync();
        }
    }
}