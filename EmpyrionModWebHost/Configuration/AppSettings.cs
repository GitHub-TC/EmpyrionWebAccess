namespace EmpyrionModWebHost.Configuration
{
    public class AppSettings
    {
        public string Secret { get; set; }
        public string BackupDirectory { get; set; }
        public bool UseHttpsRedirection { get; set; } = true;
        public int GlobalStructureUpdateInSeconds { get; set; } = 60;
        public int StructureDataUpdateCheckInSeconds { get; set; } = 1;
        public int StructureDataUpdateInMinutes { get; set; } = 60;
        public int StructureDataUpdateDelayInSeconds { get; set; } = 10;
    }
}
