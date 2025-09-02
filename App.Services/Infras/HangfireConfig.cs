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
            
            RecurringJob.RemoveIfExists("process-pending-held-funds");
            RecurringJob.AddOrUpdate<BookingHeldFundService>(
                "process-pending-held-funds",
                service => service.ProcessPendingHeldFundsAsync(),
                "0 */1 * * *");  
                
            RecurringJob.RemoveIfExists("update-completed-slots");
            RecurringJob.AddOrUpdate<BookedSlotStatusUpdateService>(
                "update-completed-slots",
                service => service.ProcessCompletedSlotsAsync(),
                "*/5 * * * *");   

            RecurringJob.RemoveIfExists("process-expired-reschedule-requests");
            RecurringJob.AddOrUpdate<RescheduleExpirationService>(
                "process-expired-reschedule-requests",
                service => service.ProcessExpiredRescheduleRequestsAsync(),
                "*/15 * * * *");   
        }

        public static void ScheduleOfferExpirationJob(string offerId, DateTimeOffset expirationTime)
        {
            BackgroundJob.Schedule<OfferExpirationService>(
                service => service.HandleExpiredOfferByIdAsync(offerId),
                expirationTime);
        }

        public static void ScheduleHeldFundReleaseJob(string heldFundId, DateTime releaseTime)
        {
            BackgroundJob.Schedule<BookingHeldFundService>(
                service => service.ProcessHeldFundReleaseAsync(heldFundId),
                releaseTime - DateTime.UtcNow);
        }
        
        public static void ScheduleSlotStatusUpdateJob(string slotId, DateTime endTime)
        {
            BackgroundJob.Schedule<BookedSlotStatusUpdateService>(
                service => service.ProcessSpecificSlotAsync(slotId),
                endTime - DateTime.UtcNow);
        }

        public static void ScheduleRescheduleExpirationJob(string rescheduleRequestId, DateTime expirationTime)
        {
            BackgroundJob.Schedule<RescheduleExpirationService>(
                service => service.HandleExpiredRescheduleRequestAsync(rescheduleRequestId),
                expirationTime - DateTime.UtcNow);
        }
    }
}
