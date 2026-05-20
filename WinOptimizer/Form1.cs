using WinOptimizer.src;
using DriveInfo = WinOptimizer.src.DriveInfo;
using DriveType = WinOptimizer.src.DriveType;

namespace WinOptimizer
{
    public partial class Form1 : Form
    {
        private readonly CancellationTokenSource _cts = new();

        // --- Controls ---
        private TabControl tabControl = null!;
        private TabPage tabClean = null!, tabDrives = null!;

        // Clean tab
        private CheckedListBox chkCleanOptions = null!;
        private Button btnClean = null!, btnScan = null!;
        private ProgressBar progressBar = null!;
        private RichTextBox logBox = null!;
        private Label lblStatus = null!;

        // Drives tab
        private ListView lvDrives = null!;
        private Button btnOptimize = null!, btnAnalyze = null!;
        private RichTextBox driveLog = null!;

        public Form1()
        {
            InitializeComponent();
            BuildUI();
            PopulateDrives();
        }

        private void BuildUI()
        {
            // Form settings
            Text = "⚡ WinOptimizer — System Cleaner & Drive Optimizer";
            Size = new Size(800, 620);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(700, 550);
            BackColor = Color.FromArgb(240, 240, 240);
            Font = new Font("Segoe UI", 9.5f);

            // Tab control
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Padding = new Point(12, 6)
            };
            tabClean = new TabPage("🧹  Clean System");
            tabDrives = new TabPage("💾  Optimize Drives");

            tabControl.TabPages.Add(tabClean);
            tabControl.TabPages.Add(tabDrives);
            Controls.Add(tabControl);

            BuildCleanTab();
            BuildDrivesTab();
        }

        private void BuildCleanTab()
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
            tabClean.Controls.Add(panel);

            // Header label
            var header = new Label
            {
                Text = "Select items to clean. All operations are safe and reversible by Windows.",
                Dock = DockStyle.Top,
                Height = 32,
                Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                ForeColor = Color.DimGray,
                Padding = new Padding(0, 8, 0, 0)
            };
            panel.Controls.Add(header);

            // Checklist
            chkCleanOptions = new CheckedListBox
            {
                Dock = DockStyle.Top,
                Height = 210,
                CheckOnClick = true,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10f)
            };

            string[] items = {
                "User Temp Files  (%TEMP%)",
                "System Temp Files  (C:\\Windows\\Temp)",
                "Windows Prefetch Files  (speeds up next boot after clean)",
                "Windows Update Download Cache",
                "Windows Error Reports (crash dumps)",
                "Thumbnail Cache  (rebuilt automatically by Explorer)",
                "DNS Cache Flush  (fixes network glitches)",
                "Empty Recycle Bin"
            };

            foreach (var item in items)
                chkCleanOptions.Items.Add(item, true); // All checked by default

            panel.Controls.Add(chkCleanOptions);

            // Buttons row
            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 46,
                Padding = new Padding(0, 6, 0, 0),
                FlowDirection = FlowDirection.LeftToRight
            };

            btnScan = CreateButton("🔍  Scan First", Color.FromArgb(70, 130, 180));
            btnClean = CreateButton("🧹  Clean Now", Color.FromArgb(46, 139, 87));
            var btnSelectAll = CreateButton("✔ Select All", Color.FromArgb(105, 105, 105));
            var btnClearAll = CreateButton("✖ Clear All", Color.FromArgb(139, 90, 43));

            btnScan.Click += BtnScan_Click;
            btnClean.Click += BtnClean_Click;
            btnSelectAll.Click += (s, e) => {
                for (int i = 0; i < chkCleanOptions.Items.Count; i++)
                    chkCleanOptions.SetItemChecked(i, true);
            };
            btnClearAll.Click += (s, e) => {
                for (int i = 0; i < chkCleanOptions.Items.Count; i++)
                    chkCleanOptions.SetItemChecked(i, false);
            };

            btnPanel.Controls.AddRange(new Control[] {
                btnScan, btnClean, btnSelectAll, btnClearAll
            });
            panel.Controls.Add(btnPanel);

            // Progress bar
            progressBar = new ProgressBar
            {
                Dock = DockStyle.Top,
                Height = 20,
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 0, // Hidden until running
                Visible = false
            };
            panel.Controls.Add(progressBar);

            // Status label
            lblStatus = new Label
            {
                Dock = DockStyle.Top,
                Height = 24,
                Text = "Ready. Select items and click Clean Now.",
                ForeColor = Color.DimGray
            };
            panel.Controls.Add(lblStatus);

            // Log box
            logBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.LightGreen,
                Font = new Font("Consolas", 9f),
                BorderStyle = BorderStyle.FixedSingle
            };
            panel.Controls.Add(logBox);

            // Fix layout order (Fill must be added last for proper docking)
            panel.Controls.SetChildIndex(logBox, 0);
            panel.Controls.SetChildIndex(lblStatus, 1);
            panel.Controls.SetChildIndex(progressBar, 2);
            panel.Controls.SetChildIndex(btnPanel, 3);
            panel.Controls.SetChildIndex(chkCleanOptions, 4);
            panel.Controls.SetChildIndex(header, 5);
        }

        private void BuildDrivesTab()
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
            tabDrives.Controls.Add(panel);

            var header = new Label
            {
                Text = "Select a drive and click Optimize. SSD uses TRIM, HDD uses Defragmentation.",
                Dock = DockStyle.Top,
                Height = 32,
                Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                ForeColor = Color.DimGray,
                Padding = new Padding(0, 8, 0, 0)
            };
            panel.Controls.Add(header);

            // Drive list
            lvDrives = new ListView
            {
                Dock = DockStyle.Top,
                Height = 160,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                BorderStyle = BorderStyle.FixedSingle
            };
            lvDrives.Columns.Add("Drive", 70);
            lvDrives.Columns.Add("Label", 120);
            lvDrives.Columns.Add("Type", 60);
            lvDrives.Columns.Add("File System", 90);
            lvDrives.Columns.Add("Total Size", 100);
            lvDrives.Columns.Add("Free Space", 100);
            lvDrives.Columns.Add("% Free", 80);
            panel.Controls.Add(lvDrives);

            // Buttons
            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 46,
                Padding = new Padding(0, 6, 0, 0)
            };

            btnAnalyze = CreateButton("🔍  Analyze", Color.FromArgb(70, 130, 180));
            btnOptimize = CreateButton("⚡  Optimize Drive", Color.FromArgb(46, 139, 87));
            var btnRefresh = CreateButton("🔄  Refresh", Color.FromArgb(105, 105, 105));

            btnAnalyze.Click += BtnAnalyze_Click;
            btnOptimize.Click += BtnOptimize_Click;
            btnRefresh.Click += (s, e) => PopulateDrives();

            btnPanel.Controls.AddRange(new Control[] {
                btnAnalyze, btnOptimize, btnRefresh
            });
            panel.Controls.Add(btnPanel);

            driveLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.LightGreen,
                Font = new Font("Consolas", 9f),
                BorderStyle = BorderStyle.FixedSingle
            };
            panel.Controls.Add(driveLog);

            panel.Controls.SetChildIndex(driveLog, 0);
            panel.Controls.SetChildIndex(btnPanel, 1);
            panel.Controls.SetChildIndex(lvDrives, 2);
            panel.Controls.SetChildIndex(header, 3);
        }

        private Button CreateButton(string text, Color color)
        {
            return new Button
            {
                Text = text,
                Height = 34,
                Width = 150,
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0)
            };
        }

        // ─────────────────────────────────────────────────────────
        //  CLEAN TAB EVENTS
        // ─────────────────────────────────────────────────────────

        private async void BtnScan_Click(object? sender, EventArgs e)
        {
            SetBusy(true);
            AppendLog("🔍 Scanning for junk files...\n", Color.Cyan);

            await Task.Run(() =>
            {
                ScanAndReport();
            });

            SetBusy(false);
        }

        private void ScanAndReport()
        {
            // These are read-only scans — just measure sizes
            var paths = new (string Label, string Path)[]
            {
                ("User Temp", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                    .Replace("Local", "Local") + @"\..\Local\Temp"),
                ("System Temp", @"C:\Windows\Temp"),
                ("Prefetch", @"C:\Windows\Prefetch"),
                ("WU Cache", Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                    @"SoftwareDistribution\Download")),
            };

            long totalBytes = 0;
            foreach (var (label, path) in paths)
            {
                if (!Directory.Exists(path)) continue;
                try
                {
                    long size = Directory.EnumerateFiles(path, "*.*",
                        SearchOption.AllDirectories).Sum(f => {
                            try { return new FileInfo(f).Length; } catch { return 0L; }
                        });
                    totalBytes += size;
                    AppendLog($"  {label}: {SystemCleaner.FormatBytes(size)} found",
                        Color.Yellow);
                }
                catch { }
            }

            AppendLog($"\n💾 Total estimated space to free: {SystemCleaner.FormatBytes(totalBytes)}\n",
                Color.LightGreen);
        }

        private async void BtnClean_Click(object? sender, EventArgs e)
        {
            var checked_items = chkCleanOptions.CheckedIndices;
            if (checked_items.Count == 0)
            {
                MessageBox.Show("Please select at least one item to clean.",
                    "Nothing Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show(
                "Ready to clean selected items.\n\nThis is safe — Windows will recreate temp files as needed.\n\nContinue?",
                "Confirm Clean",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            SetBusy(true);
            logBox.Clear();
            AppendLog("🧹 Starting cleanup...\n", Color.Cyan);

            long totalFiles = 0, totalBytes = 0;

            var progress = new Progress<string>(msg => AppendLog("  " + msg, Color.White));

            await Task.Run(() =>
            {
                var selected = chkCleanOptions.CheckedIndices.Cast<int>().ToList();

                // 0 — User Temp
                if (selected.Contains(0))
                {
                    AppendLog("\n[1/8] User Temp Files...", Color.Cyan);
                    string tempPath = Path.GetTempPath();
                    var (f, b) = SystemCleaner.CleanFolder(tempPath, progress);
                    totalFiles += f; totalBytes += b;
                    AppendLog($"  → {f} files, {SystemCleaner.FormatBytes(b)} freed", Color.LightGreen);
                }

                // 1 — System Temp
                if (selected.Contains(1))
                {
                    AppendLog("\n[2/8] System Temp Files...", Color.Cyan);
                    var (f, b) = SystemCleaner.CleanFolder(@"C:\Windows\Temp", progress);
                    totalFiles += f; totalBytes += b;
                    AppendLog($"  → {f} files, {SystemCleaner.FormatBytes(b)} freed", Color.LightGreen);
                }

                // 2 — Prefetch
                if (selected.Contains(2))
                {
                    AppendLog("\n[3/8] Prefetch Files...", Color.Cyan);
                    var (f, b) = SystemCleaner.CleanFolder(@"C:\Windows\Prefetch", progress);
                    totalFiles += f; totalBytes += b;
                    AppendLog($"  → {f} files, {SystemCleaner.FormatBytes(b)} freed", Color.LightGreen);
                }

                // 3 — Windows Update Cache
                if (selected.Contains(3))
                {
                    AppendLog("\n[4/8] Windows Update Cache...", Color.Cyan);
                    var (f, b) = SystemCleaner.CleanWindowsUpdateCache(progress);
                    totalFiles += f; totalBytes += b;
                    AppendLog($"  → {f} files, {SystemCleaner.FormatBytes(b)} freed", Color.LightGreen);
                }

                // 4 — WER files
                if (selected.Contains(4))
                {
                    AppendLog("\n[5/8] Windows Error Reports...", Color.Cyan);
                    var (f, b) = SystemCleaner.CleanWerFiles();
                    totalFiles += f; totalBytes += b;
                    AppendLog($"  → {f} files, {SystemCleaner.FormatBytes(b)} freed", Color.LightGreen);
                }

                // 5 — Thumbnail Cache
                if (selected.Contains(5))
                {
                    AppendLog("\n[6/8] Thumbnail Cache...", Color.Cyan);
                    var (f, b) = SystemCleaner.CleanThumbnailCache();
                    totalFiles += f; totalBytes += b;
                    AppendLog($"  → {f} files, {SystemCleaner.FormatBytes(b)} freed", Color.LightGreen);
                }

                // 6 — DNS Cache
                if (selected.Contains(6))
                {
                    AppendLog("\n[7/8] Flushing DNS Cache...", Color.Cyan);
                    bool ok = SystemCleaner.FlushDnsCache();
                    AppendLog(ok ? "  → DNS cache flushed ✅" : "  → Failed ❌",
                        ok ? Color.LightGreen : Color.OrangeRed);
                }

                // 7 — Recycle Bin
                if (selected.Contains(7))
                {
                    AppendLog("\n[8/8] Emptying Recycle Bin...", Color.Cyan);
                    bool ok = SystemCleaner.EmptyRecycleBin();
                    AppendLog(ok ? "  → Recycle Bin emptied ✅" : "  → Already empty",
                        Color.LightGreen);
                }
            });

            AppendLog($"\n{'─',-40}", Color.DimGray);
            AppendLog($"✅ DONE! Deleted {totalFiles} files — freed {SystemCleaner.FormatBytes(totalBytes)}",
                Color.LightGreen);

            SetBusy(false);
            lblStatus.Text = $"Last clean: freed {SystemCleaner.FormatBytes(totalBytes)} from {totalFiles} files";
        }

        // ─────────────────────────────────────────────────────────
        //  DRIVES TAB EVENTS
        // ─────────────────────────────────────────────────────────

        private void PopulateDrives()
        {
            lvDrives.Items.Clear();
            var drives = DriveOptimizer.GetFixedDrives();

            foreach (var d in drives)
            {
                double freePct = d.TotalSize > 0
                    ? (double)d.FreeSpace / d.TotalSize * 100 : 0;

                var item = new ListViewItem(d.Letter);
                item.SubItems.Add(d.Label);
                item.SubItems.Add(d.DriveType);
                item.SubItems.Add(d.FileSystem);
                item.SubItems.Add(SystemCleaner.FormatBytes(d.TotalSize));
                item.SubItems.Add(SystemCleaner.FormatBytes(d.FreeSpace));
                item.SubItems.Add($"{freePct:F1}%");
                item.Tag = d;

                // Color-code by free space
                item.ForeColor = freePct < 10 ? Color.OrangeRed :
                                 freePct < 25 ? Color.DarkOrange : Color.Black;

                lvDrives.Items.Add(item);
            }

            if (lvDrives.Items.Count > 0)
                lvDrives.Items[0].Selected = true;
        }

        private async void BtnAnalyze_Click(object? sender, EventArgs e)
        {
            if (lvDrives.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a drive.", "No Drive Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var drive = (DriveInfo)lvDrives.SelectedItems[0].Tag!;
            driveLog.Clear();
            AppendDriveLog($"🔍 Analyzing drive {drive.Letter} ({drive.Label})...\n", Color.Cyan);

            btnAnalyze.Enabled = false;
            string result = await DriveOptimizer.AnalyzeDriveAsync(drive.Letter);
            AppendDriveLog(result, Color.White);
            AppendDriveLog("\n✅ Analysis complete.", Color.LightGreen);
            btnAnalyze.Enabled = true;
        }

        private async void BtnOptimize_Click(object? sender, EventArgs e)
        {
            if (lvDrives.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a drive.", "No Drive Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var drive = (DriveInfo)lvDrives.SelectedItems[0].Tag!;
            string action = drive.DriveType == DriveType.SSD.ToString() ? "TRIM" : "Defragmentation";

            if (MessageBox.Show(
                $"Optimize {drive.Letter} ({drive.Label})?\n\n" +
                $"Type: {drive.DriveType}\nAction: {action}\n\n" +
                $"This may take several minutes for large drives.",
                "Confirm Optimization",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            driveLog.Clear();
            btnOptimize.Enabled = false;
            btnAnalyze.Enabled = false;

            var progress = new Progress<string>(msg => AppendDriveLog("  " + msg, Color.White));

            await DriveOptimizer.OptimizeDriveAsync(
                drive.Letter, drive.DriveType, progress, _cts.Token);

            AppendDriveLog("\n✅ Drive optimization complete!", Color.LightGreen);
            btnOptimize.Enabled = true;
            btnAnalyze.Enabled = true;
            PopulateDrives();
        }

        // ─────────────────────────────────────────────────────────
        //  HELPERS
        // ─────────────────────────────────────────────────────────

        private void SetBusy(bool busy)
        {
            if (InvokeRequired) { Invoke(() => SetBusy(busy)); return; }
            btnClean.Enabled = !busy;
            btnScan.Enabled = !busy;
            progressBar.Visible = busy;
            progressBar.MarqueeAnimationSpeed = busy ? 30 : 0;
            lblStatus.Text = busy ? "Working... please wait." : lblStatus.Text;
        }

        private void AppendLog(string text, Color color)
        {
            if (InvokeRequired) { Invoke(() => AppendLog(text, color)); return; }
            logBox.SelectionStart = logBox.TextLength;
            logBox.SelectionLength = 0;
            logBox.SelectionColor = color;
            logBox.AppendText(text + "\n");
            logBox.ScrollToCaret();
        }

        private void AppendDriveLog(string text, Color color)
        {
            if (InvokeRequired) { Invoke(() => AppendDriveLog(text, color)); return; }
            driveLog.SelectionStart = driveLog.TextLength;
            driveLog.SelectionLength = 0;
            driveLog.SelectionColor = color;
            driveLog.AppendText(text + "\n");
            driveLog.ScrollToCaret();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _cts.Cancel();
            base.OnFormClosing(e);
        }
    }
}