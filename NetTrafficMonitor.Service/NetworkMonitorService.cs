using System.Diagnostics;
using System.Net.NetworkInformation;
using NetTrafficMonitor.Core.Models;
using Microsoft.Data.Sqlite;
using NetTrafficMonitor.Core.Data;

namespace NetTrafficMonitor.Service;

public class NetworkMonitorService : IDisposable
{
    private readonly string _dbPath;
    private SqliteConnection? _conn;
    private DatabaseInitializer? _dbInit;
    private AdapterRepository? _adapterRepo;

    // Performance counters (Windows only)
    private PerformanceCounter? _bytesReceivedPerSec;
    private PerformanceCounter? _bytesSentPerSec;

    private int _currentAdapterId;
    private string _currentAdapterName = "";

    private bool _running;
    private Thread? _pollThread;
    private CancellationTokenSource? _cts;

    // Latest speed sample (Bps)
    public double CurrentDownloadBps { get; private set; }
    public double CurrentUploadBps { get; private set; }

    private long _lastTotalBytesReceived;
    private long _lastTotalBytesSent;

    // Snapshot tracking for delta storage
    private long _lastSnapshotBytesReceived;
    private long _lastSnapshotBytesSent;

    public event Action<(double downBps, double upBps)>? SpeedUpdated;

    public NetworkMonitorService(string dbPath)
    {
        _dbPath = dbPath;
    }

    public async Task InitializeAsync()
    {
        _dbInit = new DatabaseInitializer(_dbPath);
        await _dbInit.InitializeAsync();
        _conn = _dbInit.CreateConnection();
        _adapterRepo = new AdapterRepository(_conn);

        var selected = await _adapterRepo.GetSelectedAsync();
        if (selected != null)
        {
            _currentAdapterId = selected.Id;
            _currentAdapterName = selected.Name;
        }
    }

    /// <summary>Scans available network adapters and merges into DB.</summary>
    public async Task<List<NetworkAdapter>> RefreshAdaptersAsync()
    {
        var adapters = new List<NetworkAdapter>();
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus == OperationalStatus.Up
                && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            {
                var adapter = new NetworkAdapter
                {
                    Name = ni.Name,
                    Description = ni.Description,
                    InterfaceGuid = ni.Id,
                    MacAddress = ni.GetPhysicalAddress().ToString(),
                    FirstSeen = DateTime.UtcNow
                };
                adapters.Add(adapter);
            }
        }

        if (_adapterRepo != null)
        {
            foreach (var a in adapters)
            {
                var existing = (await _adapterRepo.GetAllAsync()).FirstOrDefault(x => x.Name == a.Name);
                if (existing != null)
                {
                    a.Id = existing.Id;
                    a.IsSelected = existing.IsSelected;
                    await _adapterRepo.UpsertAsync(a);
                }
                else if (adapters.Count == 1)
                {
                    // Auto-select first adapter if none selected
                    a.IsSelected = true;
                    await _adapterRepo.UpsertAsync(a);
                }
                else
                {
                    await _adapterRepo.UpsertAsync(a);
                }
            }
        }

        return (await _adapterRepo!.GetAllAsync()) ?? adapters;
    }

    /// <summary>Select which adapter to monitor.</summary>
    public async Task SelectAdapterAsync(int adapterId)
    {
        if (_adapterRepo == null) return;
        await _adapterRepo.SetSelectedAsync(adapterId, true);
        var adapter = await _adapterRepo.GetByIdAsync(adapterId);
        if (adapter != null)
        {
            _currentAdapterId = adapter.Id;
            _currentAdapterName = adapter.Name;
        }
        RecreateCounters();
    }

    /// <summary>Snapshot delta bytes (since last snapshot) into data_usage table.</summary>
    private void SnapshotDelta(long deltaDown, long deltaUp)
    {
        if (_conn == null || _currentAdapterId <= 0) return;
        if (deltaDown < 0) deltaDown = 0;
        if (deltaUp < 0) deltaUp = 0;
        try
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO data_usage (AdapterId, BytesDownloaded, BytesUploaded, RecordedAt)
                                VALUES (@aid, @down, @up, @now)";
            cmd.Parameters.AddWithValue("@aid", _currentAdapterId);
            cmd.Parameters.AddWithValue("@down", deltaDown);
            cmd.Parameters.AddWithValue("@up", deltaUp);
            cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("o"));
            cmd.ExecuteNonQuery();
        }
        catch { /* ignore */ }
    }

    /// <summary>Start the polling loop.</summary>
    public void Start()
    {
        if (_running) return;
        _running = true;

        GetCurrentTotals(out long initRecv, out long initSent);
        _lastTotalBytesReceived = initRecv;
        _lastTotalBytesSent = initSent;
        _lastSnapshotBytesReceived = initRecv;
        _lastSnapshotBytesSent = initSent;

        RecreateCounters();

        _cts = new CancellationTokenSource();
        _pollThread = new Thread(() => PollLoop(_cts.Token))
        {
            IsBackground = true,
            Name = "NetMonPoll"
        };
        _pollThread.Start();
    }

    public void Stop()
    {
        _running = false;
        _cts?.Cancel();
        _pollThread?.Join(2000);
    }

    private void PollLoop(CancellationToken ct)
    {
        var sampleCount = 0;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Try performance counters first (Windows, more accurate)
                if (_bytesReceivedPerSec != null && _bytesSentPerSec != null)
                {
                    try
                    {
                        CurrentDownloadBps = _bytesReceivedPerSec.NextValue();
                        CurrentUploadBps = _bytesSentPerSec.NextValue();
                        if (CurrentDownloadBps < 0) CurrentDownloadBps = 0;
                        if (CurrentUploadBps < 0) CurrentUploadBps = 0;
                    }
                    catch
                    {
                        RecreateCounters();
                    }
                }
                else
                {
                    // Fallback: delta from NetworkInterface stats
                    GetCurrentTotals(out long recvNow, out long sentNow);
                    CurrentDownloadBps = Math.Max(0, recvNow - _lastTotalBytesReceived);
                    CurrentUploadBps = Math.Max(0, sentNow - _lastTotalBytesSent);
                    _lastTotalBytesReceived = recvNow;
                    _lastTotalBytesSent = sentNow;
                }

                SpeedUpdated?.Invoke((CurrentDownloadBps, CurrentUploadBps));

                // Snapshot delta every ~10 seconds
                sampleCount++;
                if (sampleCount % 10 == 0)
                {
                    GetCurrentTotals(out long r, out long s);
                    long deltaDown = r - _lastSnapshotBytesReceived;
                    long deltaUp = s - _lastSnapshotBytesSent;
                    if (deltaDown >= 0 && deltaUp >= 0)
                    {
                        SnapshotDelta(deltaDown, deltaUp);
                    }
                    _lastSnapshotBytesReceived = r;
                    _lastSnapshotBytesSent = s;
                }
            }
            catch
            {
                // Silently continue
            }

            Thread.Sleep(1000);
        }

        // Final delta on stop
        GetCurrentTotals(out long fr, out long fs);
        long finalDeltaDown = fr - _lastSnapshotBytesReceived;
        long finalDeltaUp = fs - _lastSnapshotBytesSent;
        if (finalDeltaDown >= 0 && finalDeltaUp >= 0)
        {
            SnapshotDelta(finalDeltaDown, finalDeltaUp);
        }
    }

    private void GetCurrentTotals(out long received, out long sent)
    {
        received = 0;
        sent = 0;
        try
        {
            var iface = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(n => n.Name == _currentAdapterName);
            if (iface != null)
            {
                var ipv4 = iface.GetIPv4Statistics();
                received = ipv4.BytesReceived;
                sent = ipv4.BytesSent;
            }
        }
        catch { }

        // If adapter didn't match by name, sum all active non-loopback
        if (received == 0 && sent == 0 && !string.IsNullOrEmpty(_currentAdapterName))
        {
            try
            {
                var stats = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections();
                // fallback: sum all up interfaces that are up
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus == OperationalStatus.Up
                        && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback
                        && ni.Supports(NetworkInterfaceComponent.IPv4))
                    {
                        var s = ni.GetIPv4Statistics();
                        received += s.BytesReceived;
                        sent += s.BytesSent;
                    }
                }
            }
            catch { }
        }
    }

    private void RecreateCounters()
    {
        try
        {
            _bytesReceivedPerSec?.Dispose();
            _bytesSentPerSec?.Dispose();

            if (!string.IsNullOrEmpty(_currentAdapterName))
            {
                _bytesReceivedPerSec = new PerformanceCounter(
                    "Network Interface", "Bytes Received/sec", _currentAdapterName, readOnly: true);
                _bytesSentPerSec = new PerformanceCounter(
                    "Network Interface", "Bytes Sent/sec", _currentAdapterName, readOnly: true);
                // Prime counters
                _bytesReceivedPerSec.NextValue();
                _bytesSentPerSec.NextValue();
            }
        }
        catch
        {
            _bytesReceivedPerSec = null;
            _bytesSentPerSec = null;
        }
    }

    public int CurrentAdapterId => _currentAdapterId;
    public string CurrentAdapterName => _currentAdapterName;

    public void Dispose()
    {
        Stop();
        try { _bytesReceivedPerSec?.Dispose(); } catch { }
        try { _bytesSentPerSec?.Dispose(); } catch { }
        _conn?.Close();
        _conn?.Dispose();
    }
}
