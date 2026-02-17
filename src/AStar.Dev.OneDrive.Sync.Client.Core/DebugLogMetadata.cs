namespace AStar.Dev.OneDrive.Sync.Client.Core;

public static partial class DebugLogMetadata
{
    public static partial class Services
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

        public static class DeltaPageProcessor
        {
            public const string ProcessAllDeltaPagesAsync = "DeltaPageProcessor.ProcessAllDeltaPagesAsync";
        }
    }
}
