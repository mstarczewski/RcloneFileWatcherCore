using RcloneFileWatcherCore.DTO;
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
        internal void GenerateConfig()
        {
            var conf = new ConfigDTO
            {
                ConsoleWriter = false,
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
                        RcloneBatch = "@d:\rclone_Test.bat1",
                        RcloneFilesFromPath = @"d:\files-from-test1.txt",
                        WatchingPath = @"d:\Test\"
                    }
                }),
                UpdateRclone = new UpdateRcloneDTO
                {
                    Update = true,
                    RclonePath = @".\rclone.exe",
                    RcloneWebsiteCurrentVersionAddress = "https://downloads.rclone.org/rclone-current-windows-amd64.zip",
                    ChceckUpdateHours = 1
                }
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