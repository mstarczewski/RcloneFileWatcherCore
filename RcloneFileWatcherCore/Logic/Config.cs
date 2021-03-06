using RcloneFileWatcherCore.Logic.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using RcloneFileWatcherCore.DTO;
using System.Linq;
using System.Text.Json;

namespace RcloneFileWatcherCore.Logic
{
    class Config
    {
        private readonly string _configFileName;
        private readonly ILogger _logger;
        public Config(string configFileName, ILogger logger)
        {
            _configFileName = configFileName;
            _logger = logger;
        }

        internal List<PathDTO> LoadConfig()
        {
            try
            {
                if (!File.Exists(_configFileName))
                {
                    _logger.WriteAlways("Config file is missing");
                    return null;
                }

                var _config = JsonSerializer.Deserialize<ConfigDTO>(File.ReadAllText(_configFileName));
                _logger.Enable = _config.ConsoleWriter;
                return _config.Path;
            }
            catch (Exception ex)
            {
                _logger.WriteAlways(ex.ToString());
                return null;
            }
        }
    }
}