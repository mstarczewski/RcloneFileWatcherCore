namespace RcloneFileWatcherCore.Logic.Interfaces
{
    interface ILogger
    {
        bool Enable { get; set; }
        void Write(string text);
        void WriteAlways(string text);
    }
}
