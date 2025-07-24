namespace App.Services.Interfaces
{
    public interface IPaymentProcessingService
    {
        Task ProcessHeldFundReleaseAsync(string heldFundId);
    }
}