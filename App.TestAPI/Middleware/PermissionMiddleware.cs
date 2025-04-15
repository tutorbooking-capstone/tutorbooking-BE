using App.Core.Base;
using App.Core.Constants;
using App.Repositories.Models;
using App.Repositories.UoW;
using App.Services.Infras;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;

namespace App.TestAPI.Middleware
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
                "/api/auth/create-role",
                "/api/auth/login",
                "/api/auth/register",
                "/api/auth/confirm-email",
                "/api/auth/forgot-password",
                "/api/auth/confirm-reset-password",
                "/api/auth/reset-password",
                "/api/auth/refresh-token"
            };
            _rolePermissions = new Dictionary<string, List<string>>()
            {
                // ... (your role permissions) ...
            };
        }

        public async Task Invoke(
            HttpContext context,
            UserManager<AppUser> userManager)
        {
            if (await HasPermission(context, userManager))
                await _next(context);
            else
                await HandleForbiddenRequest(context);
        }

        private async Task<bool> HasPermission(
            HttpContext context,
            UserManager<AppUser> userManager)
        {
            string requestUri = context.Request.Path.Value!;

            if (_excludedUris.Contains(requestUri))
                return true;

            if (!requestUri.StartsWith("/api/"))
                return true;

            try
            {
                string idUser = Authentication.GetUserIdFromHttpContext(context);
                if (string.IsNullOrEmpty(idUser))
                    return false;

                var user = await userManager.FindByIdAsync(idUser);
                if (user == null || user.DeletedTime.HasValue)
                    return false;

                return true;
            }
            catch (UnauthorizedException)
            {
                _logger.LogWarning($"Unauthorized access attempt for URI: {requestUri}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking permissions for URI: {requestUri}");
                return false;
            }
        }

        private static async Task HandleForbiddenRequest(HttpContext context)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            context.Response.ContentType = "application/json";

            var error = new ErrorException(
                (int)HttpStatusCode.Forbidden,
                ResponseCodeConstants.FORBIDDEN,
                "Bạn không có quyền truy cập tài nguyên này.");
            string result = JsonSerializer.Serialize(error);

            await context.Response.WriteAsync(result);
        }
    }
}
