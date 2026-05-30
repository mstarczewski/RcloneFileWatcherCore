using Microsoft.Extensions.DependencyInjection;
using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Enums;
using RcloneFileWatcherCore.Infrastructure.Logging;
using RcloneFileWatcherCore.Infrastructure.Logging.Interfaces;
using RcloneFileWatcherCore.Logic.Interfaces;
using RcloneFileWatcherCore.Logic.Services;
using RcloneFileWatcherCore.Status;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RcloneFileWatcherCore.App
{
    /// <summary>
    /// Replaces the manual wiring that used to live in <c>StartupManager</c>. Registers the
    /// watcher/scheduler graph in the DI container so the same Core can be hosted by the
    /// console app and, later, the web GUI.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Core services. <paramref name="logger"/> is the bootstrap logger that
        /// already had its enabled levels set while loading the config; here we attach the final
        /// log writers (base sink + in-memory broadcast buffer used by the GUI).
        /// </summary>
        public static IServiceCollection AddRcloneFileWatcherCore(this IServiceCollection services, ConfigDTO config, ILogger logger)
        {
            var broadcast = new BroadcastLogWriter();
            ILogWriter baseWriter = string.IsNullOrWhiteSpace(config.LogPath)
                ? new ConsoleLogWriter()
                : new FileLogWriter(config.LogPath);
            logger.SetLogWriter(new CompositeLogWriter(baseWriter, broadcast));

            services.AddSingleton(config);
            services.AddSingleton(logger);
            services.AddSingleton(broadcast);
            services.AddSingleton(new ConcurrentDictionary<string, FileDTO>());
            services.AddSingleton<IFileSystem, FileSystemService>();
            services.AddSingleton<IBatchExecutionService, RcloneRunService>();
            services.AddSingleton<IStatusService, StatusService>();

            services.AddSingleton(sp => new FilePrepareService(
                sp.GetRequiredService<ILogger>(),
                sp.GetRequiredService<ConfigDTO>().Path,
                sp.GetRequiredService<ConcurrentDictionary<string, FileDTO>>(),
                sp.GetRequiredService<IFileSystem>()));

            services.AddSingleton<RcloneSyncService>();
            services.AddSingleton<RcloneUpdateService>();
            services.AddSingleton<RcloneFullSyncService>();

            services.AddSingleton(sp => new Dictionary<ProcessCode, IRcloneJobService>
            {
                { ProcessCode.SyncRclone, sp.GetRequiredService<RcloneSyncService>() },
                { ProcessCode.UpdateRclone, sp.GetRequiredService<RcloneUpdateService>() },
                { ProcessCode.FullSyncRclone, sp.GetRequiredService<RcloneFullSyncService>() },
            });

            services.AddSingleton<Scheduler>();
            services.AddSingleton(sp => new FileWatcherService(
                sp.GetRequiredService<ILogger>(),
                sp.GetRequiredService<ConcurrentDictionary<string, FileDTO>>(),
                sp.GetRequiredService<ConfigDTO>().Path));

            services.AddHostedService<WatcherHostedService>();
            return services;
        }
    }
}
