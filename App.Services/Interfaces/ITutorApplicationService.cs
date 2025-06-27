namespace App.Services.Interfaces
{
    public interface ITutorApplicationService
    {
        Task CreateTutorApplicationAsync(string tutorId);
        Task RequestVerificationAsync(string tutorApplicationId);
    }
}
