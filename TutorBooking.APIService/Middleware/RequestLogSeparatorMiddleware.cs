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

                #region  Handmade Query Log
                // if (queryTracker.Queries.Any())
                // {
                //     _logger.LogInformation("--- Query Summary (Chronological) ---");
                    
                //     int index = 1;
                //     foreach (var query in queryTracker.Queries)
                //     {
                //         // Get basic info about the query
                //         var (queryType, tableName) = ParseQueryInfo(query.CommandText ?? "");
                        
                //         // Log a simplified summary
                //         _logger.LogInformation(
                //             "    {Index}. {QueryType} {TableName} ({Duration:F2}ms)",
                //             index++,
                //             queryType,
                //             tableName,
                //             query.DurationMs);
                //     }
                // }
                #endregion
            }
        }

        private (string QueryType, string TableName) ParseQueryInfo(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return ("UNKNOWN", "UNKNOWN");
        
            // Get first word as query type
            var firstWord = sql.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault()?.ToUpper() ?? "UNKNOWN";
        
            string tableName = "UNKNOWN";
        
            // Simple approach - just look for common patterns
            if (sql.Contains(" FROM "))
            {
                // For SELECT statements
                var parts = sql.Split(new[] { " FROM " }, StringSplitOptions.None);
                if (parts.Length > 1)
                {
                    var afterFrom = parts[1].TrimStart();
                    tableName = afterFrom.Split(new[] { ' ', '\r', '\n', ',', ';', '(', ')' }, StringSplitOptions.RemoveEmptyEntries)
                        .FirstOrDefault() ?? "UNKNOWN";
                }
            }
            else if (sql.Contains(" INTO "))
            {
                // For INSERT statements
                var parts = sql.Split(new[] { " INTO " }, StringSplitOptions.None);
                if (parts.Length > 1)
                {
                    var afterInto = parts[1].TrimStart();
                    tableName = afterInto.Split(new[] { ' ', '\r', '\n', '(', ')' }, StringSplitOptions.RemoveEmptyEntries)
                        .FirstOrDefault() ?? "UNKNOWN";
                }
            }
            else if (firstWord == "UPDATE")
            {
                // For UPDATE statements
                var afterUpdate = sql.Substring(6).TrimStart();
                tableName = afterUpdate.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault() ?? "UNKNOWN";
            }
        
            // Return a cleaner table name (remove schema if present)
            tableName = tableName.Split('.').Last();
        
            return (firstWord, tableName);
        }
    }
}