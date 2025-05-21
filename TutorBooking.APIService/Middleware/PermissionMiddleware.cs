using App.Core.Base;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using App.Core.Constants;
using App.Repositories.Models.User;

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

                "/api/seed/hashtags",

                "/api/ngrok/tunnels",
                "/api/ngrok/tunnel-infos",
                "/api/ngrok/tunnel-infos/{id}",

                // //prefix for all database endpoints
                // "/api/database/",

                "/favicon.ico"
            };
            _rolePermissions = new Dictionary<string, List<string>>()
            {
                // ... (role permissions) ...
            };
        }

        public async Task Invoke(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            _logger.LogDebug("PermissionMiddleware: Executing for path: {Path}", path);
            var endpoint = context.GetEndpoint();

            if (endpoint?.Metadata.GetMetadata<AllowAnonymousAttribute>() != null)
            {
                _logger.LogInformation("PermissionMiddleware: [AllowAnonymous] attribute found on endpoint for path [{Path}]. Skipping permission checks.", path);
                await _next(context);
                return;
            }

            // Chỉ log thông tin về [Authorize] để debug, không thực hiện kiểm tra quyền
            var authorizeAttribute = endpoint?.Metadata.GetMetadata<AuthorizeAttribute>();
            if (authorizeAttribute != null)
            {
                _logger.LogInformation("PermissionMiddleware: [Authorize] attribute found on endpoint for path [{Path}]", path);

                // Log thông tin về role nếu có, nhưng không kiểm tra quyền
                var rolesAttribute = endpoint?.Metadata.GetMetadata<AuthorizeRolesAttribute>();
                if (rolesAttribute != null)
                {
                    var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!string.IsNullOrEmpty(userId))
                    {
                        var userRoles = context.User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? new List<string>();
                        var requiredRoles = rolesAttribute.Roles?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                        .Select(r => r.Trim())
                                                        .ToHashSet()
                                                        ?? new HashSet<string>();

                        if (_logger.IsEnabled(LogLevel.Information))
                        {
                            _logger.LogInformation(
                                "PermissionMiddleware: Role check for user {UserId} - Current roles: [{UserRoles}], Required roles: [{RequiredRoles}]",
                                userId,
                                string.Join(", ", userRoles),
                                string.Join(", ", requiredRoles));
                        }
                    }
                }
            }

            // Thực hiện kiểm tra quyền tùy chỉnh
            if (await HasPermission(context))
            {
                _logger.LogInformation("PermissionMiddleware: Custom permission GRANTED for path: {Path}", path);
                await _next(context);
            }
            else
            {
                _logger.LogWarning("PermissionMiddleware: Custom permission DENIED for path: {Path}. Returning 403.", path);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new ErrorException(
                    StatusCodes.Status403Forbidden,
                    ResponseCodeConstants.FORBIDDEN,
                    "Bạn không có quyền truy cập tài nguyên này.").ErrorDetail);
            }
        }

        private Task<bool> HasPermission(HttpContext context)
        {
            string requestUri = context.Request.Path.Value!.ToLower();
            _logger.LogDebug("PermissionMiddleware.HasPermission: Checking permissions for URI: {RequestUri}", requestUri);

            // 1. Chỉ cần kiểm tra các URI trong excluded list và trả về true nếu khớp
            if (_excludedUris.Any(uri =>
                requestUri.Equals(uri, StringComparison.OrdinalIgnoreCase) ||
                (uri.EndsWith("/") && requestUri.StartsWith(uri, StringComparison.OrdinalIgnoreCase)) ||
                requestUri.StartsWith(uri.Replace("{id}", ""))))
            {
                _logger.LogDebug("PermissionMiddleware.HasPermission: URI {RequestUri} is in excluded list. Granting access.", requestUri);
                return Task.FromResult(true);
            }

            // 2. Kiểm tra xem endpoint có attribute không
            var endpoint = context.GetEndpoint();
            if (endpoint?.Metadata.GetMetadata<AllowAnonymousAttribute>() != null)
            {
                // AllowAnonymous đã được xử lý, không cần kiểm tra thêm
                return Task.FromResult(true);
            }

            if (endpoint?.Metadata.GetMetadata<AuthorizeAttribute>() != null)
            {
                // Attribute đã được UseAuthorization() xử lý, không cần kiểm tra thêm
                // Nếu đến được đây, nghĩa là request đã vượt qua UseAuthorization()
                return Task.FromResult(true);
            }

            // 3. Chỉ áp dụng logic tùy chỉnh cho các endpoint KHÔNG có attribute
            // (Đây là trường hợp đặc biệt mà ASP.NET Core Authorization không xử lý)
            
            string? userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("PermissionMiddleware.HasPermission: Could not get User ID from HttpContext.User claims for URI: {RequestUri}. Denying access.", requestUri);
                return Task.FromResult(false);
            }

            var userRoles = context.User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? new List<string>();
            _logger.LogDebug("PermissionMiddleware.HasPermission: User ID {UserId} has roles from claims: [{Roles}] for URI: {RequestUri}", 
                userId, string.Join(", ", userRoles), requestUri);

            // 4. Áp dụng các quy tắc phân quyền dựa trên pattern URI và role
            // Đây là logic bổ sung mà ASP.NET Core Authorization không hỗ trợ mặc định
            foreach (var role in userRoles)
            {
                if (_rolePermissions.TryGetValue(role, out var allowedPaths))
                {
                    foreach (var allowedPathPattern in allowedPaths)
                    {
                        if (requestUri.StartsWith(allowedPathPattern))
                        {
                            _logger.LogDebug("PermissionMiddleware.HasPermission: User ID {UserId} has role '{Role}' which grants access to path pattern '{PathPattern}' for URI: {RequestUri}", 
                                userId, role, allowedPathPattern, requestUri);
                            return Task.FromResult(true);
                        }
                    }
                }
            }

            _logger.LogWarning("PermissionMiddleware.HasPermission: User ID {UserId} with roles [{Roles}] does NOT have custom permission for URI: {RequestUri}", 
                userId, string.Join(", ", userRoles), requestUri);
            return Task.FromResult(false);
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
