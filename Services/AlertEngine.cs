using SmartX_Part1.Models;

namespace SmartX_Part1.Services;

public static class AlertEngine
{
    public static List<DashboardAlert> BuildAlerts(SmartXStore store)
    {
        var alerts = new List<DashboardAlert>();

        foreach (var sensor in store.Sensors)
        {
            var latest = store.Telemetry
                .Where(t => t.DeviceId.Equals(sensor.DeviceId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(t => t.Timestamp)
                .FirstOrDefault();

            if (latest is null)
            {
                alerts.Add(new DashboardAlert
                {
                    Severity = "Warning",
                    DeviceId = sensor.DeviceId,
                    Message = "No telemetry has been received from this sensor."
                });
                continue;
            }

            var age = DateTime.UtcNow - latest.Timestamp;

            if (age.TotalMinutes > 10)
            {
                alerts.Add(new DashboardAlert
                {
                    Severity = "Critical",
                    DeviceId = sensor.DeviceId,
                    Message = "Possible disconnect: no recent telemetry."
                });
            }

            if (latest.Metric.Contains("temperature", StringComparison.OrdinalIgnoreCase)
                && double.TryParse(latest.DisplayValue, out var temperature)
                && temperature > 30)
            {
                alerts.Add(new DashboardAlert
                {
                    Severity = "Critical",
                    DeviceId = sensor.DeviceId,
                    Message = $"High temperature detected: {temperature}{latest.Unit}"
                });
            }

            if (latest.Metric.Contains("moisture", StringComparison.OrdinalIgnoreCase)
                && double.TryParse(latest.DisplayValue, out var moisture)
                && moisture < 20)
            {
                alerts.Add(new DashboardAlert
                {
                    Severity = "Critical",
                    DeviceId = sensor.DeviceId,
                    Message = $"Low moisture detected: {moisture}{latest.Unit}"
                });
            }

            if (latest.Metric.Contains("power", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(latest.DisplayValue, out var power)
                && power > 1200)
            {
                alerts.Add(new DashboardAlert
                {
                    Severity = "Warning",
                    DeviceId = sensor.DeviceId,
                    Message = $"Power spike detected: {power}{latest.Unit}"
                });
            }
        }

        if (alerts.Count == 0)
        {
            alerts.Add(new DashboardAlert
            {
                Severity = "Info",
                DeviceId = "SYSTEM",
                Message = "All monitored sensors are currently within normal ranges."
            });
        }

        return alerts;
    }
}
