using Microsoft.Data.Sqlite;
using NetTrafficMonitor.Core.Models;

namespace NetTrafficMonitor.Core.Data;

public class AdapterRepository
{
    private readonly SqliteConnection _conn;

    public AdapterRepository(SqliteConnection conn) => _conn = conn;

    public async Task<List<NetworkAdapter>> GetAllAsync()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Description, InterfaceGuid, MacAddress, IsSelected, FirstSeen FROM network_adapters ORDER BY Name";
        var list = new List<NetworkAdapter>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new NetworkAdapter
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                InterfaceGuid = reader.GetString(3),
                MacAddress = reader.GetString(4),
                IsSelected = reader.GetBoolean(5),
                FirstSeen = DateTime.Parse(reader.GetString(6))
            });
        }
        return list;
    }

    public async Task<NetworkAdapter?> GetByIdAsync(int id)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Description, InterfaceGuid, MacAddress, IsSelected, FirstSeen FROM network_adapters WHERE Id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new NetworkAdapter
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                InterfaceGuid = reader.GetString(3),
                MacAddress = reader.GetString(4),
                IsSelected = reader.GetBoolean(5),
                FirstSeen = DateTime.Parse(reader.GetString(6))
            };
        }
        return null;
    }

    public async Task<int> UpsertAsync(NetworkAdapter adapter)
    {
        if (adapter.Id > 0)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"UPDATE network_adapters SET Name=@n, Description=@d, InterfaceGuid=@ig, MacAddress=@m, IsSelected=@s WHERE Id=@id";
            cmd.Parameters.AddWithValue("@n", adapter.Name);
            cmd.Parameters.AddWithValue("@d", adapter.Description);
            cmd.Parameters.AddWithValue("@ig", adapter.InterfaceGuid);
            cmd.Parameters.AddWithValue("@m", adapter.MacAddress);
            cmd.Parameters.AddWithValue("@s", adapter.IsSelected);
            cmd.Parameters.AddWithValue("@id", adapter.Id);
            await cmd.ExecuteNonQueryAsync();
            return adapter.Id;
        }
        else
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO network_adapters (Name, Description, InterfaceGuid, MacAddress, IsSelected, FirstSeen) VALUES (@n,@d,@ig,@m,@s,@fs);
                                SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@n", adapter.Name);
            cmd.Parameters.AddWithValue("@d", adapter.Description);
            cmd.Parameters.AddWithValue("@ig", adapter.InterfaceGuid);
            cmd.Parameters.AddWithValue("@m", adapter.MacAddress);
            cmd.Parameters.AddWithValue("@s", adapter.IsSelected);
            cmd.Parameters.AddWithValue("@fs", adapter.FirstSeen.ToString("o"));
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
    }

    public async Task SetSelectedAsync(int adapterId, bool selected)
    {
        // Clear all
        using var cmd1 = _conn.CreateCommand();
        cmd1.CommandText = "UPDATE network_adapters SET IsSelected = 0";
        await cmd1.ExecuteNonQueryAsync();

        // Set selected
        using var cmd2 = _conn.CreateCommand();
        cmd2.CommandText = "UPDATE network_adapters SET IsSelected = 1 WHERE Id=@id";
        cmd2.Parameters.AddWithValue("@id", adapterId);
        await cmd2.ExecuteNonQueryAsync();
    }

    public async Task<NetworkAdapter?> GetSelectedAsync()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Description, InterfaceGuid, MacAddress, IsSelected, FirstSeen FROM network_adapters WHERE IsSelected=1 LIMIT 1";
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new NetworkAdapter
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                InterfaceGuid = reader.GetString(3),
                MacAddress = reader.GetString(4),
                IsSelected = reader.GetBoolean(5),
                FirstSeen = DateTime.Parse(reader.GetString(6))
            };
        }
        return null;
    }
}
