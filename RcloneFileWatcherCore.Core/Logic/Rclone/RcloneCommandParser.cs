using RcloneFileWatcherCore.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RcloneFileWatcherCore.Logic.Rclone
{
    /// <summary>
    /// Best-effort import of an existing rclone .bat/.sh script into a structured
    /// <see cref="RcloneCommandDTO"/> (managed mode). Finds the rclone invocation line, joins
    /// line continuations, tokenizes (quote-aware), maps known flags and routes the rest to
    /// ExtraArgs. Shell date variables (${datetime}/${year}/...) are converted to the app's
    /// {datetime}/{year} placeholders and escaped \$ is unescaped. The result should be
    /// reviewed by the user before saving.
    /// </summary>
    public static class RcloneCommandParser
    {
        private static readonly HashSet<string> ValueFlags = new(StringComparer.OrdinalIgnoreCase)
        {
            "--include-from", "--config", "--bwlimit", "--transfers", "--checkers",
            "--retries", "--retries-sleep", "--backup-dir", "--suffix", "--log-file", "--log-level"
        };

        private static readonly HashSet<string> BooleanFlags = new(StringComparer.OrdinalIgnoreCase)
        {
            "--create-empty-src-dirs", "--use-mmap", "--fast-list", "--dry-run",
            "--progress", "--checksum", "--update", "--verbose"
        };

        private static readonly Regex SubCommand =
            new(@"\b(sync|copy|move|copyto|moveto|bisync)\b", RegexOptions.IgnoreCase);

        public static RcloneCommandDTO Parse(string scriptText)
        {
            var line = FindRcloneLine(scriptText);
            if (line == null)
                return new RcloneCommandDTO { IncludeFrom = false, Command = string.Empty, RclonePath = string.Empty };

            return ParseLine(line);
        }

        /// <summary>
        /// Parse every rclone invocation found in the script into its own command. Useful for
        /// full-sync batch files that chain several rclone calls (one section per remote/path).
        /// Returns an empty list when no rclone line is found.
        /// </summary>
        public static List<RcloneCommandDTO> ParseMany(string scriptText)
            => FindRcloneLines(scriptText).Select(ParseLine).ToList();

        private static RcloneCommandDTO ParseLine(string line)
        {
            var cmd = new RcloneCommandDTO { IncludeFrom = false, Command = string.Empty, RclonePath = string.Empty };
            var tokens = Tokenize(line);
            if (tokens.Count == 0)
                return cmd;

            int i = 0;
            cmd.RclonePath = Subst(tokens[i++]);
            if (i < tokens.Count && IsCommandWord(tokens[i]))
                cmd.Command = tokens[i++].ToLowerInvariant();

            var positionals = new List<string>();
            var extra = new List<string>();

            for (; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (!token.StartsWith("-"))
                {
                    positionals.Add(Subst(token));
                    continue;
                }

                var flag = token;
                string value = null;
                var eq = token.IndexOf('=');
                if (eq > 0)
                {
                    flag = token.Substring(0, eq);
                    value = token.Substring(eq + 1);
                }
                flag = flag.ToLowerInvariant();

                var takesValue = ValueFlags.Contains(flag) || !BooleanFlags.Contains(flag);
                if (value == null && takesValue && i + 1 < tokens.Count && !tokens[i + 1].StartsWith("-"))
                    value = tokens[++i];
                value = value != null ? Subst(value) : null;

                switch (flag)
                {
                    case "--include-from": cmd.IncludeFrom = true; break; // re-injected by the builder
                    case "--config": cmd.ConfigFile = value; break;
                    case "--bwlimit": cmd.BwLimit = value; break;
                    case "--transfers": cmd.Transfers = ParseInt(value); break;
                    case "--checkers": cmd.Checkers = ParseInt(value); break;
                    case "--retries": cmd.Retries = ParseInt(value); break;
                    case "--retries-sleep": cmd.RetriesSleep = value; break;
                    case "--backup-dir": cmd.BackupDir = value; break;
                    case "--suffix": cmd.Suffix = value; break;
                    case "--log-file": cmd.LogFile = value; break;
                    case "--log-level": cmd.LogLevel = value; break;
                    case "--create-empty-src-dirs": cmd.CreateEmptySrcDirs = true; break;
                    default:
                        extra.Add(flag);
                        if (value != null)
                            extra.Add(value);
                        break;
                }
            }

            if (positionals.Count > 0) cmd.Source = positionals[0];
            if (positionals.Count > 1) cmd.Destination = positionals[1];
            if (positionals.Count > 2) extra.InsertRange(0, positionals.Skip(2));
            if (extra.Count > 0) cmd.ExtraArgs = string.Join("\n", extra);
            if (string.IsNullOrEmpty(cmd.Command)) cmd.Command = "sync";

            return cmd;
        }

        private static string FindRcloneLine(string scriptText)
            => FindRcloneLines(scriptText).FirstOrDefault();

        private static IEnumerable<string> FindRcloneLines(string scriptText)
        {
            if (string.IsNullOrWhiteSpace(scriptText))
                yield break;

            var joined = Regex.Replace(scriptText, @"\\\s*\r?\n", " ");
            foreach (var raw in joined.Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#") || line.StartsWith("@") || line.StartsWith("rem ", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (Regex.IsMatch(line, @"rclone(\.exe)?\b", RegexOptions.IgnoreCase) && SubCommand.IsMatch(line))
                    yield return line;
            }
        }

        private static bool IsCommandWord(string token) =>
            SubCommand.IsMatch(token) && !token.StartsWith("-");

        private static List<string> Tokenize(string line)
        {
            var tokens = new List<string>();
            var sb = new StringBuilder();
            char quote = '\0';
            bool quoted = false;

            foreach (var c in line)
            {
                if (quote != '\0')
                {
                    if (c == quote) quote = '\0';
                    else sb.Append(c);
                }
                else if (c == '"' || c == '\'')
                {
                    quote = c;
                    quoted = true;
                }
                else if (char.IsWhiteSpace(c))
                {
                    if (sb.Length > 0 || quoted) { tokens.Add(sb.ToString()); sb.Clear(); quoted = false; }
                }
                else
                {
                    sb.Append(c);
                }
            }
            if (sb.Length > 0 || quoted)
                tokens.Add(sb.ToString());

            return tokens;
        }

        private static string Subst(string value)
        {
            if (value == null)
                return null;
            return value
                // Linux shell variables
                .Replace("${datetime}", "{datetime}").Replace("$datetime", "{datetime}")
                .Replace("${date}", "{date}").Replace("$date", "{date}")
                .Replace("${time}", "{time}").Replace("$time", "{time}")
                .Replace("${year}", "{year}").Replace("$year", "{year}")
                // Windows batch variables
                .Replace("%datetime%", "{datetime}")
                .Replace("%date%", "{date}")
                .Replace("%time%", "{time}")
                .Replace("%year%", "{year}")
                .Replace("\\$", "$");
        }

        private static int ParseInt(string value) =>
            int.TryParse(value, out var n) ? n : 0;
    }
}
