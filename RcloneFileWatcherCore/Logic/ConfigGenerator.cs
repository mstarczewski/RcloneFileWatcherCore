using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Logic.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace RcloneFileWatcherCore.Logic
{
    // JSON example config generator 
    internal class ConfigGenerator
    {
        private readonly string _fileName;
        public ConfigGenerator(string fileName)
        {
            _fileName = fileName;
        }
        public void GenerateConfig()
        {
            var conf = new ConfigDTO
            {
                LogLevel = "Information|Error",
                Path = new List<PathDTO>(new List<PathDTO>
                {
                    new PathDTO
                    {
                        ExcludeContains = new List<string>(new string[]
                           {
                               ".tmp",
                               ".drivedownload1"
                           }),
                        RcloneBatch = @"d:\rclone_Test.bat",
                        RcloneFilesFromPath = @"d:\files-from-test.txt",
                        WatchingPath = @"d:\Test\"
                    },
                    new PathDTO
                    {
                        ExcludeContains = new List<string>(new string[]
                           {
                               ".tmp",
                               ".drivedownload1"
                           }),
                        RcloneBatch = "@d:\rclone_Test1.bat",
                        RcloneFilesFromPath = @"d:\files-from-test1.txt",
                        WatchingPath = @"d:\Test\"
                    }
                }),
                UpdateRclone = new UpdateRcloneDTO
                {
                    Update = true,
                    RclonePath = @".\rclone.exe",
                    CheckUpdateHours = 350
                },
                SyncIntervalSeconds = 60,
                RunOneTimeFullStartupSync = true,
                RunOneTimeFullStartupSyncBatch = @"rclone_startupsync.bat"

            };
            var options = new JsonSerializerOptions()
            {
                WriteIndented = true
            };
            string jsonString = JsonSerializer.Serialize(conf, options);
            File.WriteAllText(_fileName, jsonString);
        }
    }
}