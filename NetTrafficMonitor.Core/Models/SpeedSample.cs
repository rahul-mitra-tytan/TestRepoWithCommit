namespace NetTrafficMonitor.Core.Models;

public class SpeedSample
{
    public long Id { get; set; }
    public int AdapterId { get; set; }
    public double DownloadBps { get; set; }   // bytes per second
    public double UploadBps { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
