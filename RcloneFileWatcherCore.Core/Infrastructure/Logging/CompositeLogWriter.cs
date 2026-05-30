using RcloneFileWatcherCore.Infrastructure.Logging.Interfaces;
using System;

namespace RcloneFileWatcherCore.Infrastructure.Logging
{
    /// <summary>
    /// Fans a single log message out to several underlying writers (e.g. file + in-memory broadcast).
    /// A failure in one writer must not prevent the others from receiving the message.
    /// </summary>
    public class CompositeLogWriter : ILogWriter, IDisposable
    {
        private readonly ILogWriter[] _writers;

        public CompositeLogWriter(params ILogWriter[] writers)
        {
            _writers = writers ?? Array.Empty<ILogWriter>();
        }

        public void Write(string message)
        {
            foreach (var writer in _writers)
            {
                try
                {
                    writer.Write(message);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[CompositeLogWriter] {writer.GetType().Name} failed: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            foreach (var writer in _writers)
            {
                if (writer is IDisposable disposable)
                    disposable.Dispose();
            }
        }
    }
}
