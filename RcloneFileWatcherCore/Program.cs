using RcloneFileWatcherCore.Logic;


namespace RcloneFileWatcherCore
{
    class Program
    {
        static void Main(string[] args)
        {
            Controller controller = new Controller();
            new System.Threading.AutoResetEvent(false).WaitOne();
        }
    }
}

