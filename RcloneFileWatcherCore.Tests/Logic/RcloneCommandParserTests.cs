using Microsoft.VisualStudio.TestTools.UnitTesting;
using RcloneFileWatcherCore.Logic.Rclone;

namespace RcloneFileWatcherCore.Tests.Logic
{
    [TestClass]
    public class RcloneCommandParserTests
    {
        // Mirrors Examples/rclone_livesync_shared.sh.
        private const string BashScript = """
#!/bin/bash
datetime=$(date '+%Y-%m-%d-%H-%M-%S')
year=$(date '+%Y')
mkdir -p /var/log/rclone
/opt/rclone/rclone sync --include-from /var/log/rclone/files-from-shared.txt /mnt/samba/Shared/Wspolny pcloudcryptDaily:Shared/Wspolny \
  --retries-sleep 1m \
  --retries 30 \
  --bwlimit 15M:off \
  --create-empty-src-dirs \
  --backup-dir "pcloudcryptDaily:\$Archive/Shared/${year}/Wspolny" \
  --suffix " [${datetime}]" \
  --log-file=/var/log/rclone/livesync_shared_${datetime}.log \
  --log-level INFO
""";

        [TestMethod]
        public void Parse_BashLiveSyncScript_MapsAllKnownFields()
        {
            var cmd = RcloneCommandParser.Parse(BashScript);

            Assert.AreEqual("/opt/rclone/rclone", cmd.RclonePath);
            Assert.AreEqual("sync", cmd.Command);
            Assert.IsTrue(cmd.IncludeFrom);
            Assert.AreEqual("/mnt/samba/Shared/Wspolny", cmd.Source);
            Assert.AreEqual("pcloudcryptDaily:Shared/Wspolny", cmd.Destination);
            Assert.AreEqual("1m", cmd.RetriesSleep);
            Assert.AreEqual(30, cmd.Retries);
            Assert.AreEqual("15M:off", cmd.BwLimit);
            Assert.IsTrue(cmd.CreateEmptySrcDirs);
            Assert.AreEqual("pcloudcryptDaily:$Archive/Shared/{year}/Wspolny", cmd.BackupDir);
            Assert.AreEqual(" [{datetime}]", cmd.Suffix);
            Assert.AreEqual("/var/log/rclone/livesync_shared_{datetime}.log", cmd.LogFile);
            Assert.AreEqual("INFO", cmd.LogLevel);
        }

        [TestMethod]
        public void Parse_RoutesUnknownFlagsToExtraArgs()
        {
            var cmd = RcloneCommandParser.Parse(
                "rclone sync /src remote:dst --use-mmap --exclude \"Publiczny/**\" --transfers=6");

            Assert.AreEqual("/src", cmd.Source);
            Assert.AreEqual("remote:dst", cmd.Destination);
            Assert.AreEqual(6, cmd.Transfers);
            StringAssert.Contains(cmd.ExtraArgs, "--use-mmap");
            StringAssert.Contains(cmd.ExtraArgs, "--exclude");
            StringAssert.Contains(cmd.ExtraArgs, "Publiczny/**");
        }

        [TestMethod]
        public void Parse_WindowsBatchVariables_BecomePlaceholders()
        {
            var cmd = RcloneCommandParser.Parse(
                "rclone.exe sync e:\\Shared remote:Shared --suffix \" [%datetime%]\" --backup-dir remote:Archive\\%year%");

            Assert.AreEqual(" [{datetime}]", cmd.Suffix);
            Assert.AreEqual("remote:Archive\\{year}", cmd.BackupDir);
        }
    }
}
