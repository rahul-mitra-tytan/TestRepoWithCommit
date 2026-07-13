using System.Windows;
using Microsoft.Data.Sqlite;
using NetTrafficMonitor.Core.Models;
using NetTrafficMonitor.Service;
using NetTrafficMonitor.ViewModels;

namespace NetTrafficMonitor.Views;

public partial class MainWindow : Window
{
    private readonly SettingsViewModel _vm;

    public MainWindow(NetworkMonitorService monitor, UserPreferences prefs, SqliteConnection conn)
    {
        InitializeComponent();
        _vm = new SettingsViewModel(monitor, prefs, conn);
        DataContext = _vm;
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        var app = (App)Application.Current;
        if (app.MinimizeToTray)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnClosing(e);
    }
}
