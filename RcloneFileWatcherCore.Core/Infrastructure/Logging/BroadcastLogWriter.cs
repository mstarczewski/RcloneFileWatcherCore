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
        private readonly int _errorCapacity;
        private readonly Queue<string> _buffer;
        // Error/critical lines are kept in a separate buffer that the normal ring eviction does NOT
        // trim, so a problem stays visible in the GUI long after the rolling log has scrolled past
        // it - until the user clears it explicitly. Bounded so it can't grow without limit.
        private readonly Queue<string> _errors;
        private readonly object _lock = new object();

        public event Action<string> MessageWritten;

        public BroadcastLogWriter(int capacity = 10000, int errorCapacity = 5000)
        {
            _capacity = Math.Max(1, capacity);
            _errorCapacity = Math.Max(1, errorCapacity);
            _buffer = new Queue<string>(_capacity);
            _errors = new Queue<string>();
        }

        public void Write(string message)
        {
            lock (_lock)
            {
                if (_buffer.Count >= _capacity)
                    _buffer.Dequeue();
                _buffer.Enqueue(message);

                if (IsErrorLine(message))
                {
                    if (_errors.Count >= _errorCapacity)
                        _errors.Dequeue();
                    _errors.Enqueue(message);
                }
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

        /// <summary>Snapshot of the retained error/critical lines (kept until cleared).</summary>
        public IReadOnlyList<string> GetErrors()
        {
            lock (_lock)
            {
                return _errors.ToArray();
            }
        }

        /// <summary>Clears the retained error lines (the rolling buffer is untouched).</summary>
        public void ClearErrors()
        {
            lock (_lock)
            {
                _errors.Clear();
            }
        }

        /// <summary>True if a formatted log line is an Error/Critical entry (its level tag is the
        /// first bracketed token, e.g. "2026-05-31 10:00:00 [Error] ..."). Used to retain errors.</summary>
        public static bool IsErrorLine(string message)
        {
            if (string.IsNullOrEmpty(message))
                return false;
            var open = message.IndexOf('[');
            var close = open >= 0 ? message.IndexOf(']', open + 1) : -1;
            if (close <= open)
                return false;
            var level = message.Substring(open + 1, close - open - 1);
            return level == "Error" || level == "Critical";
        }
    }
}
