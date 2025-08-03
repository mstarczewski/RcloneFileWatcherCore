using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Globals;
using RcloneFileWatcherCore.Infrastructure.Logging;
using RcloneFileWatcherCore.Infrastructure.Logging.Interfaces;
using RcloneFileWatcherCore.Logic.Interfaces;
using RcloneFileWatcherCore.Logic.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RcloneFileWatcherCore.Config;
using System.Net.Http.Headers;
using System.Reflection;

namespace RcloneFileWatcherCore.App
{
    class StartupManager
    {
        private const string ConfigFileName = "RcloneFileWatcherCoreConfig.cfg";
        private const int ExitCodeConfigError = 2;

        private readonly ILogger _logger;
        private readonly ConfigDTO _configDTO;
        private readonly ConcurrentDictionary<string, FileDTO> _fileDTOs;
        private readonly Dictionary<Enums.ProcessCode, IRcloneJobService> _processDictionary;
        private readonly Scheduler _scheduler;
        private readonly FileWatcherService _watcher;
        private readonly IBatchExecutionService _rcloneRunner;

        public StartupManager(bool generateConfig)
        {
            _logger = new Logger();
            if (generateConfig)
            {
                GenerateConfig();
                Environment.Exit(0);
            }
            _configDTO = LoadConfiguration();
            ILogWriter _logWritter = string.IsNullOrWhiteSpace(_configDTO?.LogPath)
                ? new ConsoleLogWriter()
                : new FileLogWriter(_configDTO.LogPath);
            _logger.SetLogWriter(_logWritter);

            _fileDTOs = new ConcurrentDictionary<string, FileDTO>();
            _rcloneRunner = new RcloneRunService(_logger);
            _processDictionary = InitProcesses();
            _scheduler = new Scheduler(_logger, _processDictionary, _configDTO);
            _watcher = new FileWatcherService(_logger, _fileDTOs, _configDTO.Path);
        }

        private void GenerateConfig()
        {
            new ConfigGenerator(ConfigFileName).GenerateConfig();
            _logger.Log(Enums.LogLevel.Always, $"Example config generated:{ConfigFileName}");
        }
        public void Start()
        {

            _logger.Log(Enums.LogLevel.Always, AppVersion.GetVersion());
            _watcher.Start();
            _logger.Log(Enums.LogLevel.Information, "Watcher started");
            _scheduler.RunStartupSyncIfNeeded();
            _scheduler.SetTimer();
            _logger.Log(Enums.LogLevel.Information, "Controller started");
        }

        private ConfigDTO LoadConfiguration()
        {
            var config = new ConfigLoader(ConfigFileName, _logger).LoadConfig();
            if (config?.Path == null || !config.Path.Any())
            {
                _logger.Log(Enums.LogLevel.Error, "Error in config file");
                Environment.Exit(ExitCodeConfigError);
            }
            return config;
        }

        private Dictionary<Enums.ProcessCode, IRcloneJobService> InitProcesses()
        {
            var filePrepare = new FilePrepareService(_logger, _configDTO.Path, _fileDTOs);
            return new Dictionary<Enums.ProcessCode, IRcloneJobService>
            {
                { Enums.ProcessCode.SyncRclone, new RcloneSyncService(_logger, filePrepare, _fileDTOs,_rcloneRunner ) },
                { Enums.ProcessCode.UpdateRclone, new RcloneUpdateService(_logger) },
                { Enums.ProcessCode.FullSyncRclone, new RcloneFullSyncService(_logger,_rcloneRunner ) },
            };
        }
    }
}