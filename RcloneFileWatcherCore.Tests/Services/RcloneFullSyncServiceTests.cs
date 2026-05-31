using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Enums;
using RcloneFileWatcherCore.Infrastructure.Logging.Interfaces;
using RcloneFileWatcherCore.Logic.Interfaces;
using RcloneFileWatcherCore.Logic.Services;
using System.Collections.Generic;

namespace RcloneFileWatcherCore.Tests.Services
{
    [TestClass]
    public class RcloneFullSyncServiceTests
    {
        private static RcloneFullSyncService Create(Mock<IBatchExecutionService> runner)
            => new RcloneFullSyncService(new Mock<ILogger>().Object, runner.Object);

        [TestMethod]
        public void Managed_RunsEachCommandWithoutIncludeFrom()
        {
            var runner = new Mock<IBatchExecutionService>();
            var service = Create(runner);
            var config = new ConfigDTO
            {
                FullSyncMode = SyncMode.Managed,
                FullSyncCommands = new List<RcloneCommandDTO>
                {
                    new RcloneCommandDTO { Command = "sync", Source = "/a", Destination = "r:a" },
                    new RcloneCommandDTO { Command = "sync", Source = "/b", Destination = "r:b" },
                }
            };

            var result = service.Execute(config);

            Assert.IsTrue(result);
            // Full sync passes a null include-from path (whole-tree reconcile) for each command.
            runner.Verify(x => x.ExecuteCommand(It.IsAny<RcloneCommandDTO>(), null, It.IsAny<IReadOnlyList<string>>()), Times.Exactly(2));
            runner.Verify(x => x.ExecuteBatch(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void Managed_NoCommands_DoesNothing()
        {
            var runner = new Mock<IBatchExecutionService>();
            var service = Create(runner);
            var config = new ConfigDTO { FullSyncMode = SyncMode.Managed, FullSyncCommands = new List<RcloneCommandDTO>() };

            var result = service.Execute(config);

            Assert.IsFalse(result);
            runner.Verify(x => x.ExecuteCommand(It.IsAny<RcloneCommandDTO>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>()), Times.Never);
        }

        [TestMethod]
        public void Managed_SkipsDisabledCommands()
        {
            var runner = new Mock<IBatchExecutionService>();
            var service = Create(runner);
            var config = new ConfigDTO
            {
                FullSyncMode = SyncMode.Managed,
                FullSyncCommands = new List<RcloneCommandDTO>
                {
                    new RcloneCommandDTO { Command = "sync", Source = "/a", Destination = "r:a", Enabled = true },
                    new RcloneCommandDTO { Command = "sync", Source = "/b", Destination = "r:b", Enabled = false },
                }
            };

            var result = service.Execute(config);

            Assert.IsTrue(result);
            // Only the enabled command runs.
            runner.Verify(x => x.ExecuteCommand(It.IsAny<RcloneCommandDTO>(), null, It.IsAny<IReadOnlyList<string>>()), Times.Once);
        }

        [TestMethod]
        public void Managed_AllDisabled_DoesNothing()
        {
            var runner = new Mock<IBatchExecutionService>();
            var service = Create(runner);
            var config = new ConfigDTO
            {
                FullSyncMode = SyncMode.Managed,
                FullSyncCommands = new List<RcloneCommandDTO>
                {
                    new RcloneCommandDTO { Command = "sync", Source = "/a", Enabled = false },
                }
            };

            var result = service.Execute(config);

            Assert.IsFalse(result);
            runner.Verify(x => x.ExecuteCommand(It.IsAny<RcloneCommandDTO>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>()), Times.Never);
        }

        [TestMethod]
        public void Script_RunsBatch()
        {
            var runner = new Mock<IBatchExecutionService>();
            var service = Create(runner);
            var config = new ConfigDTO
            {
                FullSyncMode = SyncMode.Script,
                RunOneTimeFullStartupSyncBatch = "/opt/fullsync.sh"
            };

            var result = service.Execute(config);

            Assert.IsTrue(result);
            runner.Verify(x => x.ExecuteBatch("/opt/fullsync.sh"), Times.Once);
            runner.Verify(x => x.ExecuteCommand(It.IsAny<RcloneCommandDTO>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>()), Times.Never);
        }
    }
}
