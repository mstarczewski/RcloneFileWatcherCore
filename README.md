# RcloneFileWatcherCore

[![Build Status](https://github.com/mstarczewski/RcloneFileWatcherCore/actions/workflows/release.yml/badge.svg)](https://github.com/mstarczewski/RcloneFileWatcherCore/actions)
[![Latest Release](https://img.shields.io/github/v/release/mstarczewski/RcloneFileWatcherCore)](https://github.com/mstarczewski/RcloneFileWatcherCore/releases)
[![Downloads](https://img.shields.io/github/downloads/mstarczewski/RcloneFileWatcherCore/latest/total)](https://github.com/mstarczewski/RcloneFileWatcherCore/releases)
[![Windows](https://img.shields.io/badge/Platform-Windows-blue?logo=windows)](https://github.com/mstarczewski/RcloneFileWatcherCore)
[![Linux](https://img.shields.io/badge/Platform-Linux-green?logo=linux)](https://github.com/mstarczewski/RcloneFileWatcherCore)
[![Mac](https://img.shields.io/badge/Platform-macOS-black?logo=apple)](https://github.com/mstarczewski/RcloneFileWatcherCore)
[![License](https://img.shields.io/github/license/mstarczewski/RcloneFileWatcherCore)](https://github.com/mstarczewski/RcloneFileWatcherCore/blob/master/LICENSE)
![.NET](https://shields.io/badge/.NET-8.0-blue)

---

## About

**RcloneFileWatcherCore** is a **multi-platform** .NET 8-based tool that enables real-time one-way file synchronization using filesystem change tracking. Instead of scanning entire folders, it watches for file and directory changes and launches `rclone` to sync only the affected files.

>### ℹ️ Secure backups
> **This makes it possible to perform secure, real-time, encrypted backups to cloud storage providers supported by rclone.**

The configuration is optimized for Windows and Linux. On Windows, it is recommended to compile the program yourself to avoid security warnings from system features like SmartScreen. On Linux, self-compilation is optional; there are no signed binaries restrictions.

---

## Key Features

- Real-time file and directory change monitoring
- Automatically generates `--include-from` file for rclone
- Executes an rclone batch/command script with proper file filtering
- Optional full-sync at startup
- Optional full-sync at specified time of day
- Optionally auto-updates rclone binary

---

## ⚙️ Requirements

- [.NET 8.0 Runtime](https://dotnet.microsoft.com/en-us/download)
- [rclone](https://rclone.org/downloads/) **version ≥ 1.56**
- Supported OS:
  - **Windows** (tested, recommended to compile yourself to avoid security warnings)
  - **Linux** (tested, no signed binaries restrictions)
  - **macOS** (requires self-compilation and possible adaptation; not tested)

---

## Installation

1. Install and configure [rclone](https://rclone.org/)
2. Download [source code or binaries](https://github.com/mstarczewski/RcloneFileWatcherCore/releases)
3. ⚠️ **Windows only:** For security, it's recommended to compile the program yourself. Unsigned binaries may be blocked by system security features.  
   **Linux:** Self-compilation is optional; there are no signed binaries restrictions.

---

## Configuration

Create a config file named `RcloneFileWatcherCoreConfig.cfg` in the executable folder:

```json
{
  "LogLevel": "Information|Error|Debug",
  "LogPath": "RcloneFileWatcherCore.log",
  "Path": [
    {
      "WatchingPath": "/home/user/Test/",
      "RcloneFilesFromPath": "/home/user/files-from-test.txt",
      "RcloneBatch": "/home/user/rclone_Test.sh",
      "ExcludeContains": [
        ".tmp",
        ".drivedownload1"
      ]
    },
    {
      "WatchingPath": "/home/user/Test1/",
      "RcloneFilesFromPath": "/home/user/files-from-test1.txt",
      "RcloneBatch": "/home/user/rclone_Test1.sh",
      "ExcludeContains": [
        ".tmp",
        ".drivedownload1"
      ]
    }
  ],
  "UpdateRclone": {
    "Update": true,
    "RclonePath": "./rclone",
    "CheckUpdateHours": 350
  },
  "SyncIntervalSeconds": 30,
  "RunOneTimeFullStartupSync": true,
  "RunOneTimeFullStartupSyncBatch": "rclone_fullsync.sh",
  "RunStartupScriptEveryDayAt": "05:30"
}

```

### Configuration Parameters

* `"LogLevel": Information|Error `" – Sets the logging level. Multiple levels can be combined using the pipe (|), e.g. Trace|Debug|Information|Warning|Error|Critical|Always.
* `"LogPath": "RcloneFileWatcherCore.log"` – Specifies the log file path. Leave empty (`""`) to output logs to the console instead.
* `"WatchingPath"` – Directory to monitor for changes
* `"RcloneFilesFromPath"` – Path to the output file used with `--include-from` in rclone
* `"RcloneBatch"` – Path to the batch script that runs rclone (executed every 30 seconds if changes are detected). This script **must** include the `--include-from` parameter
* `"ExcludeContains"` – List of substrings; any path containing these will be excluded
* `"UpdateRclone"` – Section responsible for auto-updating rclone
* `"Update"` – Enables automatic rclone updates
* `"RclonePath"` – Path to the local `rclone.exe`
* `"RcloneWebsiteCurrentVersionAddress"` – URL to the latest rclone binary
* `"CheckUpdateHours"` – How often (in hours) to check for updates
* `"SyncIntervalSeconds"` -	Interval between sync attempts (if changes detected)
* `"RunOneTimeFullStartupSync"` -	Runs a full sync batch at startup
* `"RunOneTimeFullStartupSyncBatch"` - Path to full sync batch script
* `"RunStartupScriptEveryDayAt"` - Runs the startup script (full sync) once per day at the specified time.

### Example rclone script (Linux) (`rclone_livesync_shared.sh`)

```bash
#!/bin/bash

datetime=$(date '+%Y-%m-%d-%H-%M-%S')
year=$(date '+%Y')

mkdir -p /var/log/rclone

/opt/rclone/rclone sync --include-from /var/log/rclone/files-from-shared.txt /mnt/samba/Shared pcloudcryptDaily:Shared \
  --retries-sleep 1m \
  --retries 30 \
  --bwlimit 15M:off \
  --create-empty-src-dirs \
  --backup-dir "pcloudcryptDaily:\$Archive/Shared/${year}" \
  --suffix " [${datetime}]" \
  --log-file=/var/log/rclone/livesync_shared_${datetime}.log \
  --log-level INFO
```
### Example rclone script (Linux) (`rclone_startupsync.sh`)

```bash
#!/bin/bash

datetime=$(date '+%Y-%m-%d-%H-%M-%S')
year=$(date '+%Y')

mkdir -p /var/log/rclone

/opt/rclone/rclone sync /mnt/samba/Shared/ pcloudcryptDaily:Shared \
  --bwlimit 15M \
  --transfers=6 \
  --checkers=12 \
  --use-mmap \
  --backup-dir "pcloudcryptDaily:\$Archive/Shared/${year}" \
  --suffix " [${datetime}]" \
  --create-empty-src-dirs \
  --log-file=/var/log/rclone/shared_${datetime}.log \
  --log-level INFO
```

### Example rclone script (Windows) (`rclone_livesync_shared.bat`)

```bash
@echo off
setlocal
for /f "delims=" %%a in (
    'powershell -Command "Get-Date -Format ''yyyy-MM-dd-HH-mm-ss''"'
) do set "datetime=%%a"
for /f "delims=" %%b in (
    'powershell -Command "Get-Date -Format ''yyyy''"'
) do set "year=%%b"
@echo on
rclone.exe sync --config="C:\rclone\rclone.conf" --include-from .\Logs\files-from-shared.txt e:\Shared pcloudcryptDaily:Shared --retries-sleep 1m --retries 30 --bwlimit 30M:off --create-empty-src-dirs --backup-dir pcloudcryptDaily:$Archive\Shared\%year% --suffix " [%datetime%]" --log-file=.\Logs\log_livesync_shared.txt --log-level INFO
@endlocal
```
### Example rclone script (Windows) (`rclone_startupsync.bat`)

```bash
@echo off
setlocal
for /f "delims=" %%a in (
    'powershell -Command "Get-Date -Format ''yyyy-MM-dd-HH-mm-ss''"'
) do set "datetime=%%a"
for /f "delims=" %%b in (
    'powershell -Command "Get-Date -Format ''yyyy''"'
) do set "year=%%b"
@echo on
"D:\Rclone\rclone.exe" sync e:\Shared pcloudcryptDaily:Shared --bwlimit 25M:off --transfers=32 --checkers=60 --backup-dir pcloudcryptDaily:$Archive\Shared\%year% --suffix " [%datetime%]" --create-empty-src-dirs --log-file=d:\log_shared.txt --log-level INFO
@endlocal
```

### Additional Notes

* On Linux, you can run it as a background service using systemd or nohup.
* On Windows, you can run this tool as a service using **[NSSM](https://nssm.cc/)** – the Non-Sucking Service Manager.
* It is recommended to run  `RcloneFileWatcherCore` in the background continuously, and setup `RunStartupScriptEveryDayAt` a full rclone sync once per day to ensure data consistency. Ideally, the full sync should result in no changes if the watcher worked correctly.
