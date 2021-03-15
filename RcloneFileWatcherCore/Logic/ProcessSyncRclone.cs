using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Logic.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;

namespace RcloneFileWatcherCore.Logic
{
    class ProcessSyncRclone:IProcess
    {
        private readonly ILogger _logger;
        private readonly FilePrepare _filePrepare;
        private readonly ConcurrentDictionary<string, FileDTO> _fileDTOs;
        public ProcessSyncRclone(ILogger logger, FilePrepare filePrepare, ConcurrentDictionary<string, FileDTO> fileDTOs)
        {
            _logger = logger;
            _filePrepare = filePrepare;
            _fileDTOs = fileDTOs;
        }
        public bool Start(ConfigDTO configDTO)
        {
            try
            {
                long lastTimeStamp = Globals.TimeStamp.GetTimestampTicks();
                Globals.TimeStamp.SetTimestampTicks();
                var sourePathList = _fileDTOs.Where(x => x.Value.TimeStampTicks <= lastTimeStamp).Select(x => x.Value.SourcePath).Distinct().ToList();
                foreach (var sourcePath in sourePathList)
                {
                    string rcloneBatch = _filePrepare.PrepareFilesToSync(sourcePath, lastTimeStamp);
                    if (!string.IsNullOrWhiteSpace(sourcePath) && !string.IsNullOrWhiteSpace(rcloneBatch))
                    {
                        Process process = new Process();
                        //process.StartInfo.UseShellExecute = true;
                        process.StartInfo.CreateNoWindow = false;

                        _logger.Write("Starting rclone");
                        process.StartInfo.FileName = rcloneBatch;
                        process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        process.Start();
                        process.WaitForExit();
                        _logger.Write("Finished rclone");
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.Write(ex.ToString());
                return false;
            }

        }
    }
}