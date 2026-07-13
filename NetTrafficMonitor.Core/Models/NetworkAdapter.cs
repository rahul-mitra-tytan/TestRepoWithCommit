namespace NetTrafficMonitor.Core.Models;

public class NetworkAdapter
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string InterfaceGuid { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
    public DateTime FirstSeen { get; set; } = DateTime.UtcNow;
}
