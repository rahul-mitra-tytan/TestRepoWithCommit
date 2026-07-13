using System.Windows;
using System.Windows.Input;
using NetTrafficMonitor.Core.Models;
using NetTrafficMonitor.Core.Services;
using NetTrafficMonitor.Service;

namespace NetTrafficMonitor.Views;

public partial class HudWindow : Window
{
    private readonly NetworkMonitorService _monitor;
    private readonly UserPreferences _prefs;

    public bool ClickThrough { get; set; }

    public HudWindow(NetworkMonitorService monitor, UserPreferences prefs)
    {
        InitializeComponent();
        _monitor = monitor;
        _prefs = prefs;

        // Position: bottom-right corner
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        var screenHeight = SystemParameters.PrimaryScreenHeight;
        Left = screenWidth - Width - 20;
        Top = screenHeight - Height - 60;

        Opacity = prefs.HudOpacity;

        UpdateSpeed(monitor.CurrentDownloadBps, monitor.CurrentUploadBps);
    }

    public void UpdateSpeed(double downBps, double upBps)
    {
        var downStr = SpeedConverter.Format(downBps, _prefs.DisplayUnit);
        var upStr = SpeedConverter.Format(upBps, _prefs.DisplayUnit);

        Dispatcher.Invoke(() =>
        {
            DownloadText.Text = downStr;
            UploadText.Text = upStr;
        });
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
        base.OnMouseDown(e);
    }
}
