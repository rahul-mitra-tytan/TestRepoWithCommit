using Microsoft.Data.Sqlite;
using NetTrafficMonitor.Core.Models;

namespace NetTrafficMonitor.Core.Services;

public class DataUsageAggregator
{
    private readonly SqliteConnection _conn;

    public DataUsageAggregator(SqliteConnection conn) => _conn = conn;

    public async Task<long> GetBytesDownloadedAsync(int adapterId, DataPeriod period, DateTime? customStart = null, DateTime? customEnd = null)
    {
        var since = GetSinceDate(period, customStart);
        var until = customEnd ?? DateTime.UtcNow;
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(SUM(BytesDownloaded),0) FROM data_usage WHERE AdapterId=@aid AND RecordedAt>=@since AND RecordedAt<=@until";
        cmd.Parameters.AddWithValue("@aid", adapterId);
        cmd.Parameters.AddWithValue("@since", since.ToString("o"));
        cmd.Parameters.AddWithValue("@until", until.ToString("o"));
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    public async Task<long> GetBytesUploadedAsync(int adapterId, DataPeriod period, DateTime? customStart = null, DateTime? customEnd = null)
    {
        var since = GetSinceDate(period, customStart);
        var until = customEnd ?? DateTime.UtcNow;
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(SUM(BytesUploaded),0) FROM data_usage WHERE AdapterId=@aid AND RecordedAt>=@since AND RecordedAt<=@until";
        cmd.Parameters.AddWithValue("@aid", adapterId);
        cmd.Parameters.AddWithValue("@since", since.ToString("o"));
        cmd.Parameters.AddWithValue("@until", until.ToString("o"));
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    private static DateTime GetSinceDate(DataPeriod period, DateTime? customStart)
    {
        if (period == DataPeriod.Custom && customStart.HasValue)
            return customStart.Value.Date;

        return period switch
        {
            DataPeriod.Today => DateTime.UtcNow.Date,
            DataPeriod.ThisWeek => DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek),
            DataPeriod.ThisMonth => new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            _ => DateTime.UtcNow.Date
        };
    }
}
