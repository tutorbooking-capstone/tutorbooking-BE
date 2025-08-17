using App.Services.Hangfire;
using Hangfire;

namespace App.Services.Infras
{
    public static class HangfireConfig
    {
        public static void ConfigureRecurringJobs()
        {
            RecurringJob.RemoveIfExists("delete-expired-offers");
            RecurringJob.AddOrUpdate<OfferExpirationService>(
                "delete-expired-offers",    
                service => service.ProcessExpiredOffersAsync(),
                "*/15 * * * *");
        }

        public static void ScheduleOfferExpirationJob(string offerId, DateTimeOffset expirationTime)
        {
            BackgroundJob.Schedule<OfferExpirationService>(
                service => service.HandleExpiredOfferAsync(offerId),
                expirationTime);
        }
    }
}
