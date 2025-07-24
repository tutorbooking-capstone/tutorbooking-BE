using Hangfire.Dashboard;

namespace TutorBooking.APIService.Middleware
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            
            // Only allow admin users to access Hangfire dashboard
            return httpContext.User.IsInRole("Admin");
        }
    }
}