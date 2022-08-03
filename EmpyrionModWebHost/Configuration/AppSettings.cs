namespace EmpyrionModWebHost.Configuration
{
    public class AppSettings
    {
        public string Secret { get; set; }
        public string BackupDirectory { get; set; }
        public bool UseHttpsRedirection { get; set; } = true;
        public int GlobalStructureUpdateInSeconds { get; set; } = 5 * 60;
        public int HistoryLogUpdateInSeconds { get; set; } = 30;
        public int BackupStructureDataUpdateCheckInSeconds { get; set; } = 1;
        public int ExportDatOutdatedInMinutes { get; set; } = 60;
        public int SleepBetweenEntityExportInSeconds { get; set; } = 10;
        public int PlayerOfflineWarpDelay { get; set; } = 60;
    }
}
