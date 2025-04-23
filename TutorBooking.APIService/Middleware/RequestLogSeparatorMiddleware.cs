using System.Diagnostics; 

namespace TutorBooking.APIService.Middleware
{
    public class RequestLogSeparatorMiddleware
    {
        #region DI Constructor
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLogSeparatorMiddleware> _logger;

        public RequestLogSeparatorMiddleware(RequestDelegate next, ILogger<RequestLogSeparatorMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }
        #endregion
        
        public async Task InvokeAsync(HttpContext context)
        {
            var requestId = Activity.Current?.Id ?? context.TraceIdentifier;

            _logger.LogInformation(
                "\n\n\n[--- Request Start ---]\n \t- Method = {Method}\n \t- Path = {Path}\n \t- RequestId = {RequestId}\n",
                context.Request.Method,
                context.Request.Path,
                requestId);

            var start = Stopwatch.GetTimestamp(); 
            
            try
            {
                await _next(context);
            }
            finally
            {
                var elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;

                _logger.LogInformation(
                    "\n\t- StatusCode = {StatusCode}\n \t- DurationMs = {DurationMs} ms \n[--- Request End ---]\n\n\n",
                    context.Response.StatusCode,
                    elapsedMs);
            }
        }
    }
}