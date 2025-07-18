using App.Core.Base;
using App.Repositories.Models.User;

namespace App.Repositories.Models
{
    public class BankAccount : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty; // Cần được mã hóa
        public string AccountHolderName { get; set; } = string.Empty;
        
        public virtual AppUser? User { get; set; }
    }
}