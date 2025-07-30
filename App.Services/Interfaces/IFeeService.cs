using App.Repositories.Models.Payment;

namespace App.Services.Interfaces
{
    public interface IFeeService
    {
        Task<FeeConfig> GetActiveFeeByCodeAsync(string feeCode);
        Task<decimal> CalculateFeeAsync(string feeCode, decimal amount);
        Task<FeeConfig> CreateOrUpdateFeeConfigAsync(string feeCode, decimal value, FeeType type, string description);
        Task<List<FeeConfig>> GetAllActiveFeesAsync();
        Task<Dictionary<string, object>> GetFeeMetadataAsync();
        Task<object> GetFeeInfoByCodeAsync(string feeCode);
    }
}