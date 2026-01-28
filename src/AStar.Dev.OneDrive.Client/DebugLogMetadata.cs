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
        public static class AutoSyncSchedulerService
        {
            public const string StartAsync = "AutoSyncSchedulerService.StartAsync";
            public const string StopAsync = "AutoSyncSchedulerService.StopAsync";
            public const string UpdateSchedule = "AutoSyncSchedulerService.UpdateSchedule";
            public const string RemoveSchedule = "AutoSyncSchedulerService.RemoveSchedule";
            public const string AutoSyncTriggered = "AutoSyncSchedulerService.AutoSyncTriggered";
        }
    }
}
