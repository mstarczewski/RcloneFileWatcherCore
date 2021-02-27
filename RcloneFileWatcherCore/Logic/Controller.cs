using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Logic.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace RcloneFileWatcherCore.Logic
{
    class Controller
    {
        private ConcurrentDictionary<string, FileDTO> _fileDTOs;
        private List<PathDTO> _pathDTOs;
        private Watcher _watcher;
        private readonly ProcessRunner _processRunner;
        private ILogger _logger;
        private readonly Scheduler _scheduler;
        public Controller()
        {
            _fileDTOs = new ConcurrentDictionary<string, FileDTO>();
            _pathDTOs = new List<PathDTO>();
            _logger = new ConsoleLogger(); 
            FilePrepare _filePrepare = new FilePrepare(_logger, _pathDTOs, _fileDTOs);
            _watcher = new Watcher(_logger, _fileDTOs, _pathDTOs);
            if (!File.Exists("RcloneFileWatcher.txt"))
            {
                _logger.Write("Config file missing");
                Environment.Exit(0);
            }
            string[] parameters = File.ReadAllLines("RcloneFileWatcher.txt");
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
                    PathDTO _pathDTO = new PathDTO();
                    _pathDTO.WatchingPath = item[0];
                    _pathDTO.RcloneFilesFromPath = item[1];
                    _pathDTO.RcloneBatch = item[2];
                    _pathDTOs.Add(_pathDTO);
                }
            }
            _processRunner = new ProcessRunner(_logger, _filePrepare, _fileDTOs);
            _scheduler = new Scheduler(_logger, _processRunner);

            _watcher.Start();
            _scheduler.SetTimer();
        }
    }
}
