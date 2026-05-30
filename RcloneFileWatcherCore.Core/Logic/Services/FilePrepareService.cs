using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Enums;
using RcloneFileWatcherCore.Infrastructure.Logging.Interfaces;
using RcloneFileWatcherCore.Logic.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RcloneFileWatcherCore.Logic.Services
{
    public class FilePrepareService
    {
        private readonly ConcurrentDictionary<string, FileDTO> _fileList;
        private readonly List<PathDTO> _syncPathDTO;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;

        public FilePrepareService(ILogger logger, List<PathDTO> syncPathDTO, ConcurrentDictionary<string, FileDTO> fileList, IFileSystem fileSystem)
        {
            _logger = logger;
            _syncPathDTO = syncPathDTO;
            _fileList = fileList;
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Collects the list of changed paths to sync for the given source path (applying the
        /// exclude/ready filtering and clearing processed entries). Returns the lines for rclone's
        /// --include-from (empty if nothing changed), or null when the path is unknown. The caller
        /// decides whether to write them to a file or pipe them to rclone via stdin.
        /// </summary>
        public List<string> PrepareFilesToSync(string sourcePath, long lastTimeStamp)
        {
            _logger.Log(LogLevel.Information, $"Prepare files to sync {sourcePath}");
            var rclonePath = _syncPathDTO.FirstOrDefault(x => x.WatchingPath == sourcePath);
            if (rclonePath == null)
            {
                _logger.Log(LogLevel.Error, $"No Path found for source Path: {sourcePath}");
                return null;
            }

            var filesToWrite = new HashSet<string>();

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
                        _fileList.TryRemove(item.Key, out _);
                    }
                }
            }

            return filesToWrite.ToList();
        }

        /// <summary>Writes the --include-from lines to a file (used in script mode and when the
        /// managed command is configured to read the filter from a file rather than stdin).</summary>
        public bool WriteIncludeFromFile(string path, IEnumerable<string> lines)
        {
            try
            {
                File.WriteAllLines(path, lines);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Error writing include-from file {path}", ex);
                return false;
            }
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;
            if (OperatingSystem.IsWindows())
            {
                var normalized = path.Replace(@"\\", "/").Replace(@"\", "/");
                return normalized.StartsWith("/") ? normalized[1..] : normalized;
            }
            return path.StartsWith("/") ? path[1..] : path;
        }

        public bool IsFileFiltered(FileDTO fileDTO, List<string> excludeContains)
        {
            bool fileExists = _fileSystem.FileExists(fileDTO.FullPath);
            bool directoryExists = _fileSystem.DirectoryExists(fileDTO.FullPath);
            bool isCreatedDeletedRenamed = IsCreatedDeletedRenamed(fileDTO.WatcherChangeTypes);
            bool isExcluded = IsExcluded(fileDTO.FullPath, excludeContains);

            if ((IsExistingFile(fileDTO, fileExists)
                 || IsDeletedFile(fileDTO, fileExists)
                 || IsExistingDirectoryWithChange(fileDTO, directoryExists, isCreatedDeletedRenamed)
                 || IsDeletedDirectory(fileDTO, directoryExists))
                && !isExcluded)
            {
                return false;
            }
            return true;
        }

        private static bool IsExistingFile(FileDTO fileDTO, bool fileExists)
        {
            return !fileDTO.NotifyFilters.Equals(NotifyFilters.DirectoryName) && fileExists;
        }

        private static bool IsDeletedFile(FileDTO fileDTO, bool fileExists)
        {
            return !fileExists
                && !fileDTO.NotifyFilters.Equals(NotifyFilters.DirectoryName)
                && fileDTO.WatcherChangeTypes.Equals(WatcherChangeTypes.Deleted);
        }

        private static bool IsExistingDirectoryWithChange(FileDTO fileDTO, bool directoryExists, bool isCreatedDeletedRenamed)
        {
            return directoryExists
                && fileDTO.NotifyFilters.Equals(NotifyFilters.DirectoryName)
                && isCreatedDeletedRenamed;
        }

        private static bool IsDeletedDirectory(FileDTO fileDTO, bool directoryExists)
        {
            return !directoryExists
                && fileDTO.NotifyFilters.Equals(NotifyFilters.DirectoryName)
                && fileDTO.WatcherChangeTypes.Equals(WatcherChangeTypes.Deleted);
        }

        private static bool IsCreatedDeletedRenamed(WatcherChangeTypes changeType)
        {
            return changeType is WatcherChangeTypes.Created
                or WatcherChangeTypes.Deleted
                or WatcherChangeTypes.Renamed;
        }

        private static bool IsExcluded(string fullPath, List<string> excludeContains)
        {
            return excludeContains != null && excludeContains.Any(x => fullPath.Contains(x));
        }

        private bool IsFileReady(string filename)
        {
            try
            {
                _logger.Log(LogLevel.Debug, $"Is File Ready checking: {filename}");
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
