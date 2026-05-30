using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RcloneFileWatcherCore.App;
using RcloneFileWatcherCore.Config;
using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Infrastructure.Logging;
using RcloneFileWatcherCore.Logic.Interfaces;
using RcloneFileWatcherCore.Logic.Services;
using RcloneFileWatcherCore.Status;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace RcloneFileWatcherCore.Tests.App
{
    [TestClass]
    public class RuntimeReloadTests
    {
        [TestMethod]
        public void SavingConfig_ReloadsWatcherWithNewPaths_WithoutRestart()
        {
            // Arrange
            var logger = new Logger();
            var fileSystem = new FileSystemService();
            var runner = new Mock<IBatchExecutionService>().Object;
            var fileDTOs = new ConcurrentDictionary<string, FileDTO>();
            var status = new StatusService(fileDTOs);

            var watchDir = Directory.CreateTempSubdirectory().FullName;
            var configFile = Path.GetTempFileName();

            var configService = new ConfigService(new ConfigDTO(), configFile, logger);
            using var controller = new RuntimeController(logger, fileSystem, runner, status, configService, fileDTOs);

            // Act: start with no paths, then save a config that adds one watched directory.
            controller.Start();
            var before = status.GetStatus();

            configService.Save(new ConfigDTO
            {
                SyncIntervalSeconds = 5,
                Path = new List<PathDTO>
                {
                    new PathDTO
                    {
                        WatchingPath = watchDir,
                        RcloneFilesFromPath = Path.Combine(watchDir, "files-from.txt")
                    }
                }
            });
            var after = status.GetStatus();

            // Assert: watcher kept running and is now watching the new path; file was persisted.
            Assert.AreEqual(0, before.WatchedPaths.Count);
            Assert.IsTrue(after.WatcherRunning);
            Assert.AreEqual(1, after.WatchedPaths.Count);
            Assert.AreEqual(watchDir, after.WatchedPaths[0]);
            Assert.IsTrue(File.Exists(configFile));

            // Cleanup
            controller.Stop();
            Assert.IsFalse(status.GetStatus().WatcherRunning);

            File.Delete(configFile);
            Directory.Delete(watchDir, recursive: true);
        }
    }
}
