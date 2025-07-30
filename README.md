# RcloneFileWatcherCore 0.6

**.NET Core 3.1**

## Main Features

1. Monitor filesystem changes (file and directory level)
2. Generate a `--include-from` file for rclone
3. Execute an rclone batch file. The rclone command must include `--include-from`
4. Synchronize changes in real-time

## Installation

1. Install and configure [rclone](https://rclone.org/)
2. Download the [source code or binaries](https://github.com/mstarczewski/RcloneFileWatcherCore/releases) for RcloneFileWatcherCore

## Setup and Usage

**`RcloneFileWatcherCoreConfig.cfg`** – JSON configuration file (example below):

```json
{
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
  ],
  "UpdateRclone": {
    "Update": true,
    "RclonePath": ".\\rclone.exe",
    "RcloneWebsiteCurrentVersionAddress": "https://downloads.rclone.org/rclone-current-windows-amd64.zip",
    "ChceckUpdateHours": 1
  }
}
```

### Configuration Parameters

* `"ConsoleWriter": true` – enable or disable debug output in the console
* `"WatchingPath"` – directory to monitor for changes
* `"RcloneFilesFromPath"` – path to the output file used with `--include-from` in rclone
* `"RcloneBatch"` – path to the batch script that runs rclone (executed every 30 seconds if changes are detected). This script **must** include the `--include-from` parameter
* `"ExcludeContains"` – list of substrings; any path containing these will be excluded
* `"UpdateRclone"` – section responsible for auto-updating rclone
* `"Update"` – enables automatic rclone updates
* `"RclonePath"` – path to the local `rclone.exe`
* `"RcloneWebsiteCurrentVersionAddress"` – URL to the latest rclone binary
* `"ChceckUpdateHours"` – how often (in hours) to check for updates *(note: typo in key name – should be `CheckUpdateHours`)*

### Example rclone script (`rclone_livesync_shared.bat`)

```bash
rclone.exe sync --include-from d:\files-from-shared.txt e:\Shared pcloudcrypt:Shared --create-empty-src-dirs --backup-dir pcloudcrypt:$Archive\Shared\2021 --suffix " [backup]" --log-file=d:\log_livesync_shared.txt --log-level INFO
```

### Additional Notes

* On Windows, you can run this tool as a service using **[NSSM](https://nssm.cc/)** – the Non-Sucking Service Manager
* It is recommended to run `RcloneFileWatcherCore` in the background continuously, and schedule a full rclone sync once per day (e.g., via Task Scheduler or cron) to ensure data consistency. Ideally, the full sync should result in no changes if the watcher worked correctly.
