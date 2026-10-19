namespace SmartX_Part1.Models;

public class TelemetryInput
{
    public string DeviceId { get; set; } = "";
    public string Metric { get; set; } = "";
    public string DataType { get; set; } = "double";
    public string Value { get; set; } = "";
    public string Unit { get; set; } = "";
}
