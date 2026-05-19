# ⚡ WinOptimizer

> A lightweight, safe, one-click Windows system cleaner and drive optimizer built with **.NET 10 WinForms** — no bloatware, no registry hacks, no third-party dependencies.

![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)
![Platform](https://img.shields.io/badge/Platform-Windows%2010%2F11-0078D6?style=flat-square&logo=windows)
![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)
![Admin Required](https://img.shields.io/badge/Requires-Administrator-red?style=flat-square)

---

## 📸 Screenshots

> *Clean System tab and Drive Optimizer tab*

| 🧹 Clean System | 💾 Drive Optimizer |
|----------------|-------------------|
| Select junk categories, scan first or clean instantly | Auto-detects SSD/HDD, runs TRIM or Defrag accordingly |

---

## ✨ Features

### 🧹 System Cleaner
- **User Temp Files** — Cleans `%TEMP%` (your personal temp folder)
- **System Temp Files** — Cleans `C:\Windows\Temp`
- **Prefetch Files** — Removes `C:\Windows\Prefetch` (Windows rebuilds automatically)
- **Windows Update Cache** — Deletes already-installed update downloads from `SoftwareDistribution\Download`
- **Windows Error Reports** — Removes crash dump files from WER folders
- **Thumbnail Cache** — Clears Explorer thumbnail database (rebuilt on demand)
- **DNS Cache Flush** — Equivalent to `ipconfig /flushdns`, fixes network glitches
- **Empty Recycle Bin** — Empties all drives' recycle bins via Windows Shell API

### 💾 Drive Optimizer
- **Auto-detects SSD vs HDD** using PowerShell and Windows disk APIs
- **SSD** → Runs **TRIM** (`Optimize-Volume -ReTrim`) — correct optimization for flash storage
- **HDD** → Runs **Defragmentation** (`Optimize-Volume -Defrag`)
- **Analyze** drive fragmentation before optimizing
- Shows drive size, free space, and health at a glance

### 🛡️ Safety First
- Uses **only Windows built-in APIs** — no third-party tools
- Automatically **skips locked or in-use files**
- **Never touches** personal folders (`Documents`, `Pictures`, `Music`, `Videos`)
- **Never touches** `Program Files`, `AppData\Roaming`, or `System32`
- Real-time colored log shows exactly what was cleaned

---

## 🚀 Getting Started

### Prerequisites
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (Community edition is free)
  - Workload: **.NET desktop development**
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10)
- Windows 10 or Windows 11

### Build & Run

```bash
git clone https://github.com/YOUR_USERNAME/WinOptimizer.git
cd WinOptimizer
dotnet build
dotnet run
```

Or open `WinOptimizer.sln` in Visual Studio and press **F5**.

> ⚠️ The app will prompt for **Administrator privileges** on launch — required to clean system folders and optimize drives.

### Publish as a Single EXE

```bash
dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true
```

Output: `bin\Release\net10.0-windows\win-x64\publish\WinOptimizer.exe`

---

## 📁 Project Structure

```
WinOptimizer/
├── WinOptimizer.csproj           # Project config — targets net10.0-windows
├── app.manifest                  # Requests Administrator elevation at launch
├── Program.cs                    # Entry point
├── Form1.cs                      # Main UI with tabbed layout and event handlers
├── Form1.Designer.cs             # Minimal designer file (UI built in code)
└── src/
    ├── SystemCleaner.cs          # All safe cleanup operations
    ├── DriveOptimizer.cs         # Drive detection (SSD/HDD) and optimization logic
    ├── DriveInfo.cs              # Class — holds drive metadata (letter, label, size, etc.)
    └── DriveType.cs              # Enum — defines drive types (SSD, HDD)
```

---

## 🔒 Safety Table

| Location Cleaned | Safe? | Reason |
|-----------------|-------|--------|
| `%TEMP%` | ✅ | Windows recreates automatically |
| `C:\Windows\Temp` | ✅ | Locked files are skipped |
| `C:\Windows\Prefetch` | ✅ | Windows rebuilds on next launch |
| `SoftwareDistribution\Download` | ✅ | Only already-installed update files |
| WER crash dumps | ✅ | Diagnostic only, not needed |
| Thumbnail cache | ✅ | Explorer rebuilds on demand |
| DNS Cache | ✅ | Network-layer cache only |
| Recycle Bin | ✅ | User-initiated with confirmation |
| Drive Optimize | ✅ | Uses Windows `Optimize-Volume` cmdlet |
| `C:\Users\*\Documents` | ❌ NEVER | Personal user files |
| `C:\Program Files` | ❌ NEVER | Installed applications |
| `C:\Windows\System32` | ❌ NEVER | Core OS files |
| `AppData\Roaming` | ❌ NEVER | App settings and data |

---

## 🛠️ How It Works

### Cleaning Engine (`SystemCleaner.cs`)
- Recursively deletes files using `File.SetAttributes` + `File.Delete` to handle read-only files
- Catches and silently skips any `IOException` (locked files)
- Stops and restarts `wuauserv` (Windows Update service) before cleaning its cache
- Uses the native Win32 `SHEmptyRecycleBin` API for the Recycle Bin
- Flushes DNS via `ipconfig /flushdns` subprocess

### Drive Optimizer (`DriveOptimizer.cs`)
- Detects SSD/HDD by querying `Get-PhysicalDisk` via PowerShell
- Calls `Optimize-Volume` PowerShell cmdlet (built into Windows 8+)
- Streams verbose output live to the UI log in real time
- Restores UI state via `IProgress<string>` and async/await

---

## 📦 Dependencies

| Dependency | Version | Purpose |
|-----------|---------|---------|
| .NET 10 WinForms | 10.0 | UI framework |
| Windows Shell32 | Built-in | Recycle Bin API (`SHEmptyRecycleBin`) |
| PowerShell | Built-in | Drive type detection + `Optimize-Volume` |

**No NuGet packages required.** Everything uses APIs built into Windows and .NET 10.

---

## 🗺️ Roadmap

- [ ] Startup program manager (disable slow startup apps)
- [ ] Browser cache cleaner (Chrome, Edge, Firefox)
- [ ] Visual free-space pie chart per drive
- [ ] Schedule automatic weekly cleaning (Task Scheduler integration)
- [ ] System tray mode with silent background cleaning
- [ ] Dark / Light theme toggle

## 📄 License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.

---

## ⚠️ Disclaimer

This tool performs system maintenance operations using Windows built-in APIs. While it is designed to be safe, always ensure important files are backed up before running any system maintenance tool. The author is not responsible for any data loss.

---

<div align="center">
  Made with ❤️ using .NET 10 WinForms &nbsp;|&nbsp; Windows 10 / 11
</div>
