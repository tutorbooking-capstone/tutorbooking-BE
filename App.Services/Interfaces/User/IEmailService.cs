namespace App.Services.Interfaces.User
{
    public interface IEmailService
    {
        Task SendEmailAsync(string email, string subject, string greeting, string mainMessage);
    }
}
