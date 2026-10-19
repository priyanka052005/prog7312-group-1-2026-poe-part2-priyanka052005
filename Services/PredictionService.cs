
using SmartX_Part1.Models;
using System.Globalization;

namespace SmartX_Part1.Services;

public class DeviceSuggestion
{
    public string DeviceId { get; set; } = "";
    public string RecommendedAction { get; set; } = "";
    public string Reason { get; set; } = "";
    public string Confidence { get; set; } = "Low";
    public int Occurrences { get; set; }
}

public class PredictionService
{
    private readonly Dictionary<string, int> searchFrequency =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, Dictionary<string, int>> commandFrequency =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly object sync = new();

    public void RecordCommand(string deviceId, string action)
    {
        if (string.IsNullOrWhiteSpace(deviceId) ||
            string.IsNullOrWhiteSpace(action))
            return;

        lock (sync)
        {
            deviceId = deviceId.Trim();
            action = action.Trim().ToUpperInvariant();

            if (!commandFrequency.TryGetValue(deviceId, out var actions))
            {
                actions = new Dictionary<string, int>(
                    StringComparer.OrdinalIgnoreCase);

                commandFrequency[deviceId] = actions;
            }

            actions[action] = actions.GetValueOrDefault(action) + 1;
        }
    }

    public void RecordSearch(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            return;

        lock (sync)
        {
            deviceId = deviceId.Trim();

            searchFrequency[deviceId] =
                searchFrequency.GetValueOrDefault(deviceId) + 1;
        }
    }

    public List<DeviceSuggestion> GetSuggestions(
        IEnumerable<DeviceCommand> history,
        Dictionary<string, string> deviceStates,
        IEnumerable<TelemetryRecord>? telemetry = null)
    {
        lock (sync)
        {
            var suggestions = new List<DeviceSuggestion>();

            var executed = history
                .Where(c => c.Status.Equals(
                    "Executed",
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var deviceGroup in executed.GroupBy(
                c => c.DeviceId,
                StringComparer.OrdinalIgnoreCase))
            {
                var frequentAction = deviceGroup
                    .GroupBy(c => c.Action,
                        StringComparer.OrdinalIgnoreCase)
                    .Where(g => !g.Key.Equals(
                        "STATUS",
                        StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault();

                if (frequentAction == null ||
                    frequentAction.Count() < 3)
                    continue;

                var action = frequentAction.Key.ToUpperInvariant();

                if (action == "STOP" &&
                    deviceStates.TryGetValue(
                        deviceGroup.Key, out var state) &&
                    state.Equals(
                        "Stopped",
                        StringComparison.OrdinalIgnoreCase))
                    continue;

                AddOrReplace(suggestions, new DeviceSuggestion
                {
                    DeviceId = deviceGroup.Key,
                    RecommendedAction = action,
                    Reason = $"You have successfully used {action} " +
                             $"{frequentAction.Count()} times on this device.",
                    Confidence = frequentAction.Count() >= 5
                        ? "High" : "Medium",
                    Occurrences = frequentAction.Count()
                });
            }

            foreach (var state in deviceStates)
            {
                if (!state.Value.Equals(
                    "Stopped",
                    StringComparison.OrdinalIgnoreCase))
                    continue;

                AddOrReplace(suggestions, new DeviceSuggestion
                {
                    DeviceId = state.Key,
                    RecommendedAction = "START",
                    Reason = "This device is stopped. Check whether " +
                             "it should be restarted.",
                    Confidence = "Medium",
                    Occurrences = 1
                });
            }

            foreach (var search in searchFrequency)
            {
                if (search.Value < 3)
                    continue;

                AddOrReplace(suggestions, new DeviceSuggestion
                {
                    DeviceId = search.Key,
                    RecommendedAction = "STATUS",
                    Reason = $"You have searched for this device " +
                             $"{search.Value} times. Consider checking its status.",
                    Confidence = search.Value >= 5
                        ? "High" : "Medium",
                    Occurrences = search.Value
                });
            }

            if (telemetry != null)
                AddTelemetrySuggestions(
                    suggestions,
                    telemetry.ToList());

            return suggestions
                .OrderByDescending(s => Rank(s.Confidence))
                .ThenByDescending(s => s.Occurrences)
                .ThenBy(s => s.DeviceId)
                .ToList();
        }
    }

    private static void AddTelemetrySuggestions(
        List<DeviceSuggestion> suggestions,
        List<TelemetryRecord> telemetry)
    {
        var groups = telemetry.GroupBy(r => (
            DeviceId: r.DeviceId.ToUpperInvariant(),
            Metric: r.Metric.ToUpperInvariant()));

        foreach (var group in groups)
        {
            var readings = group
                .OrderByDescending(r => r.Timestamp)
                .Take(5)
                .ToList();

            if (readings.Count == 0)
                continue;

            var latest = readings[0];

            if (!TryNumber(latest.DisplayValue, out var current))
                continue;

            var values = readings
                .Select(r => TryNumber(r.DisplayValue, out var value)
                    ? (double?)value : null)
                .ToList();

            if (values.Any(v => !v.HasValue))
                continue;

            var numbers = values.Select(v => v!.Value).ToList();

            bool decreasing = numbers.Count >= 3 &&
                numbers.Zip(numbers.Skip(1),
                    (newer, older) => newer < older).All(x => x);

            bool increasing = numbers.Count >= 3 &&
                numbers.Zip(numbers.Skip(1),
                    (newer, older) => newer > older).All(x => x);

            var metric = latest.Metric.ToLowerInvariant();

            if (metric.Contains("moisture"))
            {
                if (current < 20)
                {
                    AddOrReplace(suggestions, new DeviceSuggestion
                    {
                        DeviceId = latest.DeviceId,
                        RecommendedAction = "INSPECT",
                        Reason = $"Moisture is critically low at " +
                                 $"{current}{latest.Unit}. " +
                                 "Inspect irrigation and check related pumps.",
                        Confidence = "High",
                        Occurrences = numbers.Count
                    });

                    if (latest.DeviceId.Equals(
                        "ESP32-A1",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        AddOrReplace(suggestions, new DeviceSuggestion
                        {
                            DeviceId = "ESP32-C3",
                            RecommendedAction = "START",
                            Reason = "Cross-device prediction: ESP32-A1 " +
                                     $"detected moisture at {current}{latest.Unit}. " +
                                     "Check whether the simulated irrigation " +
                                     "actuator ESP32-C3 should be activated.",
                            Confidence = "High",
                            Occurrences = numbers.Count
                        });
                    }
                }
                else if (decreasing)
                {
                    AddOrReplace(suggestions, new DeviceSuggestion
                    {
                        DeviceId = latest.DeviceId,
                        RecommendedAction = "MONITOR",
                        Reason = "Moisture readings are consistently falling. " +
                                 "Check irrigation before moisture becomes critical.",
                        Confidence = "Medium",
                        Occurrences = numbers.Count
                    });
                }
            }
            else if (metric.Contains("temperature"))
            {
                if (current > 30)
                {
                    AddOrReplace(suggestions, new DeviceSuggestion
                    {
                        DeviceId = latest.DeviceId,
                        RecommendedAction = "INSPECT",
                        Reason = $"Temperature is high at " +
                                 $"{current}{latest.Unit}. " +
                                 "Check cooling and ventilation.",
                        Confidence = "High",
                        Occurrences = numbers.Count
                    });
                }
                else if (increasing)
                {
                    AddOrReplace(suggestions, new DeviceSuggestion
                    {
                        DeviceId = latest.DeviceId,
                        RecommendedAction = "MONITOR",
                        Reason = "Temperature readings are consistently rising. " +
                                 "Monitor the environment for overheating.",
                        Confidence = "Medium",
                        Occurrences = numbers.Count
                    });
                }
            }
            else if (metric.Contains("power"))
            {
                if (current > 1200)
                {
                    AddOrReplace(suggestions, new DeviceSuggestion
                    {
                        DeviceId = latest.DeviceId,
                        RecommendedAction = "INSPECT",
                        Reason = $"Power consumption reached " +
                                 $"{current}{latest.Unit}. " +
                                 "Investigate a possible power spike.",
                        Confidence = "High",
                        Occurrences = numbers.Count
                    });
                }
                else if (increasing)
                {
                    AddOrReplace(suggestions, new DeviceSuggestion
                    {
                        DeviceId = latest.DeviceId,
                        RecommendedAction = "MONITOR",
                        Reason = "Power consumption is consistently increasing. " +
                                 "Review device activity.",
                        Confidence = "Medium",
                        Occurrences = numbers.Count
                    });
                }
            }
        }
    }

    private static bool TryNumber(string text, out double value)
    {
        return double.TryParse(
            text,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out value);
    }

    private static int Rank(string confidence)
    {
        return confidence.Equals("High",
            StringComparison.OrdinalIgnoreCase) ? 3 :
            confidence.Equals("Medium",
            StringComparison.OrdinalIgnoreCase) ? 2 : 1;
    }

    private static void AddOrReplace(
        List<DeviceSuggestion> suggestions,
        DeviceSuggestion incoming)
    {
        var existing = suggestions.FirstOrDefault(s =>
            s.DeviceId.Equals(
                incoming.DeviceId,
                StringComparison.OrdinalIgnoreCase));

        if (existing == null)
        {
            suggestions.Add(incoming);
            return;
        }

        if (Rank(incoming.Confidence) > Rank(existing.Confidence) ||
            (Rank(incoming.Confidence) == Rank(existing.Confidence) &&
             incoming.Occurrences >= existing.Occurrences))
        {
            suggestions.Remove(existing);
            suggestions.Add(incoming);
        }
    }

    public object GetLearningStatistics(
        IEnumerable<DeviceCommand> history)
    {
        lock (sync)
        {
            var executed = history
                .Where(c => c.Status.Equals(
                    "Executed",
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            return new
            {
                trackedDevices = executed
                    .Select(c => c.DeviceId)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count(),
                trackedSearches = searchFrequency.Values.Sum(),
                trackedCommands = executed.Count,
                searchedDevices = searchFrequency.Count
            };
        }
    }
}
