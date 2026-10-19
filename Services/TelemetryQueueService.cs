using System.Globalization;
using SmartX_Part1.Models;

namespace SmartX_Part1.Services;

public class TelemetryQueueService
{
    private readonly Queue<TelemetryInput> normalQueue = new();

    private readonly PriorityQueue<TelemetryInput, int> criticalQueue = new();

    private readonly List<TelemetryQueueResult> processingHistory = new();

    private readonly object sync = new();

    public TelemetryQueueResult Enqueue(TelemetryInput input)
    {
        if (input == null ||
            string.IsNullOrWhiteSpace(input.DeviceId) ||
            string.IsNullOrWhiteSpace(input.Metric) ||
            string.IsNullOrWhiteSpace(input.DataType) ||
            string.IsNullOrWhiteSpace(input.Value))
        {
            throw new ArgumentException("Complete telemetry information is required.");
        }

        var isCritical = IsCritical(input);

        lock (sync)
        {
            if (isCritical)
            {
                criticalQueue.Enqueue(input, 0);
            }
            else
            {
                normalQueue.Enqueue(input);
            }

            return new TelemetryQueueResult
            {
                DeviceId = input.DeviceId,
                Metric = input.Metric,
                Value = input.Value,
                Priority = isCritical ? "Critical" : "Normal",
                Status = "Queued",
                Timestamp = DateTime.UtcNow
            };
        }
    }

    public TelemetryQueueResult? ProcessNext(
        TelemetryService telemetryService,
        TelemetryHistoryService historyService)
    {
        lock (sync)
        {
            TelemetryInput input;
            string priority;

            if (criticalQueue.Count > 0)
            {
                input = criticalQueue.Dequeue();
                priority = "Critical";
            }
            else if (normalQueue.Count > 0)
            {
                input = normalQueue.Dequeue();
                priority = "Normal";
            }
            else
            {
                return null;
            }

            try
            {
                var reading = telemetryService.ProcessInput(input);

                historyService.AddReading(
                    reading.DeviceId,
                    reading.Metric,
                    reading.DataType,
                    reading.DisplayValue,
                    reading.Unit,
                    reading.Timestamp
                );

                var result = new TelemetryQueueResult
                {
                    DeviceId = reading.DeviceId,
                    Metric = reading.Metric,
                    Value = reading.DisplayValue,
                    Priority = priority,
                    Status = "Processed",
                    Timestamp = DateTime.UtcNow,
                    Reading = reading
                };

                processingHistory.Add(result);

                return result;
            }
            catch (Exception exception)
            {
                var result = new TelemetryQueueResult
                {
                    DeviceId = input.DeviceId,
                    Metric = input.Metric,
                    Value = input.Value,
                    Priority = priority,
                    Status = "Failed",
                    Error = exception.Message,
                    Timestamp = DateTime.UtcNow
                };

                processingHistory.Add(result);

                return result;
            }
        }
    }

    public object GetStatistics()
    {
        lock (sync)
        {
            return new
            {
                normalQueueCount = normalQueue.Count,
                criticalQueueCount = criticalQueue.Count,
                totalPending = normalQueue.Count + criticalQueue.Count,
                totalProcessed = processingHistory.Count(
                    item => item.Status == "Processed"
                ),
                totalFailed = processingHistory.Count(
                    item => item.Status == "Failed"
                ),
                criticalProcessed = processingHistory.Count(
                    item => item.Status == "Processed" &&
                            item.Priority == "Critical"
                )
            };
        }
    }

    public List<TelemetryQueueResult> GetHistory()
    {
        lock (sync)
        {
            return processingHistory.ToList();
        }
    }

    private static bool IsCritical(TelemetryInput input)
    {
        if (!double.TryParse(
            input.Value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var value))
        {
            return false;
        }

        var metric = input.Metric.Trim().ToLowerInvariant();

        if (metric.Contains("temperature") && value > 30)
        {
            return true;
        }

        if (metric.Contains("moisture") && value < 20)
        {
            return true;
        }

        if (metric.Contains("power") && value > 1200)
        {
            return true;
        }

        return false;
    }
}

public class TelemetryQueueResult
{
    public string DeviceId { get; set; } = "";
    public string Metric { get; set; } = "";
    public string Value { get; set; } = "";
    public string Priority { get; set; } = "Normal";
    public string Status { get; set; } = "";
    public string? Error { get; set; }
    public DateTime Timestamp { get; set; }
    public TelemetryRecord? Reading { get; set; }
}
