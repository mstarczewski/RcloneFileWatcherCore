using Microsoft.Extensions.DependencyInjection;
using RcloneFileWatcherCore.Config;
using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Infrastructure.Logging;
using RcloneFileWatcherCore.Infrastructure.Logging.Interfaces;
using RcloneFileWatcherCore.Logic.Interfaces;
using RcloneFileWatcherCore.Logic.Services;
using RcloneFileWatcherCore.Notifications;
using RcloneFileWatcherCore.Status;
using System.Collections.Concurrent;
using System.IO;

namespace RcloneFileWatcherCore.App
{
    /// <summary>
    /// Replaces the manual wiring that used to live in <c>StartupManager</c>. Registers the
    /// stable (config-independent) singletons plus the <see cref="IConfigService"/> and
    /// <see cref="IRuntimeController"/>; the controller builds the config-dependent graph
    /// (watcher, scheduler, rclone jobs) so it can be rebuilt on a live config reload.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Core services. <paramref name="logger"/> is the bootstrap logger that
        /// already had its enabled levels set while loading the config; here we attach the final
        /// log writers (base sink + in-memory broadcast buffer used by the GUI).
        /// <paramref name="configFilePath"/> is where config edits from the GUI are persisted.
        /// </summary>
        public static IServiceCollection AddRcloneFileWatcherCore(
            this IServiceCollection services, ConfigDTO config, string configFilePath, ILogger logger)
        {
            var broadcast = new BroadcastLogWriter();
            ILogWriter baseWriter = string.IsNullOrWhiteSpace(config.LogPath)
                ? new ConsoleLogWriter()
                : new FileLogWriter(config.LogPath);
            logger.SetLogWriter(new CompositeLogWriter(baseWriter, broadcast));

            services.AddSingleton(logger);
            services.AddSingleton(broadcast);
            services.AddSingleton(new ConcurrentDictionary<string, FileDTO>());
            services.AddSingleton<IFileSystem, FileSystemService>();
            services.AddSingleton<IBatchExecutionService, RcloneRunService>();
            services.AddSingleton<IRcloneVersionService, RcloneVersionService>();
            services.AddSingleton<IStatusService, StatusService>();
            services.AddSingleton<IConfigService>(sp =>
                new ConfigService(config, configFilePath, sp.GetRequiredService<ILogger>()));
            services.AddSingleton<IRuntimeController, RuntimeController>();

            // Error-email notifications: settings (with the SMTP password encrypted at rest next to
            // the config), the SMTP sender, the keyserver client, and the notifier that batches and
            // emails Error/Critical log lines.
            var configDir = Path.GetDirectoryName(Path.GetFullPath(configFilePath));
            if (string.IsNullOrEmpty(configDir))
                configDir = Directory.GetCurrentDirectory();
            var notificationsPath = Path.Combine(configDir, "notifications.json");
            var keysDir = Path.Combine(configDir, "keys");
            services.AddSingleton<INotificationSettingsStore>(_ =>
                new NotificationSettingsStore(notificationsPath, keysDir, logger));
            services.AddSingleton<IEmailSender, MailKitEmailSender>();
            services.AddSingleton<PgpKeyServer>();
            services.AddHostedService<ErrorMailNotifier>();

            services.AddHostedService<WatcherHostedService>();
            return services;
        }
    }
}
