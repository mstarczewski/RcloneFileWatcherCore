# RcloneFileWatcherCore
.NET CORE 3.1

**Initial version** 

## Main Features:
1. Monitor filesystem changes (file/directory level)
2. Generate ```--include-from``` file for rclone
3. Execute rclone batch. Rclone command must contain ```--include-from```
4. Synchronize changes in real time.

## Usage:
**RcloneFileWatcherCoreConfig.txt** - config file.

- ```ConsoleWriter.ON/OFF``` - on or off display some debug information to console.

```e:\Shared\,d:\files-from-shared.txt,rclone_livesync_shared.bat```

```e:\UsersData\,d:\files-from-UsersData.txt,rclone_livesync_UsersData.bat```

- ```e:\Shared\``` - Monitored folder

- ```d:\files-from-shared.txt``` - output path to write --files-from

- ```rclone_livesync_shared.bat``` - run rclone batch every 30 seconds only when appears any changes. Rclone batch must contain ```--include-from```


An example of a simple script - rclone_livesync_shared.bat:

```rclone.exe sync --include-from d:\files-from-shared.txt e:\Shared pcloudcrypt:Shared --create-empty-src-dirs --backup-dir pcloudcrypt:$Archive\Shared\2021 --suffix " [backup]" --log-file=d:\log_livesync_shared.txt --log-level INFO```

Windows users can run it as a service with NSSM - the Non-Sucking Service Manager.

### A case of use.
Run RcloneFileWatcherCore and leave it in background. Once per day (to be sure) run full rclone sync via scheduler/cron - it should pass without any changes.
