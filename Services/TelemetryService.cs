using SmartX_Part1.Models;

namespace SmartX_Part1.Services;

public class TelemetryService
{
    private readonly SmartXStore _store;

    public TelemetryService(SmartXStore store)
    {
        _store = store;
    }

    public TelemetryRecord ProcessInput(TelemetryInput input)
    {
        var type = input.DataType.Trim().ToLowerInvariant();

        return type switch
        {
            "double" or "float" => ProcessDouble(input),
            "int" or "integer" => ProcessInt(input),
            "bool" or "boolean" => ProcessBool(input),
            _ => throw new ArgumentException(
                "Data type must be double, int, or bool."
            )
        };
    }

    private TelemetryRecord ProcessDouble(TelemetryInput input)
    {
        if (!double.TryParse(input.Value, out var value))
            throw new ArgumentException("Value must be a valid decimal number.");

        var packet = new TelemetryPacket<double>
        {
            DeviceId = input.DeviceId,
            Metric = input.Metric,
            Value = value,
            Unit = input.Unit,
            Timestamp = DateTime.UtcNow
        };

        return SavePacket(packet, "double");
    }

    private TelemetryRecord ProcessInt(TelemetryInput input)
    {
        if (!int.TryParse(input.Value, out var value))
            throw new ArgumentException("Value must be a valid whole number.");

        var packet = new TelemetryPacket<int>
        {
            DeviceId = input.DeviceId,
            Metric = input.Metric,
            Value = value,
            Unit = input.Unit,
            Timestamp = DateTime.UtcNow
        };

        return SavePacket(packet, "int");
    }

    private TelemetryRecord ProcessBool(TelemetryInput input)
    {
        if (!bool.TryParse(input.Value, out var value))
            throw new ArgumentException("Boolean value must be true or false.");

        var packet = new TelemetryPacket<bool>
        {
            DeviceId = input.DeviceId,
            Metric = input.Metric,
            Value = value,
            Unit = input.Unit,
            Timestamp = DateTime.UtcNow
        };

        return SavePacket(packet, "bool");
    }

    private TelemetryRecord SavePacket<T>(TelemetryPacket<T> packet, string dataType)
    {
        var record = new TelemetryRecord
        {
            DeviceId = packet.DeviceId,
            Metric = packet.Metric,
            DataType = dataType,
            DisplayValue = packet.Value?.ToString() ?? "",
            Unit = packet.Unit,
            Timestamp = packet.Timestamp
        };

        _store.Telemetry.Add(record);

        return record;
    }

    // Required jagged array -> List<T> conversion.
    public List<double> ProcessHistoricalBatches(double[][] batches)
    {
        var optimisedCollection = new List<double>();

        foreach (var batch in batches)
        {
            optimisedCollection.AddRange(batch);
        }

        return optimisedCollection;
    }
}
