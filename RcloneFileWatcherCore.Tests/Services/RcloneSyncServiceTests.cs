using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Enums;
using RcloneFileWatcherCore.Infrastructure.Logging.Interfaces;
using RcloneFileWatcherCore.Logic.Interfaces;
using RcloneFileWatcherCore.Logic.Services;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RcloneFileWatcherCore.Tests.Services
{
    [TestClass]
    public class RcloneSyncServiceTests
    {
        private static (RcloneSyncService svc, Mock<IBatchExecutionService> runner, ConcurrentDictionary<string, FileDTO> queue)
            Build(List<PathDTO> paths)
        {
            var logger = new Mock<ILogger>().Object;
            var fileSystem = new Mock<IFileSystem>().Object;
            var queue = new ConcurrentDictionary<string, FileDTO>();
            var prepare = new FilePrepareService(logger, paths, queue, fileSystem);
            var runner = new Mock<IBatchExecutionService>();
            return (new RcloneSyncService(logger, prepare, queue, runner.Object), runner, queue);
        }

        private static PathDTO ManagedPath(string dir) => new PathDTO
        {
            Enabled = true,
            WatchingPath = dir,
            SyncMode = SyncMode.Managed,
            RcloneCommand = new RcloneCommandDTO { Source = dir, Destination = "remote:dst" }
        };

        private static void VerifyNoRclone(Mock<IBatchExecutionService> runner)
        {
            runner.Verify(x => x.ExecuteCommand(It.IsAny<RcloneCommandDTO>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>()), Times.Never);
            runner.Verify(x => x.ExecuteBatch(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void EmptyQueue_DoesNotInvokeRclone()
        {
            var config = new ConfigDTO { Path = new List<PathDTO> { ManagedPath("/watched") } };
            var (svc, runner, _) = Build(config.Path);

            var result = svc.Execute(config);

            Assert.IsTrue(result); // nothing to do is success, not failure
            VerifyNoRclone(runner);
        }

        [TestMethod]
        public void QuietPeriod_DefersWhileChangesAreFresh()
        {
            var config = new ConfigDTO
            {
                QuietPeriodSeconds = 30,
                QuietPeriodMaxWaitSeconds = 300,
                Path = new List<PathDTO> { ManagedPath("/watched") }
            };
            var (svc, runner, queue) = Build(config.Path);

            // A change enqueued just now → within the quiet window → sync is deferred.
            queue["k"] = new FileDTO
            {
                SourcePath = "/watched",
                PathPreparedToSync = "x.txt",
                FullPath = "/watched/x.txt",
                EnqueuedUtcTicks = System.DateTime.UtcNow.Ticks,
                TimeStampTicks = 1
            };

            var result = svc.Execute(config);

            Assert.IsTrue(result);
            VerifyNoRclone(runner);
        }

        [TestMethod]
        public void QueuedChangeForUnknownPath_DoesNotInvokeRclone()
        {
            var config = new ConfigDTO { Path = new List<PathDTO> { ManagedPath("/watched") } };
            var (svc, runner, queue) = Build(config.Path);

            // A change whose source path is not configured must not trigger any rclone run.
            queue["k"] = new FileDTO { SourcePath = "/some/other/path", TimeStampTicks = 1 };

            var result = svc.Execute(config);

            Assert.IsTrue(result);
            VerifyNoRclone(runner);
        }
    }
}
