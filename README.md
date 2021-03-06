# RcloneFileWatcherCore 0.2
.NET Core 3.1

## Main Features
1. Monitor filesystem changes (file/directory level)
2. Generate ```--include-from``` file for rclone
3. Execute rclone batch. Rclone command must contain ```--include-from```
4. Synchronize changes in real time.

## Installation
1. Install and configure [rclone](https://rclone.org/)
2. Download [source or binaries](https://github.com/mstarczewski/RcloneFileWatcherCore/releases) RcloneFileWatcherCore.

## Setup and Usage
**RcloneFileWatcherCoreConfig.cfg** - JSON config file (example below).
```{
  "ConsoleWriter": true,
  "Path": [
    {
      "WatchingPath": "e:\\Shared\\",
      "RcloneFilesFromPath": "d:\\files-from-shared.txt",
      "RcloneBatch": "d:\\rclone_livesync_shared.bat",
      "ExcludeContains": [
        ".tmp",
        ".drivedownload1"
      ]
    },
    {
      "WatchingPath": "d:\\test1\\",
      "RcloneFilesFromPath": "d:\\files-from-test1.txt",
      "RcloneBatch": "d:\\rclone_Test1.bat",
      "ExcludeContains": [
        ".tmp",
        ".drivedownload"
      ]
    }
  ]
}
```
- ```"ConsoleWriter": true``` - on or off display some debug information to console.

- ```"WatchingPath": "e:\\Shared\\"```  - Watching folder

- ```"RcloneFilesFromPath": "d:\\files-from-shared.txt"``` - output path to write --files-from (for rclone)

- ```"RcloneBatch": "d:\\files-from-shared.txt"``` - run rclone script (batch) every 30 seconds only when appears any changes. Rclone script must contain ```--include-from```

-```"ExcludeContains": [".tmp"]``` - exclude every path which contains ".tmp"

An example of a simple script - rclone_livesync_shared.bat:

```rclone.exe sync --include-from d:\files-from-shared.txt e:\Shared pcloudcrypt:Shared --create-empty-src-dirs --backup-dir pcloudcrypt:$Archive\Shared\2021 --suffix " [backup]" --log-file=d:\log_livesync_shared.txt --log-level INFO```

Windows users can run it as a service with NSSM - the Non-Sucking Service Manager.

### A case of use
Run RcloneFileWatcherCore and leave it in background. Once per day (to be sure) run full rclone sync via scheduler/cron - it should pass without any changes.
