using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Logic.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RcloneFileWatcherCore.Logic
{
    class Watcher
    {
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, FileDTO> _fileList;
        private readonly List<PathDTO> _FilePathDTO;
        private List<FileSystemWatcher> _fileWatcherList = new List<FileSystemWatcher>();
        public Watcher(ILogger logger, ConcurrentDictionary<string, FileDTO> fileList, List<PathDTO> FilePathDTO)
        {
            _logger = logger;
            _fileList = fileList;
            _FilePathDTO = FilePathDTO;
            Start();
        }

        public void Start()
        {
            foreach (var item in _FilePathDTO)
            {
                var _fileWatcher = new System.IO.FileSystemWatcher();
                _fileWatcher.Path = item.WatchingPath;
                _fileWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
                _fileWatcher.IncludeSubdirectories = true;
                _fileWatcher.EnableRaisingEvents = true;
                _fileWatcher.InternalBufferSize = 8192 * 8;
                _fileWatcher.Changed += OnChanged;
                _fileWatcher.Created += OnChanged;
                _fileWatcher.Deleted += OnChanged;
                _fileWatcher.Renamed += OnChanged;

                _fileWatcherList.Add(_fileWatcher);

                var _dirWatcher = new System.IO.FileSystemWatcher();
                _dirWatcher.Path = item.WatchingPath;
                _dirWatcher.NotifyFilter = NotifyFilters.DirectoryName;
                _dirWatcher.IncludeSubdirectories = true;
                _dirWatcher.EnableRaisingEvents = true;
                _dirWatcher.InternalBufferSize = 8192 * 8;
                _dirWatcher.Created += OnChanged;
                _dirWatcher.Deleted += OnChanged;
                _dirWatcher.Renamed += OnChanged;
                _fileWatcherList.Add(_dirWatcher);
            }
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            var sourceFileWatcher = (FileSystemWatcher)source;
            //ConsoleWriter($" File {sourceFileWatcher.NotifyFilter}:  {e.FullPath.Substring(sourceFileWatcher.Path.Length)} {e.ChangeType}");
            _fileList.TryAdd($@"{sourceFileWatcher.Path};{e.FullPath.Substring(sourceFileWatcher.Path.Length)}",
                             new FileDTO
                             {
                                 SourcePath = sourceFileWatcher.Path,
                                 PathPreparedToSync = e.FullPath.Substring(sourceFileWatcher.Path.Length) + (sourceFileWatcher.NotifyFilter == NotifyFilters.DirectoryName ? @"/**" : ""),
                                 FullPath = e.FullPath,
                                 NotifyFilters = sourceFileWatcher.NotifyFilter,
                                 WatcherChangeTypes = e.ChangeType
                             });
            _logger.Write($"Action: {e.FullPath.Substring(sourceFileWatcher.Path.Length) + (sourceFileWatcher.NotifyFilter.Equals(NotifyFilters.DirectoryName) ? @"/**" : "")}");
        }
    }
}
