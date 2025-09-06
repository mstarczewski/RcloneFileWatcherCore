using RcloneFileWatcherCore.Logic.Interfaces;
using System.IO;

namespace RcloneFileWatcherCore.Logic.Services
{
    public class FileSystemService : IFileSystem
    {
        public bool FileExists(string path) => File.Exists(path);
        public bool DirectoryExists(string path) => Directory.Exists(path);
    }
}
