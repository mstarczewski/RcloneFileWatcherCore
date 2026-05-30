using RcloneFileWatcherCore.DTO;
using System.IO;
using System.Text.Json;

namespace RcloneFileWatcherCore.Config
{
    /// <summary>
    /// Serializes a <see cref="ConfigDTO"/> back to the JSON config file using the same
    /// indented format as the example generator. Writes to a temp file first and then
    /// replaces the target so a crash mid-write cannot leave a truncated config.
    /// </summary>
    public static class ConfigWriter
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public static void Save(string filePath, ConfigDTO config)
        {
            var json = JsonSerializer.Serialize(config, Options);
            var tempPath = filePath + ".tmp";
            File.WriteAllText(tempPath, json);

            if (File.Exists(filePath))
                File.Replace(tempPath, filePath, null);
            else
                File.Move(tempPath, filePath);
        }
    }
}
