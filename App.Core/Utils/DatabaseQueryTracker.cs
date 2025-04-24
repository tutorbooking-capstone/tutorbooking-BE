using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace App.Core.Utils
{
    public class DatabaseQueryTracker : IDisposable, IObserver<DiagnosticListener>
    {
        private readonly ConcurrentBag<DatabaseQueryInfo> _queries = new();
        private IDisposable? _subscription;

        public IReadOnlyCollection<DatabaseQueryInfo> Queries => _queries;
        public int QueryCount => _queries.Count;
        public double TotalDurationMs => _queries.Sum(q => q.DurationMs);

        public DatabaseQueryTracker()
        {
            DiagnosticListener.AllListeners.Subscribe(this);
        }

        public void OnCompleted() { }
        public void OnError(Exception error) { }

        public void OnNext(DiagnosticListener listener)
        {
            if (listener.Name == DbLoggerCategory.Name)
            {
                _subscription = listener.Subscribe(new DatabaseQueryObserver(_queries));
            }
        }

        public void Dispose()
        {
            _subscription?.Dispose();
            GC.SuppressFinalize(this);
        }

        private class DatabaseQueryObserver : IObserver<KeyValuePair<string, object?>>
        {
            private readonly ConcurrentBag<DatabaseQueryInfo> _queries;

            public DatabaseQueryObserver(ConcurrentBag<DatabaseQueryInfo> queries)
            {
                _queries = queries;
            }

            public void OnCompleted() { }
            public void OnError(Exception error) { }

            public void OnNext(KeyValuePair<string, object?> value)
            {
                if (value.Key == RelationalEventId.CommandExecuting.Name)
                {
                    if (value.Value is CommandEventData executingData)
                    {
                        var query = new DatabaseQueryInfo
                        {
                            CommandText = executingData.Command.CommandText,
                            StartTime = DateTime.UtcNow
                        };
                        _queries.Add(query);
                    }
                }
                else if (value.Key == RelationalEventId.CommandExecuted.Name)
                {
                    if (value.Value is CommandExecutedEventData executedData)
                    {
                        var query = _queries.LastOrDefault(q => q.CommandText == executedData.Command.CommandText);
                        if (query != null)
                        {
                            query.DurationMs = executedData.Duration.TotalMilliseconds;
                            query.Success = executedData.Result == null || !(executedData.Result is Exception);
                        }
                    }
                }
            }
        }
    }

    public class DatabaseQueryInfo
    {
        public string CommandText { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public double DurationMs { get; set; }
        public bool Success { get; set; }
    }
}
