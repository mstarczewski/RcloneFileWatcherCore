using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Enums;
using RcloneFileWatcherCore.Infrastructure.Logging.Interfaces;
using RcloneFileWatcherCore.Logic.Services;
using RcloneFileWatcherCore.Logic.Interfaces;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace RcloneFileWatcherCore.Tests.Services
{
    [TestClass]
    public class FilePrepareServiceTests
    {
        private Mock<ILogger> _loggerMock;
        private Mock<IFileSystem> _fileSystemMock;
        private ConcurrentDictionary<string, FileDTO> _fileList;
        private List<PathDTO> _syncPathDTO;
        private FilePrepareService _service;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger>();
            _fileSystemMock = new Mock<IFileSystem>();
            _fileList = new ConcurrentDictionary<string, FileDTO>();
            _syncPathDTO = new List<PathDTO>
            {
                new PathDTO
                {
                    WatchingPath = "d:\\TestPath",
                    RcloneFilesFromPath = "d:\\files-from-test.txt",
                    RcloneBatch = "d:\\rclone_test.bat",
                    ExcludeContains = new List<string> { ".tmp", ".temp" }
                }
            };

            _service = new FilePrepareService(_loggerMock.Object, _syncPathDTO, _fileList, _fileSystemMock.Object);
        }

        [TestMethod]
        public void IsFileFiltered_WhenFileExistsAndIsNotDirectory_ReturnsFalse()
        {
            // Arrange
            var fileDto = new FileDTO
            {
                FullPath = "d:\\TestPath\\test.txt",
                NotifyFilters = NotifyFilters.FileName,
                WatcherChangeTypes = WatcherChangeTypes.Created
            };

            _fileSystemMock.Setup(x => x.FileExists(fileDto.FullPath)).Returns(true);
            _fileSystemMock.Setup(x => x.DirectoryExists(fileDto.FullPath)).Returns(false);

            // Act & Assert
            Assert.IsFalse(_service.IsFileFiltered(fileDto, new List<string>()));
        }

        [TestMethod]
        public void IsFileFiltered_WhenFileIsExcluded_ReturnsTrue()
        {
            // Arrange
            var fileDto = new FileDTO
            {
                FullPath = "d:\\TestPath\\test.tmp",
                NotifyFilters = NotifyFilters.FileName,
                WatcherChangeTypes = WatcherChangeTypes.Created
            };

            _fileSystemMock.Setup(x => x.FileExists(fileDto.FullPath)).Returns(true);
            _fileSystemMock.Setup(x => x.DirectoryExists(fileDto.FullPath)).Returns(false);

            // Act & Assert
            Assert.IsTrue(_service.IsFileFiltered(fileDto, new List<string> { ".tmp" }));
        }

        [TestMethod]
        public void IsFileFiltered_WhenDeletedFileAndNotDirectory_ReturnsFalse()
        {
            // Arrange
            var fileDto = new FileDTO
            {
                FullPath = "d:\\TestPath\\deleted.txt",
                NotifyFilters = NotifyFilters.FileName,
                WatcherChangeTypes = WatcherChangeTypes.Deleted
            };

            _fileSystemMock.Setup(x => x.FileExists(fileDto.FullPath)).Returns(false);
            _fileSystemMock.Setup(x => x.DirectoryExists(fileDto.FullPath)).Returns(false);

            // Act & Assert
            Assert.IsFalse(_service.IsFileFiltered(fileDto, new List<string>()));
        }

        [TestMethod]
        public void IsFileFiltered_WhenDirectoryAndCreated_ReturnsFalse()
        {
            // Arrange
            var fileDto = new FileDTO
            {
                FullPath = "d:\\TestPath\\NewDirectory",
                NotifyFilters = NotifyFilters.DirectoryName,
                WatcherChangeTypes = WatcherChangeTypes.Created
            };

            _fileSystemMock.Setup(x => x.FileExists(fileDto.FullPath)).Returns(false);
            _fileSystemMock.Setup(x => x.DirectoryExists(fileDto.FullPath)).Returns(true);

            // Act & Assert
            Assert.IsFalse(_service.IsFileFiltered(fileDto, new List<string>()));
        }

        [TestMethod]
        public void IsFileFiltered_WhenDirectoryDeletedAndNotExists_ReturnsFalse()
        {
            // Arrange
            var fileDto = new FileDTO
            {
                FullPath = "d:\\TestPath\\DeletedDirectory",
                NotifyFilters = NotifyFilters.DirectoryName,
                WatcherChangeTypes = WatcherChangeTypes.Deleted
            };

            _fileSystemMock.Setup(x => x.FileExists(fileDto.FullPath)).Returns(false);
            _fileSystemMock.Setup(x => x.DirectoryExists(fileDto.FullPath)).Returns(false);

            // Act & Assert
            Assert.IsFalse(_service.IsFileFiltered(fileDto, new List<string>()));
        }

        [TestMethod]
        public void PrepareFilesToSync_WhenNoPathFound_ReturnsNull()
        {
            // Arrange
            var sourcePath = "d:\\NonExistentPath";

            // Act
            var result = _service.PrepareFilesToSync(sourcePath, 0);

            // Assert
            Assert.IsNull(result);
            _loggerMock.Verify(x => x.Log(LogLevel.Error, It.IsAny<string>(), null), Times.Once);
        }
    }
}