namespace AStar.Dev.OneDrive.Client;

public static class DebugLogMetadata
{
    public static class UI
    {
        public static class MainWindowViewModel
        {
            public const string Constructor = "MainWindowViewModel.Constructor";  
        }

        public static class SyncTreeViewModel
        {
            public const string StartSync = "SyncTreeViewModel.StartSyncAsync";
        }   
    }

    public static class Services
    {
        public static class SyncEngine
        {
            public const string StartSync = "SyncEngine.StartSyncAsync";  
        }
    }
}
