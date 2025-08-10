using Hangfire.Dashboard;

namespace TutorBooking.APIService.Middleware
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            // Tạm thời cho phép tất cả truy cập trong môi trường phát triển
            return true;
            
            // var httpContext = context.GetHttpContext();
            // return httpContext.User.IsInRole(Role.Admin.ToStringRole()) || 
            //        httpContext.User.IsInRole(Role.Staff.ToStringRole());
        }
    }
}