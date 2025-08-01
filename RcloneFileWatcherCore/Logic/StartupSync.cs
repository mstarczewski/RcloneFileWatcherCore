using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Globals;
using RcloneFileWatcherCore.Logic.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;

namespace RcloneFileWatcherCore.Logic
{
    public class StartupSync : IStartupSync
    {
        private readonly ILogger _logger;
        private readonly ConfigDTO _configDTO;
        public StartupSync(ILogger logger, ConfigDTO configDTO)
        {
            _logger = logger;
            _configDTO = configDTO;
        }
        public bool StartStartupSync()
        {
            try
            {
                if ((_configDTO?.RunOneTimeFullStartupSync ?? false) && !string.IsNullOrWhiteSpace(_configDTO.RunOneTimeFullStartupSyncBatch))
                {
                    _logger.Write("Running one-time full startup sync.");
                    RcloneProcess.RunRcloneProcess(_configDTO.RunOneTimeFullStartupSyncBatch, _logger);
                    return true;
                }
                else
                {
                    _logger.Write("Skipping one-time full startup sync.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Write(ex.ToString());
                return false;
            }
        }
    }
}