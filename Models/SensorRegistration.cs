namespace SmartX_Part1.Models;

public class SensorRegistration
{
    public string DeviceId { get; set; } = "";
    public string Location { get; set; } = "";
    public string Category { get; set; } = "";
    public string Status { get; set; } = "Online";
    public DateTime CreatedAt { get; set; }
}
