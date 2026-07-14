using Microsoft.Data.Sqlite;
using System.Globalization;

namespace NetTrafficMonitor.Core.Models;

public class UserPreferences
{
    private const string TableName = "user_preferences";

    // Defaults
    public SpeedUnit DisplayUnit { get; set; } = SpeedUnit.Mbps;
    public DataSizeUnit DataUsageDisplayUnit { get; set; } = DataSizeUnit.MB;
    public double PollingIntervalSeconds { get; set; } = 1.0;
    public string FontFamily { get; set; } = "Segoe UI";
    public double FontSize { get; set; } = 12.0;
    public bool StartMinimized { get; set; } = true;
    public bool MinimizeToTray { get; set; } = true;
    public bool ShowInTaskbar { get; set; } = false;
    public bool RunOnStartup { get; set; } = false;
    public bool HudEnabled { get; set; } = false;
    public double HudOpacity { get; set; } = 0.7;
    public bool HudClickThrough { get; set; } = false;
    public int SelectedAdapterId { get; set; } = 0;

    public async Task LoadAsync(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT key, value FROM {TableName}";
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var key = reader.GetString(0);
            var val = reader.GetString(1);
            Apply(key, val);
        }
    }

    public async Task SaveAsync(SqliteConnection conn)
    {
        var props = GetType().GetProperties()
            .Where(p => p.Name != nameof(DisplayUnit)); // we store unit specially
        using var tx = await conn.BeginTransactionAsync();
        foreach (var prop in props)
        {
            var val = prop.GetValue(this)?.ToString() ?? "";
            await UpsertAsync(conn, prop.Name, val);
        }
        await UpsertAsync(conn, nameof(DisplayUnit), ((int)DisplayUnit).ToString());
        await tx.CommitAsync();
    }

    private async Task UpsertAsync(SqliteConnection conn, string key, string value)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"
            INSERT INTO {TableName} (key, value) VALUES (@k, @v)
            ON CONFLICT(key) DO UPDATE SET value = @v";
        cmd.Parameters.AddWithValue("@k", key);
        cmd.Parameters.AddWithValue("@v", value);
        await cmd.ExecuteNonQueryAsync();
    }

    private void Apply(string key, string val)
    {
        switch (key)
        {
            case nameof(DisplayUnit):
                DisplayUnit = (SpeedUnit)int.Parse(val, CultureInfo.InvariantCulture);
                break;
            case nameof(DataUsageDisplayUnit):
                DataUsageDisplayUnit = (DataSizeUnit)int.Parse(val, CultureInfo.InvariantCulture);
                break;
            case nameof(PollingIntervalSeconds):
                PollingIntervalSeconds = double.Parse(val, CultureInfo.InvariantCulture);
                break;
            case nameof(FontSize):
                FontSize = double.Parse(val, CultureInfo.InvariantCulture);
                break;
            case nameof(HudOpacity):
                HudOpacity = double.Parse(val, CultureInfo.InvariantCulture);
                break;
            case nameof(StartMinimized):
                StartMinimized = bool.Parse(val);
                break;
            case nameof(MinimizeToTray):
                MinimizeToTray = bool.Parse(val);
                break;
            case nameof(ShowInTaskbar):
                ShowInTaskbar = bool.Parse(val);
                break;
            case nameof(RunOnStartup):
                RunOnStartup = bool.Parse(val);
                break;
            case nameof(HudEnabled):
                HudEnabled = bool.Parse(val);
                break;
            case nameof(HudClickThrough):
                HudClickThrough = bool.Parse(val);
                break;
            case nameof(SelectedAdapterId):
                SelectedAdapterId = int.Parse(val, CultureInfo.InvariantCulture);
                break;
            case nameof(FontFamily):
                FontFamily = val;
                break;
        }
    }
}
