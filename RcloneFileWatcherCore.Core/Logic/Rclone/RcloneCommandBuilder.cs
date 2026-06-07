using RcloneFileWatcherCore.DTO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RcloneFileWatcherCore.Logic.Rclone
{
    /// <summary>
    /// Turns a <see cref="RcloneCommandDTO"/> into rclone command-line arguments, injecting
    /// --include-from from the path's files-from file. Empty/zero fields are omitted. Also
    /// renders a human-readable preview of the resulting command line for the GUI.
    /// </summary>
    public static class RcloneCommandBuilder
    {
        public static IReadOnlyList<string> BuildArguments(RcloneCommandDTO command, string includeFromPath)
        {
            var args = new List<string>();
            if (command == null)
                return args;

            args.Add(string.IsNullOrWhiteSpace(command.Command) ? "sync" : command.Command.Trim());

            AddValue(args, command.Source);
            AddValue(args, command.Destination);

            if (command.IncludeFrom && !string.IsNullOrWhiteSpace(includeFromPath))
                AddFlag(args, "--include-from", includeFromPath);

            AddFlag(args, "--config", command.ConfigFile);
            AddFlag(args, "--bwlimit", command.BwLimit);
            AddFlag(args, "--transfers", command.Transfers);
            AddFlag(args, "--checkers", command.Checkers);
            AddFlag(args, "--retries", command.Retries);
            AddFlag(args, "--retries-sleep", command.RetriesSleep);
            AddFlag(args, "--backup-dir", command.BackupDir);
            AddFlag(args, "--suffix", command.Suffix);
            if (command.CreateEmptySrcDirs)
                args.Add("--create-empty-src-dirs");
            if (command.UseMmap)
                args.Add("--use-mmap");
            if (command.FastList)
                args.Add("--fast-list");
            if (command.Update)
                args.Add("--update");
            if (command.DryRun)
                args.Add("--dry-run");
            AddFlag(args, "--log-file", command.LogFile);
            AddFlag(args, "--log-level", command.LogLevel);

            if (!string.IsNullOrWhiteSpace(command.ExtraArgs))
            {
                var tokens = command.ExtraArgs
                    .Split(new[] { '\r', '\n', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                args.AddRange(tokens);
            }

            return args;
        }

        public static string BuildPreview(RcloneCommandDTO command, string includeFromPath)
        {
            if (command == null)
                return string.Empty;

            var exe = string.IsNullOrWhiteSpace(command.RclonePath) ? "rclone" : command.RclonePath.Trim();
            var parts = new List<string> { Quote(exe) };
            parts.AddRange(BuildArguments(command, includeFromPath).Select(Quote));
            return string.Join(" ", parts);
        }

        private static void AddValue(List<string> args, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                args.Add(value.Trim());
        }

        private static void AddFlag(List<string> args, string flag, string value)
        {
            // Note: the value is intentionally NOT trimmed - flags like --suffix rely on a
            // leading space (e.g. " [2024-01-01]"), as in the README examples.
            if (!string.IsNullOrWhiteSpace(value))
            {
                args.Add(flag);
                args.Add(value);
            }
        }

        private static void AddFlag(List<string> args, string flag, int value)
        {
            if (value > 0)
            {
                args.Add(flag);
                args.Add(value.ToString());
            }
        }

        private static string Quote(string token)
        {
            return token.Contains(' ') ? $"\"{token}\"" : token;
        }
    }
}
