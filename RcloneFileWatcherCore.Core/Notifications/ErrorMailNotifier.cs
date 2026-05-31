using Microsoft.Extensions.Hosting;
using RcloneFileWatcherCore.Enums;
using RcloneFileWatcherCore.Infrastructure.Logging;
using RcloneFileWatcherCore.Infrastructure.Logging.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RcloneFileWatcherCore.Notifications
{
    /// <summary>
    /// Watches the log stream for Error/Critical lines and emails them. The first error after a quiet
    /// period opens a batching window (NotificationSettings.DelaySeconds); every error in that window
    /// is collected and sent as one email when the window closes — so a burst of failures produces a
    /// single message instead of a flood. Send failures are logged at Warning level (never Error) so
    /// a failing mail server can't feed itself a new error and loop.
    /// </summary>
    public class ErrorMailNotifier : IHostedService, IDisposable
    {
        private readonly BroadcastLogWriter _broadcast;
        private readonly INotificationSettingsStore _store;
        private readonly IEmailSender _sender;
        private readonly ILogger _logger;

        private readonly object _lock = new object();
        private readonly List<string> _pending = new List<string>();
        private Timer _timer;
        private bool _windowOpen;

        public ErrorMailNotifier(BroadcastLogWriter broadcast, INotificationSettingsStore store, IEmailSender sender, ILogger logger)
        {
            _broadcast = broadcast;
            _store = store;
            _sender = sender;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _broadcast.MessageWritten += OnMessage;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _broadcast.MessageWritten -= OnMessage;
            return Task.CompletedTask;
        }

        private void OnMessage(string line)
        {
            if (!BroadcastLogWriter.IsErrorLine(line))
                return;

            var settings = _store.Current;
            if (settings == null || !settings.Enabled || settings.Recipients == null || settings.Recipients.Count == 0)
                return;

            lock (_lock)
            {
                _pending.Add(line);
                if (_windowOpen)
                    return;

                _windowOpen = true;
                var delayMs = Math.Max(0, settings.DelaySeconds) * 1000;
                _timer?.Dispose();
                _timer = new Timer(_ => Flush(), null, delayMs, Timeout.Infinite);
            }
        }

        private void Flush()
        {
            List<string> batch;
            lock (_lock)
            {
                batch = new List<string>(_pending);
                _pending.Clear();
                _windowOpen = false;
                _timer?.Dispose();
                _timer = null;
            }

            if (batch.Count == 0)
                return;

            // Fire-and-forget; SendBatchAsync swallows and Warning-logs its own failures.
            _ = SendBatchAsync(batch);
        }

        private async Task SendBatchAsync(List<string> batch)
        {
            try
            {
                var settings = _store.Current;
                if (settings == null || !settings.Enabled)
                    return;

                var subject = $"RcloneFileWatcher: {batch.Count} error(s)";
                var body = string.Join(Environment.NewLine, batch);

                foreach (var recipient in settings.Recipients)
                {
                    if (recipient == null || string.IsNullOrWhiteSpace(recipient.Email))
                        continue;
                    if (recipient.Encrypt && string.IsNullOrWhiteSpace(recipient.PublicKey))
                    {
                        _logger.Log(LogLevel.Warning, $"Skipping error email to {recipient.Email}: encryption enabled but no public key.");
                        continue;
                    }

                    try
                    {
                        await _sender.SendAsync(settings.Smtp, recipient, subject, body);
                        _logger.Log(LogLevel.Information, $"Error email sent to {recipient.Email} ({batch.Count} error(s)).");
                    }
                    catch (Exception ex)
                    {
                        // Warning (not Error) so a failing mail server doesn't re-trigger notifications.
                        _logger.Log(LogLevel.Warning, $"Could not send error email to {recipient.Email}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Warning, "Error-email batch failed", ex);
            }
        }

        public void Dispose()
        {
            _broadcast.MessageWritten -= OnMessage;
            _timer?.Dispose();
        }
    }
}
