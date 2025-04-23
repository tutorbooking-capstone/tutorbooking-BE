namespace App.Repositories.Models.User
{
    public class Staff
    {
        public string UserId { get; set; } = string.Empty;

        public virtual AppUser? User { get; set; } 
    }
}
