using RcloneFileWatcherCore.Enums;
using System;

namespace RcloneFileWatcherCore.Config
{
    /// <summary>
    /// Parses the pipe/comma/semicolon/space separated LogLevel string from the config
    /// (e.g. "Information|Error") into the <see cref="LogLevel"/> flags enum. Shared by the
    /// config loader and the runtime reload path so both interpret the value identically.
    /// </summary>
    public static class LogLevelParser
    {
        public static LogLevel Parse(string configLogLevel)
        {
            if (string.IsNullOrWhiteSpace(configLogLevel))
            {
                return LogLevel.All;
            }

            LogLevel result = LogLevel.None;
            var parts = configLogLevel.Split(new[] { ',', ';', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                if (Enum.TryParse(part.Trim(), ignoreCase: true, out LogLevel level))
                {
                    result |= level;
                }
            }

            return result == LogLevel.None ? LogLevel.All : result;
        }
    }
}
