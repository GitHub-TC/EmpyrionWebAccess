namespace EmpyrionModWebHost.Configuration
{
    public class AppSettings
    {
        public string Secret { get; set; }
        public string BackupDirectory { get; set; }
        public bool UseHttpsRedirection { get; set; } = true;
    }
}
