# RcloneFileWatcherCore
.NET CORE 3.1

Initial version. 

Main features:
1. Monitoring  filesystem changes (file/directory level)
2. Generating --include-from file for rclone
3. Run rclone and synchronize changes

Using:

RcloneFileWatcherCoreConfig.txt - this is a config file

ConsoleWriter.ON/OFF - on or off display some debug information to console.

e:\Shared\,d:\files-from-shared.txt,rclone_livesync_shared.bat - monitoring e:\Shared, filename --files-from, rclone batch run every 30 seconds only when appears any changes.

...

...

...

e:\UsersData\,d:\files-from-UsersData.txt,rclone_livesync_UsersData.bat - possibility of monitoring and syncing more directories

An example of a simple script - rclone_livesync_shared.bat:
rclone.exe sync --include-from d:\files-from-shared.txt e:\Shared pcloudcrypt:Shared --create-empty-src-dirs --backup-dir pcloudcrypt:$Archive\Shared\2021 --suffix " [backup]" --log-file=d:\log_livesync_shared.txt --log-level INFO

Windows users can run it as a service with NSSM - the Non-Sucking Service Manager.

A case of use. Run it and once per a day (to be sure) run full rclone sync via Windows Scheduler/cron.
