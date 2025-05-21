using App.Core.Base;
using System.Text.Json;

namespace TutorBooking.APIService.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning("Validation errors: {Errors}", ex.ErrorDetail.ErrorMessage);
                context.Response.StatusCode = ex.StatusCode;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(ex.ErrorDetail));
            }
            catch (InvalidArgumentException ex)
            {
                _logger.LogWarning("Invalid argument: {ErrorCode}, {TextMessage}", ex.ErrorDetail.ErrorCode, ex.Message);
                context.Response.StatusCode = ex.StatusCode;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(ex.ErrorDetail));
            }
            catch (AlreadySeededException ex)
            {
                _logger.LogWarning("Already seeded: {ErrorCode}, {TextMessage}", ex.ErrorDetail.ErrorCode, ex.Message);
                context.Response.StatusCode = ex.StatusCode;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(ex.ErrorDetail));
            }
            catch (UnauthorizedRoleException ex)
            {
                _logger.LogWarning(
                    "Role mismatch: User ID {UserId} with roles {UserRoles} requires one of {RequiredRoles}", 
                    ex.ErrorDetail.ErrorMessage,
                    ex.ErrorDetail.ErrorMessage,
                    ex.ErrorDetail.ErrorMessage);
                
                context.Response.StatusCode = ex.StatusCode;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(ex.ErrorDetail));
            }
            catch (ErrorException ex)
            {
                _logger.LogError(ex, "Error: {ErrorCode}, {TextMessage}", ex.ErrorDetail.ErrorCode, ex.Message);
                context.Response.StatusCode = ex.StatusCode;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(ex.ErrorDetail));
            }
            catch (CoreException ex)
            {
                _logger.LogError(ex, "Core error: {Code}, {TextMessage}", ex.Code, ex.Message);
                context.Response.StatusCode = ex.StatusCode;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new 
                { 
                    statusCode = ex.StatusCode,
                    errorCode = ex.Code,
                    errorMessage = ex.Message,
                    ex.AdditionalData 
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error: {TextMessage}", ex.Message);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new 
                { 
                    statusCode = StatusCodes.Status500InternalServerError,
                    errorCode = "unexpected_error",
                    errorMessage = $"An unexpected error occurred: {ex.Message}"
                }));
            }
        }
    }
}
