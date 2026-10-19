using System.Text.RegularExpressions;
using SmartX_Part1.Models;

namespace SmartX_Part1.Services;

public static class SensorValidator
{
    public static List<string> Validate(SensorRegistration sensor)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(sensor.DeviceId))
            errors.Add("Device ID / MAC address is required.");

        if (sensor.DeviceId.Length < 3 || sensor.DeviceId.Length > 40)
            errors.Add("Device ID must be between 3 and 40 characters.");

        if (!Regex.IsMatch(sensor.DeviceId, @"^[A-Za-z0-9:_\-\.]+$"))
            errors.Add("Device ID contains invalid characters.");

        if (string.IsNullOrWhiteSpace(sensor.Location))
            errors.Add("Deployment location is required.");

        var allowedCategories = new[]
        {
            "Environmental",
            "Power Consumption",
            "Actuator"
        };

        if (!allowedCategories.Contains(sensor.Category))
            errors.Add("Choose a valid sensor category.");

        return errors;
    }
}
