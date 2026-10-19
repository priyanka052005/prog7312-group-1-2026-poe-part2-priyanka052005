namespace SmartX_Part1.Models;

// Required generic wrapper.
// T can be double, int, bool, string, etc.
public class TelemetryPacket<T>
{
    public string DeviceId { get; set; } = "";
    public string Metric { get; set; } = "";
    public T Value { get; set; } = default!;
    public string Unit { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
