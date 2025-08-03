using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Enums;
using RcloneFileWatcherCore.Logic.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RcloneFileWatcherCore.Logic
{
    public class FilePrepare
    {
        private readonly ConcurrentDictionary<string, FileDTO> _fileList;
        private readonly List<PathDTO> _syncPathDTO;
        private readonly ILogger _logger;

        public FilePrepare(ILogger logger, List<PathDTO> syncPathDTO, ConcurrentDictionary<string, FileDTO> fileList)
        {
            _logger = logger;
            _syncPathDTO = syncPathDTO;
            _fileList = fileList;
        }

        public string PrepareFilesToSync(string sourcePath, long lastTimeStamp)
        {
            _logger.Log(LogLevel.Information, $"Prepare files to sunc {sourcePath}");
            var rclonePath = _syncPathDTO.FirstOrDefault(x => x.WatchingPath == sourcePath);
            if (rclonePath == null)
            {
                _logger.Log(LogLevel.Error, $"No Path found for source Path: {sourcePath}");
                return null;
            }

            string rcloneBatch = rclonePath.RcloneBatch;
            var filesToWrite = new HashSet<string>();
            int removeCount = 0;

            var items = _fileList
                .Where(x => x.Value.SourcePath == sourcePath && x.Value.TimeStampTicks <= lastTimeStamp)
                .ToList();

            foreach (var item in items)
            {
                if (IsFileFiltered(item.Value, rclonePath.ExcludeContains))
                {
                    _fileList.TryRemove(item.Key, out _);
                    continue;
                }

                if (IsFileReady(item.Value.FullPath))
                {
                    string fileNameFinal = NormalizePath(item.Value.PathPreparedToSync);
                    if (!string.IsNullOrEmpty(fileNameFinal))
                    {
                        filesToWrite.Add(fileNameFinal);
                        if (_fileList.TryRemove(item.Key, out _))
                            removeCount++;
                    }
                }
            }

            try
            {
                File.WriteAllLines(rclonePath.RcloneFilesFromPath, filesToWrite);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Information, $"Error writing files", ex);
                return null;
            }

            return removeCount > 0 ? rcloneBatch : null;
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            var normalized = path.Replace(@"\\", "/").Replace(@"\", "/");
            return normalized.StartsWith("/") ? normalized[1..] : normalized;
        }

        private static bool IsFileFiltered(FileDTO fileDTO, List<string> excludeContains)
        {
            bool fileExists = File.Exists(fileDTO.FullPath);
            bool directoryExists = Directory.Exists(fileDTO.FullPath);
            bool isCreatedDeletedRenamed = fileDTO.WatcherChangeTypes is WatcherChangeTypes.Created
                or WatcherChangeTypes.Deleted
                or WatcherChangeTypes.Renamed;

            bool isExcluded = excludeContains != null && excludeContains.Any(x => fileDTO.FullPath.Contains(x));

            if (
                (
                    (!fileDTO.NotifyFilters.Equals(NotifyFilters.DirectoryName) && fileExists)
                    || (!fileExists && !fileDTO.NotifyFilters.Equals(NotifyFilters.DirectoryName) && fileDTO.WatcherChangeTypes.Equals(WatcherChangeTypes.Deleted))
                    || (directoryExists && fileDTO.NotifyFilters.Equals(NotifyFilters.DirectoryName) && isCreatedDeletedRenamed)
                    || (!directoryExists && fileDTO.NotifyFilters.Equals(NotifyFilters.DirectoryName) && fileDTO.WatcherChangeTypes.Equals(WatcherChangeTypes.Deleted))
                )
                && !isExcluded
            )
            {
                return false;
            }
            return true;
        }

        private bool IsFileReady(string filename)
        {
            try
            {
                _logger.Log(LogLevel.Information, $"Is File Ready checking: {filename}");
                if (Directory.Exists(filename))
                    return true;
                if (!File.Exists(filename))
                    return true;
                using var inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None);
                return inputStream.Length >= 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
