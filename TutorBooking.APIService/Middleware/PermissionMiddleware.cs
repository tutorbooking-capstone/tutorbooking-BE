using App.Core.Base;
using App.Repositories.Models;
using Microsoft.AspNetCore.Identity;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using App.Core.Constants;

namespace TutorBooking.APIService.Middleware
{
    public class PermissionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PermissionMiddleware> _logger;
        private readonly Dictionary<string, List<string>> _rolePermissions;
        private readonly IEnumerable<string> _excludedUris;

        public PermissionMiddleware(
            RequestDelegate next,
            ILogger<PermissionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            _excludedUris = new List<string>
            {
                "/api/auth/sync-roles",
                "/api/auth/create-role",
                "/api/auth/login",
                "/api/auth/register",
                "/api/auth/confirm-email",
                "/api/auth/forgot-password",
                "/api/auth/confirm-reset-password",
                "/api/auth/reset-password",
                "/api/auth/refresh-token",

                "/api/hashtag/seed",
                "/api/hashtag/get-seed",
                "/api/hashtag/get-seed-id"

            };
            _rolePermissions = new Dictionary<string, List<string>>()
            {
                // ... (role permissions) ...
            };
        }

        public async Task Invoke(
            HttpContext context,
            UserManager<AppUser> userManager)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            _logger.LogDebug("PermissionMiddleware: Executing for path: {Path}", path);
            var endpoint = context.GetEndpoint();
            
            if (endpoint?.Metadata.GetMetadata<AuthorizeAttribute>() != null)
            {
                _logger.LogInformation("PermissionMiddleware: [Authorize] attribute found on endpoint for path\n\n [{Path}]\n", path);
                await _next(context);
                return;
            }

            _logger.LogDebug("PermissionMiddleware: No [Authorize] attribute found. Proceeding with custom checks for path {Path}.", path);

            if (await HasPermission(context))
            {
                _logger.LogInformation("PermissionMiddleware: Custom permission GRANTED for path: {Path}", path);
                await _next(context);
            }
            else
            {
                _logger.LogWarning("PermissionMiddleware: Custom permission DENIED for path: {Path}. Returning 403.", path);
                throw new ErrorException(
                    StatusCodes.Status403Forbidden,
                    ResponseCodeConstants.FORBIDDEN,
                    "Bạn không có quyền truy cập tài nguyên này.");
            }
        }

        private Task<bool> HasPermission(HttpContext context)
        {
            string requestUri = context.Request.Path.Value!.ToLower();
            _logger.LogDebug("PermissionMiddleware.HasPermission: Checking custom permissions for URI: {RequestUri}", requestUri);

            if (_excludedUris.Any(uri => requestUri.StartsWith(uri.Replace("{id}", ""))))
            {
                _logger.LogDebug("PermissionMiddleware.HasPermission: URI {RequestUri} is in excluded list. Granting access.", requestUri);
                return Task.FromResult(true);
            }

            string? idUser = null;
            try
            {
                idUser = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(idUser))
                {
                    _logger.LogWarning("PermissionMiddleware.HasPermission: Could not get User ID from HttpContext.User claims for URI: {RequestUri}. Denying access.", requestUri);
                    return Task.FromResult(false);
                }
                _logger.LogDebug("PermissionMiddleware.HasPermission: User ID {UserId} obtained for URI: {RequestUri}", idUser, requestUri);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PermissionMiddleware.HasPermission: Error getting User ID for URI: {RequestUri}", requestUri);
                return Task.FromResult(false);
            }

            var rolesFromClaims = context.User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? new List<string>();
            _logger.LogDebug("PermissionMiddleware.HasPermission: User ID {UserId} has roles from claims: [{Roles}] for URI: {RequestUri}", idUser, string.Join(", ", rolesFromClaims), requestUri);

            bool hasAccessBasedOnCustomLogic = false;
            foreach (var role in rolesFromClaims)
            {
                if (_rolePermissions.TryGetValue(role, out var allowedPaths))
                {
                    foreach (var allowedPathPattern in allowedPaths)
                    {
                        if (requestUri.StartsWith(allowedPathPattern))
                        {
                            hasAccessBasedOnCustomLogic = true;
                            _logger.LogDebug("PermissionMiddleware.HasPermission: User ID {UserId} has role '{Role}' which grants access to path pattern starting with '{PathPattern}' for URI: {RequestUri}", idUser, role, allowedPathPattern, requestUri);
                            break;
                        }
                    }
                    if (hasAccessBasedOnCustomLogic)
                        break;
                }
            }

            if (!hasAccessBasedOnCustomLogic)
            {
                _logger.LogWarning("PermissionMiddleware.HasPermission: User ID {UserId} with roles [{Roles}] does NOT have custom permission for URI: {RequestUri}", idUser, string.Join(", ", rolesFromClaims), requestUri);
            }

            return Task.FromResult(hasAccessBasedOnCustomLogic);
        }

        private static async Task HandleForbiddenRequest(HttpContext context)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            context.Response.ContentType = "application/json";

            var error = new ErrorException(
                StatusCodes.Status403Forbidden,
                ResponseCodeConstants.FORBIDDEN,
                "Bạn không có quyền truy cập tài nguyên này.");
            string result = JsonSerializer.Serialize(error);

            await context.Response.WriteAsync(result);
        }
    }
}
