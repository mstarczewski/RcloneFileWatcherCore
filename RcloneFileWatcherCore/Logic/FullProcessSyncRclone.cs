using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Globals;
using RcloneFileWatcherCore.Logic.Interfaces;
using System;

namespace RcloneFileWatcherCore.Logic
{
    public class FullProcessSyncRclone : IProcess
    {
        private readonly ILogger _logger;
        public FullProcessSyncRclone(ILogger logger)
        {
            _logger = logger;
        }
        public bool Start(ConfigDTO configDTO)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(configDTO.RunOneTimeFullStartupSyncBatch))
                {
                    _logger.Write("Running full sync.");
                    RcloneProcess.RunRcloneProcess(configDTO.RunOneTimeFullStartupSyncBatch, _logger);
                    return true;
                }
                else
                {
                    _logger.Write("Skipping full sync.");
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