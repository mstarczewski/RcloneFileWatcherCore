using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Logic.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RcloneFileWatcherCore.Logic
{
    class Controller
    {
        private ConcurrentDictionary<string, FileDTO> _fileDTOs;
        private ConfigDTO _config;
        private Watcher _watcher;
        private ILogger _logger;
        private readonly Scheduler _scheduler;
        private const string _configFileName = "RcloneFileWatcherCoreConfig.cfg";
        private const int _exitCodeConfigError = 2;
        private readonly Dictionary<Enums.ProcessCode, IProcess> _processDictionary;
        internal Controller()
        {

            //new ConfigGenerator(@"d:\RcloneFileWatcherCoreConfig.cfg").GenerateConfig(); //Example JSON config generator
            _logger = new ConsoleLogger();
            _config = new Config(_configFileName, _logger).LoadConfig();
            if (!_config.Path?.Any() ?? true)
            {
                _logger.WriteAlways("Error in config file");
                Environment.Exit(_exitCodeConfigError);
            }

            _fileDTOs = new ConcurrentDictionary<string, FileDTO>();
            FilePrepare _filePrepare = new FilePrepare(_logger, _config.Path, _fileDTOs);

            _processDictionary = new Dictionary<Enums.ProcessCode, IProcess>();
            _processDictionary.Add(Enums.ProcessCode.SyncRclone, new ProcessSyncRclone(_logger, _filePrepare, _fileDTOs));
            _processDictionary.Add(Enums.ProcessCode.UpdateRclone, new ProcessUpdateRclone(_logger));

            _scheduler = new Scheduler(_logger, _processDictionary, _config);
            _watcher = new Watcher(_logger, _fileDTOs, _config.Path);
            _watcher.Start();
            _scheduler.SetTimer();
            _logger.Write("Started");
        }
    }
}