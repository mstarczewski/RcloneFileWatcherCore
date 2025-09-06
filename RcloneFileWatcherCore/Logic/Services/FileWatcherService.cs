using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Infrastructure.Logging.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace RcloneFileWatcherCore.Logic.Services
{
    class FileWatcherService
    {
        private const int BufferSize = 65536;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, FileDTO> _fileDTOs;
        private readonly List<PathDTO> _filePathDTO;
        private readonly List<FileSystemWatcher> _fileWatcherList = new();

        public FileWatcherService(ILogger logger, ConcurrentDictionary<string, FileDTO> fileDTOs, List<PathDTO> filePathDTO)
        {
            _logger = logger;
            _fileDTOs = fileDTOs;
            _filePathDTO = filePathDTO;
        }

        public void Start()
        {
            foreach (var item in _filePathDTO)
            {
                _fileWatcherList.Add(CreateAndRegisterWatcher(item.WatchingPath, NotifyFilters.FileName | NotifyFilters.LastWrite, true));
                _fileWatcherList.Add(CreateAndRegisterWatcher(item.WatchingPath, NotifyFilters.DirectoryName, false));
                _logger.Log(Enums.LogLevel.Information, $"Watching: {item.WatchingPath}");
            }
        }

        private FileSystemWatcher CreateAndRegisterWatcher(string path, NotifyFilters filters, bool isFileWatcher)
        {
            var watcher = new FileSystemWatcher
            {
                Path = path,
                NotifyFilter = filters,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true,
                InternalBufferSize = BufferSize
            };

            watcher.Created += OnChanged;
            watcher.Deleted += OnChanged;
            watcher.Renamed += OnRenamed;
            watcher.Renamed += OnChanged;

            if (isFileWatcher)
                watcher.Changed += OnChanged;

            watcher.Error += (s, e) =>
               _logger.Log(Enums.LogLevel.Error, $"Watcher error at {path}, e.GetException()");

            return watcher;
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            AddRenamedOldPathToCollection(e, (FileSystemWatcher)sender, Globals.TimeStamp.GetTimestampTicks());
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            AddChangesToCollection(e, (FileSystemWatcher)sender, Globals.TimeStamp.GetTimestampTicks());
        }

        private void AddRenamedOldPathToCollection(RenamedEventArgs e, FileSystemWatcher watcher, long timestamp)
        {
            var isDir = watcher.NotifyFilter.HasFlag(NotifyFilters.DirectoryName);
            var relativeOldPath = GetRelativePath(watcher.Path, e.OldFullPath);
            var syncPath = isDir ? $"{relativeOldPath}/**" : relativeOldPath;
            var key = $"{watcher.Path};{GetRelativePath(watcher.Path, e.FullPath)};{timestamp};{WatcherChangeTypes.Deleted}";

            _fileDTOs.TryAdd(key, new FileDTO
            {
                SourcePath = watcher.Path,
                PathPreparedToSync = syncPath,
                FullPath = e.OldFullPath,
                NotifyFilters = watcher.NotifyFilter,
                WatcherChangeTypes = WatcherChangeTypes.Deleted,
                TimeStampTicks = timestamp
            });

            _logger.Log(Enums.LogLevel.Debug, $"Action:{WatcherChangeTypes.Deleted} - {syncPath}");
        }

        private void AddChangesToCollection(FileSystemEventArgs e, FileSystemWatcher watcher, long timestamp)
        {
            var isDir = watcher.NotifyFilter.HasFlag(NotifyFilters.DirectoryName);
            var relativePath = GetRelativePath(watcher.Path, e.FullPath);
            var syncPath = isDir ? $"{relativePath}/**" : relativePath;
            var key = $"{watcher.Path};{relativePath};{timestamp};{e.ChangeType}";

            _fileDTOs.TryAdd(key, new FileDTO
            {
                SourcePath = watcher.Path,
                PathPreparedToSync = syncPath,
                FullPath = e.FullPath,
                NotifyFilters = watcher.NotifyFilter,
                WatcherChangeTypes = e.ChangeType,
                TimeStampTicks = timestamp
            });
            _logger.Log(Enums.LogLevel.Debug, $"Action:{e.ChangeType} - {syncPath}");
        }

        private static string GetRelativePath(string basePath, string fullPath)
        {
            var path = Path.GetRelativePath(basePath, fullPath);
            if (OperatingSystem.IsWindows())
            {
                return path.Replace('\\', '/');
            }
            return path;
        }
    }
}
