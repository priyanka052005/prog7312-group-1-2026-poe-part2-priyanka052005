namespace SmartX_Part1.Models;

public class SensorAttachment
{
    public string DeviceId { get; set; } = "";
    public string OriginalFileName { get; set; } = "";
    public string StoredFileName { get; set; } = "";
    public DateTime UploadedAt { get; set; }
    public long SizeBytes { get; set; }
}
