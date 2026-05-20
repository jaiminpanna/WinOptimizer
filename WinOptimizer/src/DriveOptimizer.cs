namespace WinOptimizer.src
{

    public static class DriveOptimizer
    {
        /// <summary>
        /// Get all fixed (non-removable) drives
        /// </summary>
        public static List<DriveInfo> GetFixedDrives()
        {
            var result = new List<DriveInfo>();
            foreach (var drive in System.IO.DriveInfo.GetDrives())
            {
                if (drive.DriveType != System.IO.DriveType.Fixed) continue;
                if (!drive.IsReady) continue;

                result.Add(new DriveInfo
                {
                    Letter = drive.Name.TrimEnd('\\'),
                    Label = string.IsNullOrWhiteSpace(drive.VolumeLabel)
                            ? "Local Disk" : drive.VolumeLabel,
                    DriveType = GetDriveType(drive.Name),
                    FileSystem = drive.DriveFormat,
                    TotalSize = drive.TotalSize,
                    FreeSpace = drive.AvailableFreeSpace,
                    IsHealthy = true
                });
            }
            return result;
        }

        /// <summary>
        /// Detect SSD vs HDD using PowerShell
        /// </summary>
        private static string GetDriveType(string driveName)
        {
            try
            {
                string driveLetter = driveName.TrimEnd('\\', ':');
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-Command \"Get-PhysicalDisk | Where-Object {{ $_.DeviceId -eq " +
                                $"(Get-Partition -DriveLetter '{driveLetter}' | " +
                                $"Get-Disk).Number }} | Select-Object -ExpandProperty MediaType\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = System.Diagnostics.Process.Start(psi);
                string output = p?.StandardOutput.ReadToEnd().Trim() ?? "";
                p?.WaitForExit(5000);

                return output.Contains(DriveType.SSD.ToString()) ? DriveType.SSD.ToString() : DriveType.HDD.ToString();
            }
            catch
            {
                return DriveType.HDD.ToString(); // Default to HDD if detection fails
            }
        }

        /// <summary>
        /// Optimize a drive — Defrag for HDD, TRIM for SSD
        /// Uses Windows built-in Optimize-Volume (safe, no 3rd party)
        /// </summary>
        public static async Task<string> OptimizeDriveAsync(
            string driveLetter,
            string driveType,
            IProgress<string>? progress = null,
            CancellationToken ct = default)
        {
            string letter = driveLetter.TrimEnd('\\').TrimEnd(':');

            string action = driveType == DriveType.SSD.ToString() ? "retrim" : "defrag";
            string actionName = driveType == DriveType.SSD.ToString() ? "TRIM (SSD Optimization)" : "Defragmentation (HDD)";

            progress?.Report($"Starting {actionName} on drive {letter}:...");

            string command = driveType == DriveType.SSD.ToString()
                ? $"Optimize-Volume -DriveLetter {letter} -ReTrim -Verbose"
                : $"Optimize-Volume -DriveLetter {letter} -Defrag -Verbose";

            string output = await RunPowerShellAsync(command, progress, ct);

            progress?.Report($"✅ Drive {letter}: optimization complete.");
            return output;
        }

        /// <summary>
        /// Analyze fragmentation level of a drive (HDD only)
        /// </summary>
        public static async Task<string> AnalyzeDriveAsync(string driveLetter)
        {
            string letter = driveLetter.TrimEnd('\\').TrimEnd(':');
            string cmd = $"Optimize-Volume -DriveLetter {letter} -Analyze -Verbose";
            return await RunPowerShellAsync(cmd, null, default);
        }

        private static Task<string> RunPowerShellAsync(
            string command,
            IProgress<string>? progress,
            CancellationToken ct)
        {
            return Task.Run(() =>
            {
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "powershell",
                        Arguments = $"-NoProfile -Command \"{command}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    var sb = new System.Text.StringBuilder();
                    using var p = new System.Diagnostics.Process { StartInfo = psi };
                    p.OutputDataReceived += (s, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            sb.AppendLine(e.Data);
                            progress?.Report(e.Data);
                        }
                    };
                    p.Start();
                    p.BeginOutputReadLine();
                    p.WaitForExit();
                    return sb.ToString();
                }
                catch (Exception ex)
                {
                    return $"Error: {ex.Message}";
                }
            }, ct);
        }
    }
}