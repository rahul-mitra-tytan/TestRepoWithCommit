using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;
using NetTrafficMonitor.Core.Data;
using NetTrafficMonitor.Core.Models;
using NetTrafficMonitor.Core.Services;
using NetTrafficMonitor.Service;
using NetTrafficMonitor.ViewModels;
using NetTrafficMonitor.Views;

namespace NetTrafficMonitor;

public partial class App : System.Windows.Application
{
    private NotifyIcon? _trayIcon;
    private NetworkMonitorService? _monitor;
    private UserPreferences? _prefs;
    private SqliteConnection? _conn;
    private MainWindow? _settingsWindow;
    private HudWindow? _hudWindow;

    private string DbPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NetTrafficMonitor", "data.db");

    private const string MutexName = "NetTrafficMonitor-SingleInstance";

    public bool MinimizeToTray
    {
        get => _prefs?.MinimizeToTray ?? false;
        set { if (_prefs is not null) _prefs.MinimizeToTray = value; }
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        using var mutex = new Mutex(true, MutexName, out bool createdNew);
        if (!createdNew) { Shutdown(); return; }

        Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);

        // Init DB
        var dbInit = new DatabaseInitializer(DbPath);
        await dbInit.InitializeAsync();
        _conn = dbInit.CreateConnection();

        // Load preferences
        _prefs = new UserPreferences();
        await _prefs.LoadAsync(_conn);

        // Apply selected theme
        ApplyTheme();

        // Start monitor
        _monitor = new NetworkMonitorService(DbPath);
        await _monitor.InitializeAsync();
        _monitor.SpeedUpdated += OnSpeedUpdated;

        // Restore selected adapter
        if (_prefs.SelectedAdapterId > 0)
            await _monitor.SelectAdapterAsync(_prefs.SelectedAdapterId);

        // Auto-select if nothing selected yet
        if (_monitor.CurrentAdapterId <= 0)
        {
            var adapters = await _monitor.RefreshAdaptersAsync();
            if (adapters.Count > 0)
                await _monitor.SelectAdapterAsync(adapters[0].Id);
        }

        _monitor.Start();

        CreateTrayIcon();

        if (!_prefs.StartMinimized)
            ShowSettingsWindow();

        ApplyTheme();
    }

    private void ApplyTheme()
{
 var theme = _prefs.Theme;
 Dispatcher.InvokeAsync(() =>
 {
   var resources = System.Windows.Application.Current?.Resources as ResourceDictionary;
   if (resources == null) return;

   var merged = resources.MergedDictionaries;
   bool hasLight = merged.Any(d => d.Source?.ToString() == "Resources/LightStyles.xaml");

   if (theme == Theme.Dark)
   {
     if (hasLight)
     {
       merged.Remove(merged.First(d => d.Source?.ToString() == "Resources/LightStyles.xaml"));
     }
   }
   else // Light or System
   {
     if (!hasLight)
     {
       merged.Insert(0, new ResourceDictionary { Source = new Uri("Resources/LightStyles.xaml", UriKind.Relative) });
     }
   }
 });
}
