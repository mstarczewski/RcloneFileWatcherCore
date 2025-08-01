namespace RcloneFileWatcherCore.Logic.Interfaces
{
    public interface ILogger
    {
        bool Enable { get; set; }
        void Write(string text);
        void WriteAlways(string text);
    }
}
