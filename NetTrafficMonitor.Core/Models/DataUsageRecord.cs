namespace NetTrafficMonitor.Core.Models;

public class DataUsageRecord
{
    public long Id { get; set; }
    public int AdapterId { get; set; }
    public long BytesDownloaded { get; set; }
    public long BytesUploaded { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
