using RcloneFileWatcherCore.Logic.Interfaces;
using System;
using System.IO;
using RcloneFileWatcherCore.DTO;
using System.Text.Json;

namespace RcloneFileWatcherCore.Logic
{
    internal class Config
    {
        private readonly string _configFileName;
        private readonly ILogger _logger;

        public Config(string configFileName, ILogger logger)
        {
            _configFileName = configFileName;
            _logger = logger;
        }

        internal ConfigDTO LoadConfig()
        {
            if (!File.Exists(_configFileName))
            {
                _logger.WriteAlways($"Config file is missing: {_configFileName}");
                return null;
            }

            try
            {
                using var stream = File.OpenRead(_configFileName);
                var config = JsonSerializer.Deserialize<ConfigDTO>(stream);
                if (config == null)
                {
                    _logger.WriteAlways("Config file is empty or invalid");
                    return null;
                }
                _logger.Enable = config.ConsoleWriter;
                return config;
            }
            catch (Exception ex)
            {
                _logger.WriteAlways($"Config file error: {_configFileName}");
                _logger.WriteAlways(ex.ToString());
                return null;
            }
        }
    }
}