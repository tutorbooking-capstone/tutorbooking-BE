using App.Core.Provider;
using App.Repositories.Models.Payment;
using App.Repositories.UoW;
using App.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace App.Services.Services
{

    public class FeeService : IFeeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserProvider _currentUserProvider;

        public FeeService(
            IUnitOfWork unitOfWork,
            ICurrentUserProvider currentUserProvider)
        {
            _unitOfWork = unitOfWork;
            _currentUserProvider = currentUserProvider;
        }

        private string GetCurrentActorId() => _currentUserProvider.GetCurrentUserId() ?? "system";

        public async Task<FeeConfig> GetActiveFeeByCodeAsync(string feeCode)
        {
            var now = DateTime.UtcNow;
            var result = await _unitOfWork.GetRepository<FeeConfig>()
                .ExistEntities()
                .Where(f => f.FeeCode == feeCode && f.IsActive && f.EffectiveFrom <= now && (!f.EffectiveTo.HasValue || f.EffectiveTo > now))
                .OrderByDescending(f => f.EffectiveFrom)
                .FirstOrDefaultAsync();

            return result 
                ?? throw new Exception($"Fee config with code {feeCode} not found");
        }

        public async Task<decimal> CalculateFeeAsync(string feeCode, decimal amount)
        {
            var feeConfig = await GetActiveFeeByCodeAsync(feeCode);
            if (feeConfig == null)
                return 0;

            return feeConfig.CalculateFee(amount);
        }

        public async Task<FeeConfig> CreateOrUpdateFeeConfigAsync(string feeCode, decimal value, FeeType type, string description)
        {
            var currentFee = await _unitOfWork.GetRepository<FeeConfig>()
                .ExistEntities()
                .Where(f => f.FeeCode == feeCode && f.IsActive && !f.EffectiveTo.HasValue)
                .FirstOrDefaultAsync();

            // Nếu fee đã tồn tại, đánh dấu nó hết hiệu lực
            if (currentFee != null)
            {
                var updateFields = currentFee.MarkAsInactive(GetCurrentActorId());
                _unitOfWork.GetRepository<FeeConfig>().UpdateFields(currentFee, updateFields);
            }

            var newFee = FeeConfig.CreateNew(
                feeCode,
                value,
                type,
                description,
                GetCurrentActorId());

            _unitOfWork.GetRepository<FeeConfig>().Insert(newFee);
            await _unitOfWork.SaveAsync();

            return newFee;
        }

        public async Task<List<FeeConfig>> GetAllActiveFeesAsync()
        {
            var now = DateTime.UtcNow;
            var activeFeeVersions = await _unitOfWork.GetRepository<FeeConfig>()
                .ExistEntities()
                .Where(f => f.IsActive && f.EffectiveFrom <= now && (!f.EffectiveTo.HasValue || f.EffectiveTo > now))
                .OrderBy(f => f.FeeCode)
                .ThenByDescending(f => f.EffectiveFrom)
                .ToListAsync();

            // Thêm các fee code từ static class nếu chúng chưa có bất kỳ phiên bản nào trong DB
            var existingFeeCodesInDb = activeFeeVersions.Select(f => f.FeeCode).ToHashSet();
            var allFeeCodesFromStatic = GetAllFeeCodesFromStatic();

            foreach (var feeCode in allFeeCodesFromStatic)
            {
                if (!existingFeeCodesInDb.Contains(feeCode))
                {
                    activeFeeVersions.Add(new FeeConfig
                    {
                        FeeCode = feeCode,
                        Description = $"Default description for {feeCode}",
                        Type = FeeType.Percentage,
                        Value = 0,
                        IsActive = false,  
                        EffectiveFrom = DateTime.UtcNow
                    });
                }
            }

            return activeFeeVersions;
        }

        public Task<Dictionary<string, object>> GetFeeMetadataAsync()
        {
            var metadata = new Dictionary<string, object>();

            // Thêm thông tin về FeeType enum
            var feeTypes = Enum.GetNames(typeof(FeeType))
                .Select(name => new 
                {
                    Name = name,
                    Value = (int)Enum.Parse(typeof(FeeType), name)
                })
                .ToList();

            // Thêm thông tin về FeeCodes
            var feeCodes = GetAllFeeCodesFromStatic();

            metadata.Add("FeeTypes", feeTypes);
            metadata.Add("FeeCodes", feeCodes);

            return Task.FromResult(metadata);
        }

        public async Task<object> GetFeeInfoByCodeAsync(string feeCode)
        {
            var fee = await GetActiveFeeByCodeAsync(feeCode);
            if (fee != null)
                return fee;

            var allFeeCodes = GetAllFeeCodesFromStatic();
            if (!allFeeCodes.Contains(feeCode))
                return new { Error = $"Mã phí không hợp lệ: {feeCode}" };

            return new
            {
                FeeCode = feeCode,
                Description = $"Phí {feeCode} chưa được thiết lập",
                Type = FeeType.Percentage,
                Value = 0,
                IsActive = false,
                EffectiveFrom = DateTime.UtcNow
            };
        }

        private List<string> GetAllFeeCodesFromStatic()
        {
            // Lấy tất cả các constant từ class FeeCodes
            return typeof(FeeCodes)
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string))
                .Select(fi => fi.GetRawConstantValue() as string)
                .Where(value => value != null) // Lọc bỏ các giá trị null
                .ToList()!; // Chắc chắn rằng kết quả không null
        }
    }

}