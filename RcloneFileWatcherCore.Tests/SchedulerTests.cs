using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RcloneFileWatcherCore.App;
using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Enums;
using RcloneFileWatcherCore.Infrastructure.Logging.Interfaces;
using RcloneFileWatcherCore.Logic;
using RcloneFileWatcherCore.Logic.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;

namespace RcloneFileWatcherCore.Tests
{
    [TestClass]
    public class SchedulerTests
    {
        // Generous upper bound for the 1000ms timer to elapse; the wait returns as soon
        // as the callback fires, so this only bounds the failure case.
        private const int TimeoutMs = 10000;

        private Mock<ILogger> _loggerMock;
        private Mock<IRcloneJobService> _updateProcessMock;
        private Mock<IRcloneJobService> _syncProcessMock;
        private ConfigDTO _configDTO;
        private Dictionary<Enums.ProcessCode, IRcloneJobService> _processDictionary;

        [TestInitialize]
        public void Setup()
        {
            // Arrange
            _loggerMock = new Mock<ILogger>();
            _updateProcessMock = new Mock<IRcloneJobService>();
            _syncProcessMock = new Mock<IRcloneJobService>();

            _configDTO = new ConfigDTO
            {
                UpdateRclone = new UpdateRcloneDTO
                {
                    Update = true,
                    CheckUpdateHours = 1,
                },
                SyncIntervalSeconds = 1
            };

            _processDictionary = new Dictionary<Enums.ProcessCode, IRcloneJobService>
            {
                { Enums.ProcessCode.UpdateRclone, _updateProcessMock.Object },
                { Enums.ProcessCode.SyncRclone, _syncProcessMock.Object }
            };
        }

        [TestMethod]
        public void Constructor_InitializesTimerCorrectly()
        {
            // Act
            using var scheduler = new Scheduler(_loggerMock.Object, _processDictionary, _configDTO, new object());

            // Assert
            Assert.IsNotNull(scheduler);
        }

        [TestMethod]
        public void SetTimer_EnablesTimer()
        {
            // Arrange
            using var syncCalled = new ManualResetEventSlim();
            _syncProcessMock.Setup(x => x.Execute(It.IsAny<ConfigDTO>())).Callback(() => syncCalled.Set());
            using var scheduler = new Scheduler(_loggerMock.Object, _processDictionary, _configDTO, new object());

            // Act
            scheduler.SetTimer();

            // Assert - Timer is enabled and triggers OnTimedEvent
            Assert.IsTrue(syncCalled.Wait(TimeoutMs), "Timer did not elapse and call the sync process within the timeout.");
            _syncProcessMock.Verify(x => x.Execute(It.IsAny<ConfigDTO>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public void OnTimedEvent_WhenUpdateIsEnabled_CallsUpdateProcess()
        {
            // Arrange
            _configDTO.UpdateRclone.Update = true;
            _configDTO.UpdateRclone.CheckUpdateHours = 0; // Ensure update will trigger
            using var updateCalled = new ManualResetEventSlim();
            _updateProcessMock.Setup(x => x.Execute(It.IsAny<ConfigDTO>())).Callback(() => updateCalled.Set());
            using var scheduler = new Scheduler(_loggerMock.Object, _processDictionary, _configDTO, new object());

            // Act
            scheduler.SetTimer();

            // Assert
            Assert.IsTrue(updateCalled.Wait(TimeoutMs), "Timer did not elapse and call the update process within the timeout.");
            _updateProcessMock.Verify(x => x.Execute(It.IsAny<ConfigDTO>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public void OnTimedEvent_WhenUpdateIsDisabled_DoesNotCallUpdateProcess()
        {
            // Arrange
            _configDTO.UpdateRclone.Update = false;
            using var syncCalled = new ManualResetEventSlim();
            _syncProcessMock.Setup(x => x.Execute(It.IsAny<ConfigDTO>())).Callback(() => syncCalled.Set());
            using var scheduler = new Scheduler(_loggerMock.Object, _processDictionary, _configDTO, new object());

            // Act
            scheduler.SetTimer();

            // Assert - wait for a full timer cycle (sync runs after the update decision in OnTimedEvent),
            // then confirm the disabled update process was never invoked.
            Assert.IsTrue(syncCalled.Wait(TimeoutMs), "Timer did not elapse within the timeout.");
            _updateProcessMock.Verify(x => x.Execute(It.IsAny<ConfigDTO>()), Times.Never);
        }

        [TestMethod]
        public void OnTimedEvent_AlwaysCallsSyncProcess()
        {
            // Arrange
            using var syncCalled = new ManualResetEventSlim();
            _syncProcessMock.Setup(x => x.Execute(It.IsAny<ConfigDTO>())).Callback(() => syncCalled.Set());
            using var scheduler = new Scheduler(_loggerMock.Object, _processDictionary, _configDTO, new object());

            // Act
            scheduler.SetTimer();

            // Assert
            Assert.IsTrue(syncCalled.Wait(TimeoutMs), "Timer did not elapse and call the sync process within the timeout.");
            _syncProcessMock.Verify(x => x.Execute(It.IsAny<ConfigDTO>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public void OnTimedEvent_WhenExceptionOccurs_LogsError()
        {
            // Arrange
            _syncProcessMock.Setup(x => x.Execute(It.IsAny<ConfigDTO>())).Throws<Exception>();
            using var errorLogged = new ManualResetEventSlim();
            _loggerMock.Setup(x => x.Log(LogLevel.Error, It.IsAny<string>(), It.IsAny<Exception>())).Callback(() => errorLogged.Set());
            using var scheduler = new Scheduler(_loggerMock.Object, _processDictionary, _configDTO, new object());

            // Act
            scheduler.SetTimer();

            // Assert
            Assert.IsTrue(errorLogged.Wait(TimeoutMs), "Timer did not elapse and log an error within the timeout.");
            _loggerMock.Verify(x => x.Log(LogLevel.Error, It.IsAny<string>(), It.IsAny<Exception>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public void Dispose_DisposesTimer()
        {
            // Arrange
            var scheduler = new Scheduler(_loggerMock.Object, _processDictionary, _configDTO, new object());

            // Act
            scheduler.Dispose();

            // Assert - no exception should be thrown
            scheduler.Dispose(); // Second dispose should not throw
        }
    }
}