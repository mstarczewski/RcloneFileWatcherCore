using RcloneFileWatcherCore.DTO;
using System;

namespace RcloneFileWatcherCore.Config
{
    public interface IConfigService
    {
        /// <summary>The configuration currently in effect.</summary>
        ConfigDTO Current { get; }

        /// <summary>Path of the JSON config file that <see cref="Save"/> writes to.</summary>
        string FilePath { get; }

        /// <summary>Raised after a successful <see cref="Save"/> so the runtime can reload.</summary>
        event Action<ConfigDTO> Changed;

        /// <summary>
        /// Persists the new configuration to disk, makes it the current one, applies the log
        /// level to the logger, and raises <see cref="Changed"/>.
        /// </summary>
        void Save(ConfigDTO config);
    }
}
