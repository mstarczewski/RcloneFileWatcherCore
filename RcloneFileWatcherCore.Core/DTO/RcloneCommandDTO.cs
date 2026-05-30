namespace RcloneFileWatcherCore.DTO
{
    /// <summary>
    /// Structured description of an rclone invocation, used in <see cref="Enums.SyncMode.Managed"/>
    /// mode instead of an external .bat/.sh script. The builder turns this into command-line
    /// arguments and injects --include-from from the path's files-from file.
    /// Empty/zero fields are omitted from the command line.
    /// </summary>
    public class RcloneCommandDTO
    {
        public string RclonePath { get; set; } = "rclone";
        public string Command { get; set; } = "sync";
        public string Source { get; set; }
        public string Destination { get; set; }

        /// <summary>Inject --include-from with the live-sync filter (the list of changed paths).</summary>
        public bool IncludeFrom { get; set; } = true;

        /// <summary>
        /// Feed the changed-files list to rclone via stdin (<c>--include-from -</c>) instead of
        /// writing the RcloneFilesFromPath file. Avoids the on-disk "exchange" file.
        /// </summary>
        public bool IncludeFromStdin { get; set; } = true;

        public string ConfigFile { get; set; }
        public string BwLimit { get; set; }
        public int Transfers { get; set; }
        public int Checkers { get; set; }
        public int Retries { get; set; }
        public string RetriesSleep { get; set; }
        public string BackupDir { get; set; }
        public string Suffix { get; set; }
        public bool CreateEmptySrcDirs { get; set; }
        public string LogFile { get; set; }
        public string LogLevel { get; set; }

        /// <summary>Free-form extra arguments, one token per line.</summary>
        public string ExtraArgs { get; set; }
    }
}
