using App.Core.Base;
using App.Core.Utils;
using App.Repositories.Models.User;
using System.Linq.Expressions;

namespace App.Repositories.Models
{
    public class BankAccount : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty; // Cần được mã hóa
        public string AccountHolderName { get; set; } = string.Empty;
        
        public virtual AppUser? User { get; set; }

        #region Behaviors
        public Expression<Func<BankAccount, object>>[] UpdateDetails(string newBankName, string newAccountNumber, string newAccountHolderName, string updatedBy)
        {
            var updatedFields = new List<Expression<Func<BankAccount, object>>>();

            if (BankName != newBankName)
            {
                BankName = newBankName;
                updatedFields.Add(x => x.BankName);
            }

            if (AccountNumber != newAccountNumber)
            {
                AccountNumber = newAccountNumber;
                updatedFields.Add(x => x.AccountNumber);
            }

            if (AccountHolderName != newAccountHolderName)
            {
                AccountHolderName = newAccountHolderName;
                updatedFields.Add(x => x.AccountHolderName);
            }

            if (updatedFields.Any())
            {
                LastUpdatedBy = updatedBy;
                LastUpdatedTime = CoreHelper.SystemTimeNow;
                updatedFields.Add(x => x.LastUpdatedBy!);
                updatedFields.Add(x => x.LastUpdatedTime);
            }
            
            return updatedFields.ToArray();
        }
        #endregion
    }
}