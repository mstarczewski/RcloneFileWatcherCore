using System;
using System.Collections.Generic;
using System.Linq;

namespace RcloneFileWatcherCore.Logic.Rclone
{
    /// <summary>
    /// Expands date/time placeholders in managed rclone arguments at execution time, mirroring
    /// the ${datetime}/${year} variables used in the example shell scripts (so --suffix,
    /// --backup-dir and --log-file get a per-run timestamp). The preview keeps the raw tokens.
    /// </summary>
    public static class RclonePlaceholders
    {
        public const string Hint = "{datetime}, {date}, {time}, {year}";

        public static string Expand(string value, DateTime now)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return value
                .Replace("{datetime}", now.ToString("yyyy-MM-dd-HH-mm-ss"))
                .Replace("{date}", now.ToString("yyyy-MM-dd"))
                .Replace("{time}", now.ToString("HH-mm-ss"))
                .Replace("{year}", now.ToString("yyyy"));
        }

        public static IReadOnlyList<string> Expand(IReadOnlyList<string> args, DateTime now)
        {
            return args.Select(a => Expand(a, now)).ToList();
        }
    }
}
