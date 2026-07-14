using NetTrafficMonitor.Core.Models;

namespace NetTrafficMonitor.Core.Services;

public static class DataSizeConverter
{
    /// <summary>Convert bytes to the target data size unit, returning a display string.</summary>
    public static string Format(long bytes, DataSizeUnit unit)
    {
        double value = unit switch
        {
            DataSizeUnit.KB => bytes / 1024.0,
            DataSizeUnit.MB => bytes / (1024.0 * 1024.0),
            DataSizeUnit.GB => bytes / (1024.0 * 1024.0 * 1024.0),
            DataSizeUnit.TB => bytes / (1024.0 * 1024.0 * 1024.0 * 1024.0),
            _ => bytes
        };
        string suffix = unit switch
        {
            DataSizeUnit.KB => "KB",
            DataSizeUnit.MB => "MB",
            DataSizeUnit.GB => "GB",
            DataSizeUnit.TB => "TB",
            _ => "B"
        };
        // Use F2 for KB, F2 for MB/GB, auto-precision
        string format = unit == DataSizeUnit.KB ? "F1" : "F2";
        return $"{value.ToString(format)} {suffix}";
    }

    /// <summary>Raw numeric value in the target unit.</summary>
    public static double Convert(long bytes, DataSizeUnit unit)
    {
        return unit switch
        {
            DataSizeUnit.KB => bytes / 1024.0,
            DataSizeUnit.MB => bytes / (1024.0 * 1024.0),
            DataSizeUnit.GB => bytes / (1024.0 * 1024.0 * 1024.0),
            DataSizeUnit.TB => bytes / (1024.0 * 1024.0 * 1024.0 * 1024.0),
            _ => bytes
        };
    }
}