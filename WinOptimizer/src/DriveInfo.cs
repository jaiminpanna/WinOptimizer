namespace WinOptimizer.src
{
    public class DriveInfo
    {
        public string Letter { get; set; } = "";
        public string Label { get; set; } = "";
        public string DriveType { get; set; } = "";
        public string FileSystem { get; set; } = "";
        public long TotalSize { get; set; }
        public long FreeSpace { get; set; }
        public bool IsHealthy { get; set; }
    }
}
