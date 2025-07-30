// App.Repositories/Models/Payment/FeeConfig.cs
using App.Core.Base;
using App.Core.Utils;
using System.Linq.Expressions;

namespace App.Repositories.Models.Payment
{
    #region Enums
    public enum FeeType
    {
        Percentage = 0,  // Tỷ lệ phần trăm (0.1 = 10%)
        Flat = 1         // Số tiền cố định
    }
    #endregion
    
    public static class FeeCodes
    {
        public const string WITHDRAWAL_FEE = "WITHDRAWAL_FEE"; // Phí rút tiền (Example: 1% fee on withdrawals)
        public const string COMMISSION_FEE = "COMMISSION_FEE"; // Phí hoa hồng (Example: $5 flat fee per commission)
    }
    
    public class FeeConfig : BaseEntity
    {
        public string FeeCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public FeeType Type { get; set; } = FeeType.Percentage;
        public decimal Value { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
        public DateTime? EffectiveTo { get; set; } = null;
        
        #region Behaviors
        public decimal CalculateFee(decimal amount)
        {
            return Type switch
            {
                FeeType.Percentage => Math.Round(amount * Value, 2), //Dùng tỷ lệ thập phân (0.1 = 10%)
                FeeType.Flat => Value, // Phí cố định
                _ => 0
            };
        }
        
        public Expression<Func<FeeConfig, object>>[] MarkAsInactive(string updatedBy)
        {
            if (!IsActive) return Array.Empty<Expression<Func<FeeConfig, object>>>();
            
            IsActive = false;
            EffectiveTo = DateTime.UtcNow;
            LastUpdatedBy = updatedBy;
            LastUpdatedTime = CoreHelper.SystemTimeNow;
            
            return
            [
                x => x.IsActive,
                x => x.EffectiveTo!,
                x => x.LastUpdatedBy!,
                x => x.LastUpdatedTime
            ];
        }
        
        public static FeeConfig CreateNew(
            string feeCode, 
            decimal value, 
            FeeType type, 
            string description, 
            string createdBy)
        {
            var newFee = new FeeConfig
            {
                FeeCode = feeCode,
                Value = value,
                Type = type,
                Description = description,
                IsActive = true,
                EffectiveFrom = DateTime.UtcNow
            };
            
            newFee.TrackCreate(createdBy);
            return newFee;
        }
        #endregion
    }
}