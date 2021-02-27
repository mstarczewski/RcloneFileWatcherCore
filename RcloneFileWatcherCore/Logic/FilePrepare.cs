using RcloneFileWatcherCore.DTO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RcloneFileWatcherCore.Logic
{
    class FilePrepare
    {
        private readonly ConcurrentDictionary<string, FileDTO> _fileList;
        private readonly List<SyncPathDTO> _syncPathDTO;
        private readonly Logger _logger;
        
        public FilePrepare(Logger logger, List<SyncPathDTO> syncPathDTO, ConcurrentDictionary<string, FileDTO> fileList)
        {
            _logger = logger;
            _syncPathDTO = syncPathDTO;
            _fileList = fileList;
        }

        public string PrepareFilesToSync(string sourcePath)
        {
            FileDTO fileRemovedDTO = new FileDTO();
            _logger.ConsoleWriter(sourcePath);
            var rclonePath = _syncPathDTO.Where(x => x.WatchingPath == sourcePath).FirstOrDefault();
            string rcloneBatch = rclonePath.RcloneBatch;
            using (StreamWriter sw = new StreamWriter(rclonePath.RcloneFilesFromPath))
            {
                foreach (var item in _fileList.Where(x => x.Value.SourcePath == sourcePath))
                {
                    if (IsFileToFiltered(item.Value))
                    {
                        _fileList.TryRemove(item.Key, out fileRemovedDTO);
                    }
                    else if (IsFileReady(item.Value.FullPath))
                    {
                        string fileNameFinal = (item.Value.PathPreparedToSync.Replace(@"\\", @"/").Replace(@"\", @"/"));
                        if (fileNameFinal.Length > 0 && fileNameFinal[0] == '/')
                        {
                            fileNameFinal = fileNameFinal.Substring(1);
                        }
                        sw.WriteLine(fileNameFinal);
                        _fileList.TryRemove(item.Key, out fileRemovedDTO); //if false do nothing
                    }
                }
                sw.Flush();
            }
            return rcloneBatch;
        }

        private static bool IsFileToFiltered(FileDTO fileDTO)
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
              && !(fileDTO.FullPath.Contains(".tmp.drivedownload")))
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
                _logger.ConsoleWriter($"IsFileReady checking: {filename}");
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
