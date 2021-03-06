using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Logic.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace RcloneFileWatcherCore.Logic
{
    class Controller
    {
        private ConcurrentDictionary<string, FileDTO> _fileDTOs;
        private List<PathDTO> _pathDTOs;
        private Watcher _watcher;
        private Config _config;
        private readonly ProcessRunner _processRunner;
        private ILogger _logger;
        private readonly Scheduler _scheduler;
        private const string _configFileName = "RcloneFileWatcherCoreConfig.cfg";
        private const int _exitCodeConfigError = 2;
        internal Controller()
        {
            //new ConfigGenerator(@"d:\RcloneFileWatcherCoreConfig").GenerateConfig(); //Example JSON config generator
            _fileDTOs = new ConcurrentDictionary<string, FileDTO>();
            _logger = new ConsoleLogger();
            _config = new Config(_configFileName, _logger);
            _pathDTOs = _config.LoadConfig();
            if (!_pathDTOs?.Any() ?? true)
            {
                Environment.Exit(_exitCodeConfigError);
            }
            FilePrepare _filePrepare = new FilePrepare(_logger, _pathDTOs, _fileDTOs);
            _processRunner = new ProcessRunner(_logger, _filePrepare, _fileDTOs);
            _scheduler = new Scheduler(_logger, _processRunner);
            _watcher = new Watcher(_logger, _fileDTOs, _pathDTOs);
            _watcher.Start();
            _scheduler.SetTimer();
            _logger.Write("Started");
        }
    }
}