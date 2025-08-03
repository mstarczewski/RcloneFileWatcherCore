using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Enums;
using RcloneFileWatcherCore.Infrastructure.Logging.Interfaces;
using System;
using System.IO;
using System.Text.Json;

namespace RcloneFileWatcherCore.Config
{
    internal class ConfigLoader
    {
        private readonly string _configFileName;
        private readonly ILogger _logger;

        public ConfigLoader(string configFileName, ILogger logger)
        {
            _configFileName = configFileName;
            _logger = logger;
        }

        internal ConfigDTO LoadConfig()
        {
            if (!File.Exists(_configFileName))
            {
                _logger.Log(LogLevel.Error, $"Config file is missing: {_configFileName}");
                return null;
            }

            try
            {
                using var stream = File.OpenRead(_configFileName);
                var config = JsonSerializer.Deserialize<ConfigDTO>(stream);
                if (config == null)
                {
                    _logger.Log(LogLevel.Error, "Config file is empty or invalid");
                    return null;
                }
                _logger.EnabledLevels = ParseLogLevels(config.LogLevel);
                return config;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Config file error: {_configFileName}", ex);
                return null;
            }
        }
        private LogLevel ParseLogLevels(string configLogLevel)
        {
            if (string.IsNullOrWhiteSpace(configLogLevel))
            {
                return LogLevel.All;
            }

            LogLevel result = LogLevel.None;
            var parts = configLogLevel.Split(new[] { ',', ';', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                if (Enum.TryParse(part.Trim(), ignoreCase: true, out LogLevel level))
                {
                    result |= level;
                }
            }

            return result == LogLevel.None ? LogLevel.All : result;
        }
    }
}