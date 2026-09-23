using System.IO;
using Microsoft.Extensions.Logging;

namespace SpotlightDesktop.Services;

public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _filePath;
    private readonly object _writeLock = new();

    public FileLoggerProvider(string filePath)
    {
        _filePath = filePath;
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName, _filePath, _writeLock);

    public void Dispose()
    {
    }

    private sealed class FileLogger : ILogger
    {
        private readonly string _category;
        private readonly string _filePath;
        private readonly object _writeLock;

        public FileLogger(string category, string filePath, object writeLock)
        {
            _category = category;
            _filePath = filePath;
            _writeLock = writeLock;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{logLevel}] {_category}: {formatter(state, exception)}";
            if (exception is not null) line += Environment.NewLine + exception;

            lock (_writeLock)
            {
                File.AppendAllText(_filePath, line + Environment.NewLine);
            }
        }
    }
}
