using Microsoft.Data.Sqlite;

namespace NetTrafficMonitor.Core.Data;

public class DatabaseInitializer
{
    private readonly string _connectionString;

    public DatabaseInitializer(string dbPath)
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();
    }

    public SqliteConnection CreateConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    public async Task InitializeAsync()
    {
        using var conn = CreateConnection();

        // Network adapters
        using var cmd1 = conn.CreateCommand();
        cmd1.CommandText = @"
            CREATE TABLE IF NOT EXISTS network_adapters (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Description TEXT,
                InterfaceGuid TEXT,
                MacAddress TEXT,
                IsSelected INTEGER NOT NULL DEFAULT 0,
                FirstSeen TEXT NOT NULL
            )";
        await cmd1.ExecuteNonQueryAsync();

        // Data usage cumulative records (snapshots)
        using var cmd2 = conn.CreateCommand();
        cmd2.CommandText = @"
            CREATE TABLE IF NOT EXISTS data_usage (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                AdapterId INTEGER NOT NULL,
                BytesDownloaded INTEGER NOT NULL DEFAULT 0,
                BytesUploaded INTEGER NOT NULL DEFAULT 0,
                RecordedAt TEXT NOT NULL,
                FOREIGN KEY(AdapterId) REFERENCES network_adapters(Id)
            )";
        await cmd2.ExecuteNonQueryAsync();

        // Speed samples (high-frequency)
        using var cmd3 = conn.CreateCommand();
        cmd3.CommandText = @"
            CREATE TABLE IF NOT EXISTS speed_samples (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                AdapterId INTEGER NOT NULL,
                DownloadBps REAL NOT NULL DEFAULT 0,
                UploadBps REAL NOT NULL DEFAULT 0,
                Timestamp TEXT NOT NULL,
                FOREIGN KEY(AdapterId) REFERENCES network_adapters(Id)
            )";
        await cmd3.ExecuteNonQueryAsync();

        // User preferences (key-value)
        using var cmd4 = conn.CreateCommand();
        cmd4.CommandText = @"
            CREATE TABLE IF NOT EXISTS user_preferences (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            )";
        await cmd4.ExecuteNonQueryAsync();

        // Indexes for perf
        using var cmd5 = conn.CreateCommand();
        cmd5.CommandText = "CREATE INDEX IF NOT EXISTS idx_data_usage_adapter ON data_usage(AdapterId, RecordedAt)";
        await cmd5.ExecuteNonQueryAsync();

        using var cmd6 = conn.CreateCommand();
        cmd6.CommandText = "CREATE INDEX IF NOT EXISTS idx_speed_samples_adapter ON speed_samples(AdapterId, Timestamp)";
        await cmd6.ExecuteNonQueryAsync();
    }
}
