using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartX_Part1.Services;

public class DisconnectAlert
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DeviceId { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}

public class DisconnectAlertService
{
    private readonly HashSet<string> alertedDevices =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly List<DisconnectAlert> alertHistory = new();

    private readonly object sync = new();

    public bool RecordDisconnection(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return false;
        }

        lock (sync)
        {
            if (!alertedDevices.Add(deviceId))
            {
                return false;
            }

            alertHistory.Add(new DisconnectAlert
            {
                DeviceId = deviceId,
                Message = $"Sensor {deviceId} has disconnected.",
                Timestamp = DateTime.UtcNow,
                IsActive = true
            });

            return true;
        }
    }

    public void RecordReconnection(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return;
        }

        lock (sync)
        {
            if (!alertedDevices.Remove(deviceId))
            {
                return;
            }

            foreach (var alert in alertHistory.Where(alert =>
                alert.DeviceId.Equals(
                    deviceId,
                    StringComparison.OrdinalIgnoreCase
                ) && alert.IsActive))
            {
                alert.IsActive = false;
            }
        }
    }

    public List<DisconnectAlert> GetActiveAlerts()
    {
        lock (sync)
        {
            return alertHistory
                .Where(alert => alert.IsActive)
                .OrderByDescending(alert => alert.Timestamp)
                .ToList();
        }
    }

    public List<DisconnectAlert> GetHistory()
    {
        lock (sync)
        {
            return alertHistory
                .OrderByDescending(alert => alert.Timestamp)
                .ToList();
        }
    }

    public object GetStatistics()
    {
        lock (sync)
        {
            return new
            {
                activeDisconnectAlerts = alertedDevices.Count,
                totalDisconnectEvents = alertHistory.Count,
                resolvedDisconnectAlerts = alertHistory.Count(alert => !alert.IsActive)
            };
        }
    }
}
