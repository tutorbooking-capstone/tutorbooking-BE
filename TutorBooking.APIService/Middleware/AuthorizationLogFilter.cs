using App.Repositories.Models.User;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace TutorBooking.APIService.Middleware
{
public class AuthorizationLogFilter : IAuthorizationFilter
{
    private readonly ILogger<AuthorizationLogFilter> _logger;

    public AuthorizationLogFilter(ILogger<AuthorizationLogFilter> logger) 
    {
        _logger = logger;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var endpoint = context.HttpContext.GetEndpoint();
        var user = context.HttpContext.User;
        var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRoles = user?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? new List<string>();
        
        _logger.LogInformation(
            "Authorization check for path {Path} - User ID: {UserId}, Roles: [{Roles}]",
            context.HttpContext.Request.Path,
            userId,
            string.Join(", ", userRoles));
            

        var authAttribute = endpoint?.Metadata.GetMetadata<AuthorizeRolesAttribute>();
        if (authAttribute != null && context.HttpContext.Request.Path.ToString().Contains("/api/tutor/languages"))
        {
            var requiredRoles = authAttribute.Roles?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                        .Select(r => r.Trim())
                                        .ToHashSet() 
                                        ?? new HashSet<string>();
                                        
            _logger.LogWarning(
                "Detailed auth check for /api/tutor/languages - User has roles: [{UserRoles}], Required roles: [{RequiredRoles}]",
                string.Join(", ", userRoles),
                string.Join(", ", requiredRoles));
        }
    }
}
}
