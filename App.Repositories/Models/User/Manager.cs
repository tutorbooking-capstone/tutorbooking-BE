namespace App.Repositories.Models.User
{
    public class Manager
    {
        public string UserId { get; set; } = string.Empty;
        public string EncryptedCitizenId { get; set; } = string.Empty;
        
        public virtual AppUser? User { get; set; } 
    }
}
