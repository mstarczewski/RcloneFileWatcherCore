using RcloneFileWatcherCore.DTO;

namespace RcloneFileWatcherCore.Logic.Interfaces
{
    /// <summary>Probes an rclone executable path to report whether it exists and its version.</summary>
    public interface IRcloneVersionService
    {
        /// <summary>Runs "<paramref name="rclonePath"/> version" and returns availability + version.
        /// An empty/null path is treated as "rclone" resolved from PATH.</summary>
        RcloneVersionInfo Probe(string rclonePath);
    }
}
