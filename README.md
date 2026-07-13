# NetTrafficMonitor

A lightweight, open-source **Windows network monitor** built with .NET 9 and WPF. It tracks real-time upload/download speeds, logs historical usage to a local SQLite database, and displays everything through a modern dark-themed tray icon, settings window, and optional floating HUD.

---

## Screenshots

| Tray Icon | Settings Window | HUD Overlay |
|-----------|-----------------|-------------|
| *(right-click tray → Show Settings)* | Three-tab WPF window | Transparency + drag + click-through |

---

## Features

- **Real-time speed monitoring** — poll your active network adapter at 1 Hz using Windows Performance Counters (`Bytes Received/sec` / `Bytes Sent/sec`).
- **Historical data** — cumulative snapshots are stored in a local SQLite database; browse daily / weekly / monthly summaries.
- **Multiple display units** — Bps, KBps, MBps, GBps, bps, Kbps, Mbps, Gbps.
- **System Tray integration** — minimize to tray, optional startup, clean right-click menu.
- **Floating HUD** — optional always-on-top overlay window, draggable, configurable opacity, click-through mode.
- **Persistent settings** — adapter selection, font, speed unit, window preferences all saved to SQLite.
- **Clean architecture** — three C# projects: `Core` (models + data), `Service` (polling engine), `App` (WPF UI).

---

## Architecture

```
NetTrafficMonitor.sln
├── NetTrafficMonitor.Core   [net9.0]
│   ├── Models/              (NetworkAdapter, DataUsageRecord, SpeedSample, SpeedUnit, DataPeriod, UserPreferences)
│   ├── Data/                (DatabaseInitializer, AdapterRepository)
│   └── Services/            (SpeedConverter, DataUsageAggregator)
│
├── NetTrafficMonitor.Service [net9.0]
│   └── NetworkMonitorService  (PerformanceCounter polling, adapter scan, DB snapshots)
│
└── NetTrafficMonitor.App   [net9.0-windows]
    ├── Views/               (MainWindow settings, HudWindow overlay)
    ├── ViewModels/          (SettingsViewModel, AsyncRelayCommand)
    └── Converters/          (SpeedUnitToStringConverter, DataPeriodToStringConverter)
```

### Tech stack

| Layer | Technology |
|-------|-----------|
| Runtime | .NET 9 |
| UI | WPF + Windows Forms (`NotifyIcon`) |
| Storage | Microsoft.Data.Sqlite 9.0.4 (shared `trafficmonitor.db` file) |
| Native counters | `System.Diagnostics.PerformanceCounter` 9.0.4 |
| OS APIs | `System.Net.NetworkInformation`, `System.Management` |

---

## Getting started

### Prerequisites

- **Windows 10 / 11**
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- (Optional) Visual Studio 2022 / VS Code with C# Dev Kit

### Build

```powershell
# restore + build everything (requires Windows SDK for the App project)
dotnet build -c Release -p:EnableWindowsTargeting=true

# or build just the Core + Service projects (these are cross-platform)
dotnet build NetTrafficMonitor.Core/NetTrafficMonitor.Core.csproj
dotnet build NetTrafficMonitor.Service/NetTrafficMonitor.Service.csproj
```

### Publish (self-contained, single folder)

```powershell
dotnet publish NetTrafficMonitor.App/NetTrafficMonitor.App.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:EnableWindowsTargeting=true `
  -o publish
```

The output directory (`publish/`) contains everything needed to run — no separate installer required.

### Run

```powershell
# from the repo root
dotnet run --project NetTrafficMonitor.App

# or after publishing
.\publish\NetTrafficMonitor.exe
```

The app starts minimized to the system tray. right-click the tray icon → **Show Settings**.

---

## Using the app

### General tab

- **Network Adapter** — pick which interface to monitor (or click *Refresh Adapters* to re-scan).
- **Display Unit** — choose the speed unit (Mbps default).
- **Font** — customize family and size for the UI and HUD.
- **Behaviour** — *Start minimized*, *Minimize to tray*, *Run on Windows startup*.

### HUD tab

- Toggle the floating overlay.
- Adjust **Opacity** and **Font Size**.
- Enable **Click-through** to interact with apps underneath the HUD.

### Data Usage tab

- Browse cumulative usage for **Today / This Week / This Month** with Download / Upload totals.
- Click **Refresh** to update.

### Tray menu

| Action | Description |
|--------|-------------|
| Show Settings | Open the main WPF window |
| Toggle HUD | Show / hide the floating overlay |
| Exit | Stop monitoring and close |

---

## Database schema

| Table | Purpose |
|-------|---------|
| `network_adapters` | Discovered NICs, selection flag, MAC address |
| `data_usage` | Periodic cumulative snapshots (bytes sent/received) |
| `speed_samples` | High-frequency per-second speed readings |
| `user_preferences` | Key-value store for all settings |

The SQLite file lives next to the executable: `trafficmonitor.db`.

---

## Project structure walk-through

### Core

- **`DatabaseInitializer`** — creates all tables + indexes on first run.
- **`AdapterRepository`** — upsert / query `network_adapters`, set the active adapter.
- **`DataUsageAggregator`** — sums `data_usage` rows per adapter + time window.
- **`SpeedConverter`** — converts raw bytes/sec to any `SpeedUnit` enum value.
- **`SpeedUnit`** enum — `Bps, KBps, MBps, GBps, bps, Kbps, Mbps, Gbps`.
- **`DataPeriod`** enum — `Today, ThisWeek, ThisMonth`.
- **`UserPreferences`** — POCO with `LoadAsync` / `SaveAsync` against `user_preferences`.

### Service

- **`NetworkMonitorService`** — the polling engine:
  1. Prefers Windows Performance Counters when available (per-adapter `Bytes Received/sec` / `Bytes Sent/sec`).
  2. Falls back to `NetworkInterface.GetIPv4Statistics()` delta on non-Windows or when counters fail.
  3. Fires `SpeedUpdated` every second.
  4. Writes cumulative snapshots to `data_usage` every 10 s.
  5. Exposes `RefreshAdaptersAsync` and `SelectAdapterAsync` for the UI.

### App

- **`MainWindow`** — three-tab WPF settings window bound to `SettingsViewModel`.
- **`HudWindow`** — borderless, transparent, always-on-top overlay driven by `NetworkMonitorService.SpeedUpdated`.
- **`App.xaml.cs`** (not shown in summary) — bootstraps DI, creates the SQLite connection + service, handles tray icon via `NotifyIcon`, manages HUD lifetime.

---

## Troubleshooting

| Issue | Fix |
|-------|-----|
| `NETSDK1137` — "The project is using Windows-specific features" | Pass `-p:EnableWindowsTargeting=true` to `dotnet build` / `dotnet publish`. |
| Performance counters throw `InvalidOperationException` | The service falls back to `NetworkInterface` stats automatically; no action needed. |
| No adapters shown | Click **Refresh Adapters**; permissions are not required — `NetworkInterface.GetAllNetworkInterfaces()` is read-only. |
| HUD is clickable when it shouldn't be | Enable **Click-through** in the HUD tab — this sets `IsHitTestVisible = false` on the HUD window. |

---

## Roadmap ideas

- [ ] Per-adapter speed history chart
- [ ] Daily / weekly / monthly data-usage export to CSV
- [ ] Multi-language support
- [ ] Auto-detect and warn near ISP data cap
- [ ] Plugin architecture for custom counters

---

## Contributing

1. Fork the repo  
2. Create a feature branch (`git checkout -b feature/my-feature`)  
3. Commit your changes (`git commit -m "Add feature"`)  
4. Push (`git push origin feature/my-feature`)  
5. Open a Pull Request

---

## License

MIT — see [LICENSE](LICENSE) for details.
