namespace RcloneFileWatcherCore.DTO
{
    /// <summary>Result of probing an rclone executable path for availability and version.</summary>
    public sealed class RcloneVersionInfo
    {
        /// <summary>The executable path probed ("rclone" when none was configured = from PATH).</summary>
        public string Path { get; set; }

        /// <summary>True when the executable ran and reported a version.</summary>
        public bool Available { get; set; }

        /// <summary>Version line reported by rclone (e.g. "rclone v1.65.0"), when available.</summary>
        public string Version { get; set; }

        /// <summary>Short reason when not available (error message or non-zero exit).</summary>
        public string Error { get; set; }
    }
}
