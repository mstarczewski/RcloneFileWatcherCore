using Microsoft.VisualStudio.TestTools.UnitTesting;
using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Logic.Rclone;
using System.Linq;

namespace RcloneFileWatcherCore.Tests.Logic
{
    [TestClass]
    public class RcloneCommandBuilderTests
    {
        [TestMethod]
        public void BuildArguments_IncludesCoreParts_AndInjectsIncludeFrom()
        {
            var command = new RcloneCommandDTO
            {
                Command = "sync",
                Source = "/mnt/data",
                Destination = "remote:backup",
                Transfers = 6,
                CreateEmptySrcDirs = true
            };

            var args = RcloneCommandBuilder.BuildArguments(command, "/var/log/files-from.txt").ToList();

            Assert.AreEqual("sync", args[0]);
            Assert.AreEqual("/mnt/data", args[1]);
            Assert.AreEqual("remote:backup", args[2]);
            CollectionAssert.Contains(args, "--include-from");
            CollectionAssert.Contains(args, "/var/log/files-from.txt");
            CollectionAssert.Contains(args, "--transfers");
            CollectionAssert.Contains(args, "6");
            CollectionAssert.Contains(args, "--create-empty-src-dirs");
        }

        [TestMethod]
        public void BuildArguments_OmitsEmptyAndZeroFields()
        {
            var command = new RcloneCommandDTO
            {
                Command = "copy",
                Source = "/data",
                Destination = "remote:x",
                IncludeFrom = false,
                Transfers = 0,
                BwLimit = ""
            };

            var args = RcloneCommandBuilder.BuildArguments(command, "/ignored.txt").ToList();

            CollectionAssert.DoesNotContain(args, "--include-from");
            CollectionAssert.DoesNotContain(args, "--transfers");
            CollectionAssert.DoesNotContain(args, "--bwlimit");
        }

        [TestMethod]
        public void BuildArguments_SplitsExtraArgsIntoTokens()
        {
            var command = new RcloneCommandDTO
            {
                Source = "/data",
                Destination = "remote:x",
                ExtraArgs = "--use-mmap\n--fast-list"
            };

            var args = RcloneCommandBuilder.BuildArguments(command, null).ToList();

            CollectionAssert.Contains(args, "--use-mmap");
            CollectionAssert.Contains(args, "--fast-list");
        }

        [TestMethod]
        public void BuildPreview_QuotesTokensWithSpaces_AndStartsWithExe()
        {
            var command = new RcloneCommandDTO
            {
                RclonePath = "/opt/rclone/rclone",
                Command = "sync",
                Source = "/mnt/My Files",
                Destination = "remote:x",
                Suffix = " [backup]"
            };

            var preview = RcloneCommandBuilder.BuildPreview(command, null);

            StringAssert.StartsWith(preview, "/opt/rclone/rclone sync");
            StringAssert.Contains(preview, "\"/mnt/My Files\"");
            StringAssert.Contains(preview, "--suffix \" [backup]\"");
        }
    }
}
