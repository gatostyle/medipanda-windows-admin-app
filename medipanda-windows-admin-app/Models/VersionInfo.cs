namespace medipanda_windows_admin.Models
{
    public class VersionInfo
    {
        public string Version { get; set; }
        public string DownloadUrl { get; set; }
        public bool IsForceUpdate { get; set; }
        public string ReleaseNotes { get; set; }
        public string ReleaseDate { get; set; }
    }
}