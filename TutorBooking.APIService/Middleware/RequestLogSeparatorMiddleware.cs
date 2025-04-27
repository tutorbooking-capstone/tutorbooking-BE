using System.Diagnostics;
using App.Core.Utils;

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
        
        public async Task InvokeAsync(HttpContext context, DatabaseQueryTracker queryTracker)
        {
            var requestId = Activity.Current?.Id ?? context.TraceIdentifier;

            LogRequestStart(context, requestId);
            
            var sw = Stopwatch.StartNew();
            try
            {
                await _next(context);
            }
            finally
            {
                sw.Stop();
                LogDatabaseQueries(queryTracker);
                LogRequestEnd(context, sw.Elapsed.TotalMilliseconds, requestId);
            }
        }

        private string HSpace => "\n\n\n\n\n\n\n";
        private void LogRequestStart(HttpContext context, string requestId)
        {
            _logger.LogInformation("""
                {HSpace}
                [------- Request Start -------]
                    Method: {Method}
                    Path: {Path}
                    RequestId: {RequestId}
                [-----------------------------]
                
                """,
                HSpace,
                context.Request.Method,
                context.Request.Path,
                requestId);
        }

        private void LogRequestEnd(HttpContext context, double totalMs, string requestId)
        {
            _logger.LogInformation("""

                [---------------------------]
                    Status: {StatusCode}
                    Duration: {TotalMs} ms
                [------- Request End -------]
                {HSpace}
                """,
                context.Response.StatusCode,
                totalMs,
                HSpace);
        }

        private void LogDatabaseQueries(DatabaseQueryTracker queryTracker)
        {
            if (queryTracker.QueryCount > 0)
            {
                _logger.LogInformation("""

                    --- Database Queries ---
                        Total Queries: {QueryCount}
                        Total Duration: {TotalDuration} ms

                    """,
                    queryTracker.QueryCount,
                    queryTracker.TotalDurationMs);

                foreach (var query in queryTracker.Queries)
                {
                    _logger.LogDebug("""

                        Query: {CommandText}
                            Duration: {Duration} ms
                            Status: {Status}
                        """,
                        query.CommandText,
                        query.DurationMs,
                        query.Success ? "Success" : "Failed");
                }
            }
        }
    }
}