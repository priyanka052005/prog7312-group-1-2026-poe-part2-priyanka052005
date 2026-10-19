using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartX_Part1.Services;

public class TelemetryHistoryEntry
{
    public string DeviceId { get; set; } = "";
    public string Metric { get; set; } = "";
    public string DataType { get; set; } = "";
    public string Value { get; set; } = "";
    public string Unit { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class TelemetryHistoryService
{
    private readonly SortedDictionary<DateTime, List<TelemetryHistoryEntry>> history = new();

    private readonly Dictionary<string, List<TelemetryHistoryEntry>> deviceLookup =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly object sync = new();

    public void AddReading(
        string deviceId,
        string metric,
        string dataType,
        string value,
        string unit,
        DateTime timestamp)
    {
        lock (sync)
        {
            var entry = new TelemetryHistoryEntry
            {
                DeviceId = deviceId,
                Metric = metric,
                DataType = dataType,
                Value = value,
                Unit = unit,
                Timestamp = timestamp
            };

            if (!history.TryGetValue(timestamp, out var readings))
            {
                readings = new List<TelemetryHistoryEntry>();
                history[timestamp] = readings;
            }

            readings.Add(entry);

            if (!deviceLookup.TryGetValue(deviceId, out var deviceReadings))
            {
                deviceReadings = new List<TelemetryHistoryEntry>();
                deviceLookup[deviceId] = deviceReadings;
            }

            deviceReadings.Add(entry);
        }
    }

    public List<TelemetryHistoryEntry> GetChronologicalHistory()
    {
        lock (sync)
        {
            return history.Values
                .SelectMany(readings => readings)
                .ToList();
        }
    }

    public List<TelemetryHistoryEntry> GetNewestFirst()
    {
        lock (sync)
        {
            return history.Reverse()
                .SelectMany(group => group.Value)
                .ToList();
        }
    }

    public List<TelemetryHistoryEntry> GetDeviceHistory(string deviceId)
    {
        lock (sync)
        {
            if (!deviceLookup.TryGetValue(deviceId, out var readings))
            {
                return new List<TelemetryHistoryEntry>();
            }

            return readings
                .OrderBy(reading => reading.Timestamp)
                .ToList();
        }
    }

    public object GetStatistics()
    {
        lock (sync)
        {
            return new
            {
                totalReadings = history.Values.Sum(readings => readings.Count),
                uniqueTimestamps = history.Count,
                trackedDevices = deviceLookup.Count,
                earliestTimestamp = history.Count > 0
                    ? (DateTime?)history.First().Key
                    : null,
                latestTimestamp = history.Count > 0
                    ? (DateTime?)history.Last().Key
                    : null
            };
        }
    }
}
