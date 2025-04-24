using App.Core.Base;
using App.Core.Constants;
using App.Core.Jsetting;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace App.Services.Infras
{
    #region Handle Unauthorized Exception
    public class UnauthorizedException : Exception
    {
        public UnauthorizedException(string message) : base(message) { }
    }
    #endregion

    public class Authentication
    {
        public static string CreateToken(string id, JwtSettings jwtSettings, bool isRefresh = false)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, id)
            };

            DateTime dateTimeExpr = DateTime.UtcNow.AddMinutes(jwtSettings.AccessTokenExpirationMinutes);
            if (isRefresh)
                dateTimeExpr = DateTime.UtcNow.AddDays(jwtSettings.RefreshTokenExpirationDays);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey ?? string.Empty));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            JwtSecurityToken accessToken = new JwtSecurityToken(
                claims: claims,
                issuer: jwtSettings.Issuer,
                audience: jwtSettings.Audience,
                expires: dateTimeExpr,
                signingCredentials: creds
            );
            var tokenString = new JwtSecurityTokenHandler().WriteToken(accessToken);
            return tokenString;
        }

        public static string GetUserIdFromHttpContextAccessor(IHttpContextAccessor httpContextAccessor)
        {
            try
            {
                if (httpContextAccessor.HttpContext == null)
                    throw new UnauthorizedException("Need Authorization");

                string? authorizationHeader = httpContextAccessor.HttpContext.Request.Headers["Authorization"];

                if (string.IsNullOrWhiteSpace(authorizationHeader))
                {
                    authorizationHeader = httpContextAccessor.HttpContext.Request.Cookies["TokenString"];
                    if (!string.IsNullOrWhiteSpace(authorizationHeader))
                        authorizationHeader = $"Bearer {authorizationHeader}";
                }

                if (string.IsNullOrWhiteSpace(authorizationHeader) || !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    throw new UnauthorizedException($"Invalid authorization header: {authorizationHeader}");

                string jwtToken = authorizationHeader["Bearer ".Length..].Trim();
                var tokenHandler = new JwtSecurityTokenHandler();

                if (!tokenHandler.CanReadToken(jwtToken))
                    throw new UnauthorizedException("Invalid token format");

                var token = tokenHandler.ReadJwtToken(jwtToken);
                var idClaim = token.Claims.FirstOrDefault(claim => claim.Type == JwtRegisteredClaimNames.Sub);

                return idClaim?.Value ?? throw new UnauthorizedException("Cannot get userId from token");
            }
            catch (UnauthorizedException ex)
            {
                var errorResponse = new
                {
                    data = "An unexpected error occurred.",
                    message = ex.Message,
                    statusCode = StatusCodes.Status401Unauthorized,
                    code = "Unauthorized!"
                };

                var jsonResponse = System.Text.Json.JsonSerializer.Serialize(errorResponse);

                if (httpContextAccessor.HttpContext != null)
                {
                    httpContextAccessor.HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    httpContextAccessor.HttpContext.Response.ContentType = "application/json";
                    httpContextAccessor.HttpContext.Response.WriteAsync(jsonResponse).Wait();
                }

                httpContextAccessor.HttpContext?.Response.WriteAsync(jsonResponse).Wait();

                throw; 
            }
        }
        public static string GetUserIdFromHttpContext(HttpContext httpContext)
        {
            try
            {
                if (!httpContext.Request.Headers.ContainsKey("Authorization"))
                    throw new UnauthorizedException("Need Authorization");

                string? authorizationHeader = httpContext.Request.Headers["Authorization"];

                if (string.IsNullOrWhiteSpace(authorizationHeader) || !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))

                    throw new UnauthorizedException($"Invalid authorization header: {authorizationHeader}");

                string jwtToken = authorizationHeader["Bearer ".Length..].Trim();
                var tokenHandler = new JwtSecurityTokenHandler();

                if (!tokenHandler.CanReadToken(jwtToken))
                    throw new UnauthorizedException("Invalid token format");

                var token = tokenHandler.ReadJwtToken(jwtToken);
                var idClaim = token.Claims.FirstOrDefault(claim => claim.Type == JwtRegisteredClaimNames.Sub);

                return idClaim?.Value ?? throw new UnauthorizedException("Cannot get userId from token");
            }
            catch (UnauthorizedException ex)
            {
                var errorResponse = new
                {
                    data = "An unexpected error occurred.",
                    message = ex.Message,
                    statusCode = StatusCodes.Status401Unauthorized,
                    code = "Unauthorized!"
                };

                var jsonResponse = JsonSerializer.Serialize(errorResponse);
                httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                httpContext.Response.ContentType = "application/json";
                httpContext.Response.WriteAsync(jsonResponse).Wait();
                throw;
            }
        }


        public static string GetUserRoleFromHttpContext(HttpContext httpContext)
        {
            try
            {
                if (!httpContext.Request.Headers.ContainsKey("Authorization"))
                    throw new UnauthorizedException("Need Authorization");

                string? authorizationHeader = httpContext.Request.Headers["Authorization"];

                if (string.IsNullOrWhiteSpace(authorizationHeader) || !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    throw new UnauthorizedException($"Invalid authorization header: {authorizationHeader}");

                string jwtToken = authorizationHeader["Bearer ".Length..].Trim();
                var tokenHandler = new JwtSecurityTokenHandler();

                if (!tokenHandler.CanReadToken(jwtToken))
                    throw new UnauthorizedException("Invalid token format");

                var token = tokenHandler.ReadJwtToken(jwtToken);
                var roleClaim = token.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.Role);

                return roleClaim?.Value ?? throw new UnauthorizedException("Cannot get user id from token");
            }
            catch (UnauthorizedException ex)
            {
                var errorResponse = new
                {
                    data = "An unexpected error occurred.",
                    message = ex.Message,
                    statusCode = StatusCodes.Status401Unauthorized,
                    code = "Unauthorized!"
                };

                var jsonResponse = System.Text.Json.JsonSerializer.Serialize(errorResponse);

                httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                httpContext.Response.ContentType = "application/json";
                httpContext.Response.WriteAsync(jsonResponse).Wait();

                throw; // Re-throw the exception to maintain the error flow
            }
        }


        public static async Task HandleForbiddenRequest(HttpContext context)
        {
            int code = (int)HttpStatusCode.Forbidden;
            var error = new ErrorException(StatusCodes.Status401Unauthorized, ErrorCode.Forbidden, "You don't have permission to access this feature");
            string result = JsonSerializer.Serialize(error);

            context.Response.ContentType = "application/json";
            context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
            context.Response.StatusCode = code;

            await context.Response.WriteAsync(result);
        }

    }

}
