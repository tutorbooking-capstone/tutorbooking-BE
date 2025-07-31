using App.DTOs.PaymentDTOs;

namespace App.Services.Interfaces
{
    public interface IBankAccountService
    {
        Task<BankAccountResponse> CreateBankAccountAsync(BankAccountRequest request);
        Task<List<BankAccountResponse>> GetUserBankAccountsAsync();
        Task<BankAccountResponse> GetBankAccountByIdAsync(string id);
        Task<BankAccountResponse> UpdateBankAccountAsync(string id, BankAccountRequest request);
        Task DeleteBankAccountAsync(string id);
    }
}