using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Globals;
using RcloneFileWatcherCore.Logic.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;

namespace RcloneFileWatcherCore.Logic
{
    class Controller
    {
        private const string ConfigFileName = "RcloneFileWatcherCoreConfig.cfg";
        private const int ExitCodeConfigError = 2;

        private readonly ILogger _logger;
        private readonly ConfigDTO _configDTO;
        private readonly ConcurrentDictionary<string, FileDTO> _fileDTOs;
        private readonly Dictionary<Enums.ProcessCode, IProcess> _processDictionary;
        private readonly Scheduler _scheduler;
        private readonly Watcher _watcher;
        private readonly IRcloneRunner _rcloneRunner;

        internal Controller()
        {
            _logger = new ConsoleLogger();
            _configDTO = LoadConfiguration();
            _fileDTOs = new ConcurrentDictionary<string, FileDTO>();
            _rcloneRunner = new RcloneRunner(_logger);
            _processDictionary = InitProcesses();
            _scheduler = new Scheduler(_logger, _processDictionary, _configDTO);
            _watcher = new Watcher(_logger, _fileDTOs, _configDTO.Path);
        }

        public void Start(bool generateConfig)
        {

            _logger.WriteAlways(AppVersion.GetVersion());

            if (generateConfig)
            {
                new ConfigGenerator(ConfigFileName).GenerateConfig();
                _logger.WriteAlways($"Example config generated:{ConfigFileName}");
                Environment.Exit(0);
            }
            _watcher.Start();
            _logger.Write("Watcher started");
            _scheduler.RunStartupSyncIfNeeded();
            _scheduler.SetTimer();
            _logger.Write("Controller started");
        }

        private ConfigDTO LoadConfiguration()
        {
            var config = new Config(ConfigFileName, _logger).LoadConfig();
            if (config?.Path == null || !config.Path.Any())
            {
                _logger.WriteAlways("Error in config file");
                Environment.Exit(ExitCodeConfigError);
            }
            return config;
        }

        private Dictionary<Enums.ProcessCode, IProcess> InitProcesses()
        {
            var filePrepare = new FilePrepare(_logger, _configDTO.Path, _fileDTOs);
            return new Dictionary<Enums.ProcessCode, IProcess>
            {
                { Enums.ProcessCode.SyncRclone, new ProcessSyncRclone(_logger, filePrepare, _fileDTOs,_rcloneRunner ) },
                { Enums.ProcessCode.UpdateRclone, new ProcessUpdateRclone(_logger) },
                { Enums.ProcessCode.FullSyncRclone, new FullProcessSyncRclone(_logger,_rcloneRunner ) },
            };
        }
    }
}