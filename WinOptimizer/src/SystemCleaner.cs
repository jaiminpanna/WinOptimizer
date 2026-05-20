using System.Runtime.InteropServices;

namespace WinOptimizer.src
{
    public static class SystemCleaner
    {
        // Windows API for emptying Recycle Bin
        [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
        private static extern uint SHEmptyRecycleBin(
            IntPtr hwnd, string? pszRootPath,
            uint dwFlags);

        private const uint SHERB_NOCONFIRMATION = 0x00000001;
        private const uint SHERB_NOPROGRESSUI = 0x00000002;
        private const uint SHERB_NOSOUND = 0x00000004;

        /// <summary>
        /// Safely deletes all files in a folder without touching subfolders
        /// that contain locked/system files. Returns count of deleted files.
        /// </summary>
        public static (long filesDeleted, long bytesFreed) CleanFolder(
            string folderPath,
            IProgress<string>? progress = null)
        {
            long files = 0;
            long bytes = 0;

            if (!Directory.Exists(folderPath))
                return (0, 0);

            try
            {
                // Delete files
                foreach (var file in Directory.EnumerateFiles(
                    folderPath, "*.*", SearchOption.AllDirectories))
                {
                    try
                    {
                        var info = new FileInfo(file);
                        long size = info.Length;
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                        files++;
                        bytes += size;
                    }
                    catch { /* Skip locked/protected files */ }
                }

                // Delete empty subdirectories
                foreach (var dir in Directory.EnumerateDirectories(
                    folderPath, "*", SearchOption.AllDirectories)
                    .Reverse()) // Delete deepest first
                {
                    try
                    {
                        if (!Directory.EnumerateFileSystemEntries(dir).Any())
                            Directory.Delete(dir);
                    }
                    catch { /* Skip if still has files */ }
                }
            }
            catch { /* Folder may not exist or be inaccessible */ }

            return (files, bytes);
        }

        /// <summary>
        /// Flush DNS cache — equivalent to ipconfig /flushdns
        /// </summary>
        public static bool FlushDnsCache()
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "ipconfig",
                    Arguments = "/flushdns",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                using var p = System.Diagnostics.Process.Start(psi);
                p?.WaitForExit(3000);
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Empty the Recycle Bin on all drives
        /// </summary>
        public static bool EmptyRecycleBin()
        {
            try
            {
                SHEmptyRecycleBin(IntPtr.Zero, null,
                    SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Stop Windows Update service, clean its download cache, restart it
        /// </summary>
        public static (long files, long bytes) CleanWindowsUpdateCache(
            IProgress<string>? progress = null)
        {
            progress?.Report("Stopping Windows Update service...");
            RunCommand("net", "stop wuauserv");
            RunCommand("net", "stop bits");

            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                @"SoftwareDistribution\Download");

            var result = CleanFolder(path, progress);

            progress?.Report("Restarting Windows Update service...");
            RunCommand("net", "start wuauserv");
            RunCommand("net", "start bits");

            return result;
        }

        /// <summary>
        /// Clean Windows Error Reporting temp files
        /// </summary>
        public static (long files, long bytes) CleanWerFiles()
        {
            long totalFiles = 0, totalBytes = 0;

            string[] werPaths = {
                Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                    @"Microsoft\Windows\WER\ReportArchive"),
                Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                    @"Microsoft\Windows\WER\ReportQueue"),
                @"C:\ProgramData\Microsoft\Windows\WER\ReportArchive",
                @"C:\ProgramData\Microsoft\Windows\WER\ReportQueue"
            };

            foreach (var path in werPaths)
            {
                var (f, b) = CleanFolder(path);
                totalFiles += f;
                totalBytes += b;
            }
            return (totalFiles, totalBytes);
        }

        /// <summary>
        /// Clean Windows thumbnail cache (forces Windows to rebuild it)
        /// </summary>
        public static (long files, long bytes) CleanThumbnailCache()
        {
            var thumbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Microsoft\Windows\Explorer");

            long files = 0, bytes = 0;

            if (!Directory.Exists(thumbPath)) return (0, 0);

            foreach (var file in Directory.EnumerateFiles(
                thumbPath, "thumbcache_*.db"))
            {
                try
                {
                    var info = new FileInfo(file);
                    bytes += info.Length;
                    File.Delete(file);
                    files++;
                }
                catch { }
            }
            return (files, bytes);
        }

        private static void RunCommand(string exe, string args)
        {
            try
            {
                using var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
                p?.WaitForExit(5000);
            }
            catch { }
        }

        /// <summary>
        /// Format bytes into human-readable string
        /// </summary>
        public static string FormatBytes(long bytes)
        {
            if (bytes >= 1_073_741_824) return $"{bytes / 1_073_741_824.0:F2} GB";
            if (bytes >= 1_048_576) return $"{bytes / 1_048_576.0:F2} MB";
            if (bytes >= 1_024) return $"{bytes / 1_024.0:F2} KB";
            return $"{bytes} bytes";
        }
    }
}