namespace RcloneFileWatcherCore.Logic.Interfaces
{
    public interface IFileSystem
    {
        bool FileExists(string path);
        bool DirectoryExists(string path);
    }
}
