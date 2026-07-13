using NetTrafficMonitor.Core.Models;

namespace NetTrafficMonitor.Core.Services;

public static class SpeedConverter
{
    /// <summary>Convert bytes-per-second to the target unit, returning a display string.</summary>
    public static string Format(double bytesPerSecond, SpeedUnit unit)
    {
        double bits = bytesPerSecond * 8;
        double value = unit switch
        {
            SpeedUnit.Bps  => bytesPerSecond,
            SpeedUnit.KBps => bytesPerSecond / 1024.0,
            SpeedUnit.MBps => bytesPerSecond / (1024.0 * 1024.0),
            SpeedUnit.GBps => bytesPerSecond / (1024.0 * 1024.0 * 1024.0),
            SpeedUnit.bps  => bits,
            SpeedUnit.Kbps => bits / 1000.0,
            SpeedUnit.Mbps => bits / 1_000_000.0,
            SpeedUnit.Gbps => bits / 1_000_000_000.0,
            _ => bytesPerSecond
        };
        string suffix = unit switch
        {
            SpeedUnit.Bps  => "B/s",
            SpeedUnit.KBps => "KB/s",
            SpeedUnit.MBps => "MB/s",
            SpeedUnit.GBps => "GB/s",
            SpeedUnit.bps  => "b/s",
            SpeedUnit.Kbps => "Kb/s",
            SpeedUnit.Mbps => "Mb/s",
            SpeedUnit.Gbps => "Gb/s",
            _ => "B/s"
        };
        return $"{value:F2} {suffix}";
    }

    /// <summary>Raw numeric value in the target unit (for tooltip / HUD).</summary>
    public static double Convert(double bytesPerSecond, SpeedUnit unit)
    {
        double bits = bytesPerSecond * 8;
        return unit switch
        {
            SpeedUnit.Bps  => bytesPerSecond,
            SpeedUnit.KBps => bytesPerSecond / 1024.0,
            SpeedUnit.MBps => bytesPerSecond / (1024.0 * 1024.0),
            SpeedUnit.GBps => bytesPerSecond / (1024.0 * 1024.0 * 1024.0),
            SpeedUnit.bps  => bits,
            SpeedUnit.Kbps => bits / 1000.0,
            SpeedUnit.Mbps => bits / 1_000_000.0,
            SpeedUnit.Gbps => bits / 1_000_000_000.0,
            _ => bytesPerSecond
        };
    }
}
