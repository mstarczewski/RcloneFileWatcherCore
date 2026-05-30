using Microsoft.Extensions.Hosting;
using RcloneFileWatcherCore.Globals;
using RcloneFileWatcherCore.Infrastructure.Logging.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace RcloneFileWatcherCore.App
{
    /// <summary>
    /// Hosts the runtime inside the generic host. Replaces the old blocking
    /// <c>AutoResetEvent.WaitOne()</c> loop: the watcher (FileSystemWatcher) and scheduler
    /// (timer) run on their own threads via the <see cref="IRuntimeController"/>, so startup
    /// only kicks them off and the host keeps the process alive until shutdown.
    /// </summary>
    public class WatcherHostedService : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IRuntimeController _controller;

        public WatcherHostedService(ILogger logger, IRuntimeController controller)
        {
            _logger = logger;
            _controller = controller;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.Log(Enums.LogLevel.Always, AppVersion.GetVersion());
            _controller.Start();
            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _controller.Stop();
            await base.StopAsync(cancellationToken);
        }
    }
}
