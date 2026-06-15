using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Enums;
using RcloneFileWatcherCore.Infrastructure.Logging.Interfaces;
using System;

namespace RcloneFileWatcherCore.Config
{
    public class ConfigService : IConfigService
    {
        private readonly ILogger _logger;
        private readonly object _lock = new object();
        private ConfigDTO _current;

        public ConfigService(ConfigDTO initial, string filePath, ILogger logger)
        {
            _current = ConfigNormalizer.Normalize(initial ?? new ConfigDTO());
            FilePath = filePath;
            _logger = logger;
        }

        public ConfigDTO Current
        {
            get { lock (_lock) { return _current; } }
        }

        public string FilePath { get; }

        public event Action<ConfigDTO> Changed;

        public void Save(ConfigDTO config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            ConfigNormalizer.Normalize(config);
            ConfigWriter.Save(FilePath, config);

            lock (_lock)
            {
                _current = config;
            }

            // Log level can be applied live; the log writer (file vs console) stays as set at
            // startup, so changing LogPath still needs a restart.
            _logger.EnabledLevels = LogLevelParser.Parse(config.LogLevel);
            _logger.Log(LogLevel.Information, $"Configuration saved to {FilePath}");

            Changed?.Invoke(config);
        }
    }
}
