using RcloneFileWatcherCore.DTO;
using System.Collections.Generic;

public interface IBatchExecutionService
{
    /// <summary>Run an external rclone script (.bat/.sh) — Script sync mode.</summary>
    bool ExecuteBatch(string batchPath);

    /// <summary>
    /// Run rclone directly from a structured command — Managed sync mode. The builder injects
    /// --include-from from <paramref name="includeFromPath"/> (use "-" for stdin); when
    /// <paramref name="includeFromStdin"/> is provided those lines are piped to rclone's stdin
    /// instead of a file. rclone's output is streamed to the log.
    /// </summary>
    bool ExecuteCommand(RcloneCommandDTO command, string includeFromPath, IReadOnlyList<string> includeFromStdin = null);

    /// <summary>True while at least one rclone/script process is running.</summary>
    bool AnyRunning { get; }

    /// <summary>Hard-stops every running rclone/script process.</summary>
    void CancelRunning();
}
