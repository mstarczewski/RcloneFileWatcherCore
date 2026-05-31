using RcloneFileWatcherCore.Infrastructure.Logging.Interfaces;
using System;
using System.Collections.Generic;

namespace RcloneFileWatcherCore.Infrastructure.Logging
{
    /// <summary>
    /// In-memory log writer that keeps the most recent messages in a bounded ring buffer
    /// and raises an event for each new message. Intended as a source for the (future) GUI:
    /// it lets the UI both read the recent backlog and subscribe to a live stream of log lines.
    /// </summary>
    public class BroadcastLogWriter : ILogWriter
    {
        private readonly int _capacity;
        private readonly Queue<string> _buffer;
        private readonly object _lock = new object();

        public event Action<string> MessageWritten;

        public BroadcastLogWriter(int capacity = 10000)
        {
            _capacity = Math.Max(1, capacity);
            _buffer = new Queue<string>(_capacity);
        }

        public void Write(string message)
        {
            lock (_lock)
            {
                if (_buffer.Count >= _capacity)
                    _buffer.Dequeue();
                _buffer.Enqueue(message);
            }
            MessageWritten?.Invoke(message);
        }

        public IReadOnlyList<string> GetRecent()
        {
            lock (_lock)
            {
                return _buffer.ToArray();
            }
        }
    }
}
