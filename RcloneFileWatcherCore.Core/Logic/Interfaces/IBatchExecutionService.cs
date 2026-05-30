using RcloneFileWatcherCore.DTO;

public interface IBatchExecutionService
{
    /// <summary>Run an external rclone script (.bat/.sh) — Script sync mode.</summary>
    bool ExecuteBatch(string batchPath);

    /// <summary>
    /// Run rclone directly from a structured command — Managed sync mode. The builder injects
    /// --include-from from <paramref name="includeFromPath"/>; rclone's output is streamed to the log.
    /// </summary>
    bool ExecuteCommand(RcloneCommandDTO command, string includeFromPath);
}
