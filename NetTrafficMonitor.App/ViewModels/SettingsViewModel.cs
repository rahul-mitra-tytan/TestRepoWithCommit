using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Data.Sqlite;
using NetTrafficMonitor.Core.Data;
using NetTrafficMonitor.Core.Models;
using NetTrafficMonitor.Core.Services;
using NetTrafficMonitor.Service;

namespace NetTrafficMonitor.ViewModels;

public class SettingsViewModel : INotifyPropertyChanged
{
    private readonly NetworkMonitorService _monitor;
    private readonly UserPreferences _prefs;
    private readonly SqliteConnection _conn;
    private readonly AdapterRepository _adapterRepo;
    private readonly DataUsageAggregator _aggregator;

    public SettingsViewModel(NetworkMonitorService monitor, UserPreferences prefs, SqliteConnection conn)
    {
        _monitor = monitor;
        _prefs = prefs;
        _conn = conn;
        _adapterRepo = new AdapterRepository(conn);
        _aggregator = new DataUsageAggregator(conn);

        _speedUnits = new ObservableCollection<SpeedUnit>(
            Enum.GetValues<SpeedUnit>());
        _selectedUnit = _prefs.DisplayUnit;

        _dataPeriods = new ObservableCollection<DataPeriod>(
            Enum.GetValues<DataPeriod>());
        _selectedPeriod = DataPeriod.Today;

        _adapters = new ObservableCollection<NetworkAdapter>();
        _selectedAdapter = null;

        LoadAdaptersCommand = new AsyncRelayCommand(async () => await LoadAdaptersAsync());
        SaveCommand = new AsyncRelayCommand(async () => await SaveAsync());
        RefreshUsageCommand = new AsyncRelayCommand(async () => await RefreshUsageAsync());

        _ = LoadAdaptersAsync();
        _ = RefreshUsageAsync();
    }

    public ObservableCollection<SpeedUnit> SpeedUnits => _speedUnits;
    private readonly ObservableCollection<SpeedUnit> _speedUnits;

    public SpeedUnit SelectedUnit
    {
        get => _selectedUnit;
        set { _selectedUnit = value; OnPropertyChanged(); }
    }
    private SpeedUnit _selectedUnit;

    public ObservableCollection<DataPeriod> DataPeriods => _dataPeriods;
    private readonly ObservableCollection<DataPeriod> _dataPeriods;

    public DataPeriod SelectedPeriod
    {
        get => _selectedPeriod;
        set { _selectedPeriod = value; OnPropertyChanged(); _ = RefreshUsageAsync(); }
    }
    private DataPeriod _selectedPeriod;

    public ObservableCollection<NetworkAdapter> Adapters => _adapters;
    private readonly ObservableCollection<NetworkAdapter> _adapters;

    public NetworkAdapter? SelectedAdapter
    {
        get => _selectedAdapter;
        set { _selectedAdapter = value; OnPropertyChanged(); }
    }
    private NetworkAdapter? _selectedAdapter;

    public string FontFamily
    {
        get => _prefs.FontFamily;
        set { _prefs.FontFamily = value; OnPropertyChanged(); }
    }

    public double FontSize
    {
        get => _prefs.FontSize;
        set { _prefs.FontSize = value; OnPropertyChanged(); }
    }

    public bool StartMinimized
    {
        get => _prefs.StartMinimized;
        set { _prefs.StartMinimized = value; OnPropertyChanged(); }
    }

    public bool MinimizeToTray
    {
        get => _prefs.MinimizeToTray;
        set { _prefs.MinimizeToTray = value; OnPropertyChanged(); }
    }

    public bool RunOnStartup
    {
        get => _prefs.RunOnStartup;
        set { _prefs.RunOnStartup = value; OnPropertyChanged(); }
    }

    public bool HudEnabled
    {
        get => _prefs.HudEnabled;
        set { _prefs.HudEnabled = value; OnPropertyChanged(); }
    }

    public double HudOpacity
    {
        get => _prefs.HudOpacity;
        set { _prefs.HudOpacity = value; OnPropertyChanged(); }
    }

    public string PeriodFormattedDownload => SpeedConverter.Format(PeriodDownloadBytes, _prefs.DisplayUnit);
    public string PeriodFormattedUpload => SpeedConverter.Format(PeriodUploadBytes, _prefs.DisplayUnit);

    public long PeriodDownloadBytes { get; private set; }
    public long PeriodUploadBytes { get; private set; }

    public ICommand LoadAdaptersCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand RefreshUsageCommand { get; }

    private async Task LoadAdaptersAsync()
    {
        var adapters = await _monitor.RefreshAdaptersAsync();
        _adapters.Clear();
        foreach (var a in adapters)
        {
            _adapters.Add(a);
            if (a.IsSelected)
                _selectedAdapter = a;
        }
        OnPropertyChanged(nameof(SelectedAdapter));
    }

    private async Task SaveAsync()
    {
        _prefs.DisplayUnit = _selectedUnit;

        if (_selectedAdapter != null)
        {
            await _monitor.SelectAdapterAsync(_selectedAdapter.Id);
            _prefs.SelectedAdapterId = _selectedAdapter.Id;
        }

        await _prefs.SaveAsync(_conn);
    }

    private async Task RefreshUsageAsync()
    {
        int adapterId = _selectedAdapter?.Id ?? _monitor.CurrentAdapterId;
        if (adapterId <= 0) return;

        PeriodDownloadBytes = await _aggregator.GetBytesDownloadedAsync(adapterId, _selectedPeriod);
        PeriodUploadBytes = await _aggregator.GetBytesUploadedAsync(adapterId, _selectedPeriod);
        OnPropertyChanged(nameof(PeriodDownloadBytes));
        OnPropertyChanged(nameof(PeriodUploadBytes));
        OnPropertyChanged(nameof(PeriodFormattedDownload));
        OnPropertyChanged(nameof(PeriodFormattedUpload));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

// Simple async relay command
public class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private bool _isExecuting;

    public AsyncRelayCommand(Func<Task> execute) => _execute = execute;

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !_isExecuting;

    public async void Execute(object? parameter)
    {
        if (_isExecuting) return;
        _isExecuting = true;
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        try { await _execute(); }
        finally
        {
            _isExecuting = false;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
