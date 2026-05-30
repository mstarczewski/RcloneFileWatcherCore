namespace RcloneFileWatcherCore.Enums
{
    /// <summary>
    /// How a watched path runs rclone when changes are detected.
    /// <see cref="Script"/> keeps the original behavior (run a .bat/.sh that contains the
    /// full rclone command). <see cref="Managed"/> builds and runs the rclone command from
    /// structured fields, injecting --include-from automatically.
    /// </summary>
    public enum SyncMode
    {
        Script = 0,
        Managed = 1
    }
}
