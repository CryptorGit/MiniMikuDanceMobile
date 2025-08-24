using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace MiniMikuDanceMaui;

internal sealed class SimpleFileLoggerProvider : ILoggerProvider
{
    private readonly string _path;
    private readonly object _lock = new();

    public SimpleFileLoggerProvider(string path)
    {
        _path = path;
    }

    public ILogger CreateLogger(string categoryName) => new SimpleFileLogger(_path, _lock);

    public void Dispose()
    {
    }

    private sealed class SimpleFileLogger : ILogger
    {
        private readonly string _path;
        private readonly object _lock;

        public SimpleFileLogger(string path, object @lock)
        {
            _path = path;
            _lock = @lock;
        }

        IDisposable ILogger.BeginScope<TState>(TState state) => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel) || formatter == null) return;
            var message = formatter(state, exception);
            var text = $"[{DateTime.Now:O}] {logLevel}: {message}";
            if (exception != null) text += $" {exception}";
            lock (_lock)
            {
                File.AppendAllText(_path, text + Environment.NewLine);
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
