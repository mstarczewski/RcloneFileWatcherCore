using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Logic.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RcloneFileWatcherCore.Logic
{
    class FilePrepare
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
            int removeCount = 0;
            _logger.Write(sourcePath);
            var rclonePath = _syncPathDTO.Where(x => x.WatchingPath == sourcePath).FirstOrDefault();
            string rcloneBatch = rclonePath.RcloneBatch;
            HashSet<string> filesToWrite = new HashSet<string>();
            foreach (var item in _fileList.Where(x => x.Value.SourcePath == sourcePath && x.Value.TimeStampTicks <= lastTimeStamp))
            {
                if (IsFileToFiltered(item.Value, rclonePath.ExcludeContains))
                {
                    _fileList.TryRemove(item.Key, out _);
                }
                else if (IsFileReady(item.Value.FullPath))
                {
                    string fileNameFinal = (item.Value.PathPreparedToSync.Replace(@"\\", @"/").Replace(@"\", @"/"));
                    if (fileNameFinal.Length > 0 && fileNameFinal[0] == '/')
                    {
                        fileNameFinal = fileNameFinal.Substring(1);
                    }
                    filesToWrite.Add(fileNameFinal);
                    removeCount += _fileList.TryRemove(item.Key, out _) ? 1 : 0;
                }
            }
            File.WriteAllLines(rclonePath.RcloneFilesFromPath, filesToWrite);
            return removeCount > 0 ? rcloneBatch : null;
        }

        private static bool IsFileToFiltered(FileDTO fileDTO, List<string> excludeContains)
        {
            bool fileExists = File.Exists(fileDTO.FullPath);
            bool directoryExists = Directory.Exists(fileDTO.FullPath);
            bool watcherChangeTypesCreatedDeletedeRenamed = fileDTO.WatcherChangeTypes.Equals(WatcherChangeTypes.Created)
                                                            || fileDTO.WatcherChangeTypes.Equals(WatcherChangeTypes.Deleted)
                                                            || fileDTO.WatcherChangeTypes.Equals(WatcherChangeTypes.Renamed);

            if (((!fileDTO.NotifyFilters.Equals(NotifyFilters.DirectoryName) && fileExists)
              || (!fileExists && (!fileDTO.NotifyFilters.Equals(NotifyFilters.DirectoryName) && fileDTO.WatcherChangeTypes.Equals(WatcherChangeTypes.Deleted)))
              || (directoryExists && (fileDTO.NotifyFilters.Equals(NotifyFilters.DirectoryName) && (watcherChangeTypesCreatedDeletedeRenamed)))
              || (!directoryExists && (fileDTO.NotifyFilters.Equals(NotifyFilters.DirectoryName) && fileDTO.WatcherChangeTypes.Equals(WatcherChangeTypes.Deleted))))
              && !(excludeContains.Any(x=>fileDTO.FullPath.Contains(x)))) 
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        private bool IsFileReady(string filename)
        {
            try
            {
                _logger.Write($"IsFileReady checking: {filename}");
                if (Directory.Exists(filename))
                {
                    return true;
                }
                if (!File.Exists(filename))
                {
                    return true;
                }
                using (FileStream inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    return inputStream.Length >= 0;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
