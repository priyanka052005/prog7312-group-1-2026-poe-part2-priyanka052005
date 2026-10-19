namespace SmartX_Part1.Models;

public class TelemetryRecord
{
    public string DeviceId { get; set; } = "";
    public string Metric { get; set; } = "";
    public string DataType { get; set; } = "";
    public string DisplayValue { get; set; } = "";
    public string Unit { get; set; } = "";
    public DateTime Timestamp { get; set; }
}
