// App.Repositories/Models/Payment/FeeConfig.cs
using App.Core.Base;

namespace App.Repositories.Models.Payment
{
    #region Enums
    public enum FeeType
    {
        Percentage = 0,
        Flat = 1
    }
    #endregion
    
    public class FeeConfig : CoreEntity
    {
        public string FeeCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public FeeType Type { get; set; } = FeeType.Percentage;
        public decimal Value { get; set; }
        public bool IsActive { get; set; } = true;
        
        public decimal CalculateFee(decimal amount)
        {
            return Type switch
            {
                FeeType.Percentage => Math.Round(amount * Value, 2),
                FeeType.Flat => Value,
                _ => 0
            };
        }
    }

}