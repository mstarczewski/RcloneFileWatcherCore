using RcloneFileWatcherCore.DTO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RcloneFileWatcherCore.Logic
{
    class Controller
    {
        private ConcurrentDictionary<string, FileDTO> _fileList;
        private List<SyncPathDTO> _syncPathDTO;
        private Watcher _watcherLogic;
        private readonly ProcessLogic _processLogic;
        private Logger _logger;
        private readonly Scheduler _timerLogic;
        public Controller()
        {
            _fileList = new ConcurrentDictionary<string, FileDTO>();
            _syncPathDTO = new List<SyncPathDTO>();
            _logger = new Logger();
            FilePrepare _setUpLogic = new FilePrepare(_logger, _syncPathDTO, _fileList);
            _watcherLogic = new Watcher(_logger, _fileList, _syncPathDTO);
            if (!File.Exists("RcloneFileWatcher.txt"))
            {
                _logger.ConsoleWriter("Config file missing");
                Environment.Exit(0);
            }
            string[] parameters = File.ReadAllLines("RcloneFileWatcher.txt");
            foreach (var param in parameters)
            {
                if (param.ToUpper().Contains("CONSOLEWRITER.OFF"))
                {
                    _logger.WriteToConsole(false);
                }
                else if (param.ToUpper().Contains("CONSOLEWRITER.ON"))
                { _logger.WriteToConsole(true); }
                else
                {
                    var item = param.Split(',');
                    SyncPathDTO _fileFromPath = new SyncPathDTO();
                    _fileFromPath.WatchingPath = item[0];
                    _fileFromPath.RcloneFilesFromPath = item[1];
                    _fileFromPath.RcloneBatch = item[2];
                    _syncPathDTO.Add(_fileFromPath);
                }
            }
            _processLogic = new ProcessLogic(_logger);
            _timerLogic = new Scheduler(_logger, _processLogic, _setUpLogic, _fileList);

            _watcherLogic.Start();
            _timerLogic.SetTimer();
        }
    }
}
