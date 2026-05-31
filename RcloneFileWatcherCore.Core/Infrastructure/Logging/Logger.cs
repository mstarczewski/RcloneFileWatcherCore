using RcloneFileWatcherCore.Enums;
using RcloneFileWatcherCore.Infrastructure.Logging.Interfaces;
using System;

namespace RcloneFileWatcherCore.Infrastructure.Logging
{
    public class Logger : ILogger, IDisposable
    {
        // Only the writer reference is shared mutable state; each writer is itself thread-safe
        // (FileLogWriter locks, BroadcastLogWriter locks, Console is synchronized), so we publish
        // the reference with volatile and don't hold a lock across Write — a slow disk flush in the
        // file sink no longer blocks the GUI broadcast or the console.
        private volatile ILogWriter _logWriter;

        public LogLevel EnabledLevels { get; set; } = LogLevel.All;

        public Logger()
        {
            _logWriter = new ConsoleLogWriter();
        }

        public Logger(ILogWriter logWriter)
        {
            _logWriter = logWriter ?? throw new ArgumentNullException(nameof(logWriter), "Log writer cannot be null");
        }

        public void SetLogWriter(ILogWriter logWriter)
        {
            _logWriter = logWriter ?? throw new ArgumentNullException(nameof(logWriter), "Log writer cannot be null");
        }

        public void Log(LogLevel level, string message, Exception exception = null)
        {
            if (level != LogLevel.Always && (EnabledLevels & level) == 0)
                return;

            var output = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
            if (exception != null)
                output += Environment.NewLine + exception;

            var writer = _logWriter;
            try
            {
                writer.Write(output);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[LoggerError] {writer.GetType().Name} failed: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_logWriter is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        public void Trace(string message) => Log(LogLevel.Trace, message);
        public void Debug(string message) => Log(LogLevel.Debug, message);
        public void Info(string message) => Log(LogLevel.Information, message);
        public void Warn(string message) => Log(LogLevel.Warning, message);
        public void Error(string message, Exception ex = null) => Log(LogLevel.Error, message, ex);
        public void Critical(string message, Exception ex = null) => Log(LogLevel.Critical, message, ex);
        public void Always(string message) => Log(LogLevel.Always, message);
    }
}