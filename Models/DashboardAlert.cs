namespace SmartX_Part1.Models;

public class DashboardAlert
{
    public string Severity { get; set; } = "Info";
    public string DeviceId { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
