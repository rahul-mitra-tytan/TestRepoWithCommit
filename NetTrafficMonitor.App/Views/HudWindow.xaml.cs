using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NetTrafficMonitor.Core.Models;
using NetTrafficMonitor.Core.Services;
using NetTrafficMonitor.Service;

namespace NetTrafficMonitor.Views;

public enum HudViewMode
{
    Speed,
    Graph
}

public partial class HudWindow : Window, INotifyPropertyChanged
{
    private readonly NetworkMonitorService _monitor;
    private readonly UserPreferences _prefs;

    private const int MaxHistory = 60;

    public bool ClickThrough { get; set; }

    public HudViewMode ViewMode
    {
        get => _viewMode;
        set
        {
            if (_viewMode == value) return;
            _viewMode = value;
            OnPropertyChanged(nameof(ViewMode));
            UpdateViewVisibility();
        }
    }
    private HudViewMode _viewMode;

    public SpeedUnit DisplayUnit
    {
        get => _displayUnit;
        set
        {
            if (_displayUnit == value) return;
            _displayUnit = value;
            _prefs.DisplayUnit = value;
            OnPropertyChanged(nameof(DisplayUnit));
            UpdateSpeedText();
        }
    }
    private SpeedUnit _displayUnit;

    private readonly Queue<double> _downloadHistory = new();
    private readonly Queue<double> _uploadHistory = new();

    public HudWindow(NetworkMonitorService monitor, UserPreferences prefs)
    {
        InitializeComponent();
        _monitor = monitor;
        _prefs = prefs;
        _displayUnit = prefs.DisplayUnit;
        _viewMode = HudViewMode.Speed;

        DataContext = this;

        // Position: bottom-right corner
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        var screenHeight = SystemParameters.PrimaryScreenHeight;
        Left = screenWidth - Width - 20;
        Top = screenHeight - Height - 60;
        Opacity = prefs.HudOpacity;

        UpdateSpeed(monitor.CurrentDownloadBps, monitor.CurrentUploadBps);

        // Subscribe for live updates
        _monitor.SpeedUpdated += OnSpeedUpdated;

        // Right-click to open context menu
        MouseRightButtonUp += (_, __) => ContextMenu?.IsOpen = true;
    }

    private void OnSpeedUpdated((double downBps, double upBps) speed)
    {
        Dispatcher.Invoke(() =>
        {
            // Update text in speed mode
            if (_viewMode == HudViewMode.Speed)
            {
                UpdateSpeedText();
            }

            // Push history for graph mode
            _downloadHistory.Enqueue(speed.downBps);
            _uploadHistory.Enqueue(speed.upBps);

            while (_downloadHistory.Count > MaxHistory)
                _downloadHistory.Dequeue();
            while (_uploadHistory.Count > MaxHistory)
                _uploadHistory.Dequeue();

            if (_viewMode == HudViewMode.Graph)
                DrawGraph();
        });
    }

    private void UpdateSpeedText()
    {
        if (DownloadText != null && UploadText != null)
        {
            DownloadText.Text = SpeedConverter.Format(_monitor.CurrentDownloadBps, _displayUnit);
            UploadText.Text = SpeedConverter.Format(_monitor.CurrentUploadBps, _displayUnit);
        }
    }

    private void UpdateViewVisibility()
    {
        if (SpeedView != null)
            SpeedView.Visibility = _viewMode == HudViewMode.Speed ? Visibility.Visible : Visibility.Collapsed;

        if (GraphView != null)
        {
            GraphView.Visibility = _viewMode == HudViewMode.Graph ? Visibility.Visible : Visibility.Collapsed;
            if (_viewMode == HudViewMode.Graph)
                DrawGraph();
        }
    }

    private void DrawGraph()
    {
        if (GraphView == null || DownloadGraph == null || UploadGraph == null)
            return;

        if (_downloadHistory.Count < 2)
            return;

        double w = GraphView.ActualWidth;
        double h = GraphView.ActualHeight;
        if (w <= 0 || h <= 0)
            return;

        // Find max for scaling
        double max = 0;
        foreach (var v in _downloadHistory)
            if (v > max) max = v;
        foreach (var v in _uploadHistory)
            if (v > max) max = v;
        if (max <= 0) max = 1;

        // Build polyline points
        var dl = new List<Point>();
        var up = new List<Point>();

        int i = 0;
        foreach (var d in _downloadHistory)
        {
            double x = (i / (double)(MaxHistory - 1)) * w;
            double y = h - (d / max) * (h - 10) - 5; // 5px padding
            dl.Add(new Point(x, y));
            i++;
        }

        i = 0;
        foreach (var u in _uploadHistory)
        {
            double x = (i / (double)(MaxHistory - 1)) * w;
            double y = h - (u / max) * (h - 10) - 5;
            up.Add(new Point(x, y));
            i++;
        }

        DownloadGraph.Data = Geometry.Parse(PolylinePoints(dl));
        UploadGraph.Data = Geometry.Parse(PolylinePoints(up));
    }

    private static string PolylinePoints(IList<Point> pts)
    {
        if (pts.Count == 0) return string.Empty;
        var s = $"M {pts[0].X},{pts[0].Y}";
        for (int i = 1; i < pts.Count; i++)
            s += $" L {pts[i].X},{pts[i].Y}";
        return s;
    }

    private void OnSpeedViewClick(object sender, RoutedEventArgs e) => ViewMode = HudViewMode.Speed;
    private void OnGraphViewClick(object sender, RoutedEventArgs e) => ViewMode = HudViewMode.Graph;

    private void OnUnitClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menu && menu.Tag is string tag)
        {
            if (Enum.TryParse<SpeedUnit>(tag, true, out var unit))
            {
                DisplayUnit = unit;
            }
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();

        base.OnMouseDown(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _monitor.SpeedUpdated -= OnSpeedUpdated;
        base.OnClosed(e);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
