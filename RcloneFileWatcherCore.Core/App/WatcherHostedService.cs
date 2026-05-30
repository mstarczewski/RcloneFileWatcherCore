using Microsoft.Extensions.Hosting;
using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Globals;
using RcloneFileWatcherCore.Infrastructure.Logging.Interfaces;
using RcloneFileWatcherCore.Logic.Services;
using RcloneFileWatcherCore.Status;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RcloneFileWatcherCore.App
{
    /// <summary>
    /// Hosts the file watcher and scheduler inside the generic host. Replaces the old blocking
    /// <c>AutoResetEvent.WaitOne()</c> loop: the watcher (FileSystemWatcher) and scheduler
    /// (timer) run on their own threads, so startup only needs to kick them off and the host
    /// keeps the process alive until shutdown.
    /// </summary>
    public class WatcherHostedService : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly FileWatcherService _watcher;
        private readonly Scheduler _scheduler;
        private readonly ConfigDTO _config;
        private readonly IStatusService _status;

        public WatcherHostedService(ILogger logger, FileWatcherService watcher, Scheduler scheduler, ConfigDTO config, IStatusService status)
        {
            _logger = logger;
            _watcher = watcher;
            _scheduler = scheduler;
            _config = config;
            _status = status;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.Log(Enums.LogLevel.Always, AppVersion.GetVersion());
            _watcher.Start();
            _status.MarkWatcherStarted(GetWatchedPaths());
            _logger.Log(Enums.LogLevel.Information, "Watcher started");
            _scheduler.RunStartupSyncIfNeeded();
            _scheduler.SetTimer();
            _logger.Log(Enums.LogLevel.Information, "StartupManager started");
            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _status.MarkWatcherStopped();
            await base.StopAsync(cancellationToken);
        }

        private IReadOnlyList<string> GetWatchedPaths()
        {
            return _config.Path?.Select(p => p.WatchingPath).ToList() ?? new List<string>();
        }
    }
}
