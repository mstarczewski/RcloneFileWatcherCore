using RcloneFileWatcherCore.Logic.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using RcloneFileWatcherCore.DTO;

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
                List<PathDTO> pathDTOs = new List<PathDTO>();
                if (!File.Exists(_configFileName))
                {
                    _logger.WriteAlways("Config file is missing");
                    return null;
                }
                string[] parameters = File.ReadAllLines(_configFileName);
                foreach (var param in parameters)
                {
                    if (param.ToUpper().Contains("CONSOLEWRITER.OFF"))
                    {
                        _logger.Enable = false;
                    }
                    else if (param.ToUpper().Contains("CONSOLEWRITER.ON"))
                    {
                        _logger.Enable = true;
                    }
                    else
                    {
                        var item = param.Split(',');
                        if (item.Length == 3)
                        {
                            PathDTO pathDTO = new PathDTO();
                            pathDTO.WatchingPath = item[0];
                            pathDTO.RcloneFilesFromPath = item[1];
                            pathDTO.RcloneBatch = item[2];
                            pathDTOs.Add(pathDTO);
                        }
                        else
                        {
                            _logger.WriteAlways("Error in config file.");
                            return null;
                        }
                    }
                }
                return pathDTOs;
            }
            catch (Exception ex)
            {
                _logger.WriteAlways(ex.ToString());
                return null;
            }
        }
    }
}