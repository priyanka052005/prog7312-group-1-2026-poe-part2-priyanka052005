
using SmartX_Part1.Models;

namespace SmartX_Part1.Services;

public class SmartXStore
{
    private readonly Dictionary<string, SensorRegistration> deviceRegistry =
        new(StringComparer.OrdinalIgnoreCase);

    public List<SensorRegistration> Sensors { get; } = new();
    public List<TelemetryRecord> Telemetry { get; } = new();
    public List<SensorAttachment> Attachments { get; } = new();

    public IReadOnlyDictionary<string, SensorRegistration> DeviceRegistry =>
        deviceRegistry;

    public SmartXStore()
    {
        RegisterSensor(new SensorRegistration
        {
            DeviceId = "ESP32-A1",
            Location = "Greenhouse / Zone A",
            Category = "Environmental",
            Status = "Online",
            CreatedAt = DateTime.UtcNow.AddMinutes(-25)
        });

        RegisterSensor(new SensorRegistration
        {
            DeviceId = "ESP32-B2",
            Location = "Utility Room",
            Category = "Power Consumption",
            Status = "Online",
            CreatedAt = DateTime.UtcNow.AddMinutes(-20)
        });

        RegisterSensor(new SensorRegistration
        {
            DeviceId = "ESP32-C3",
            Location = "Hydroponics / Zone B",
            Category = "Actuator",
            Status = "Online",
            CreatedAt = DateTime.UtcNow.AddMinutes(-15)
        });

        Telemetry.AddRange(
        [
            new TelemetryRecord
            {
                DeviceId = "ESP32-A1",
                Metric = "Temperature",
                DataType = "double",
                DisplayValue = "23.6",
                Unit = "°C",
                Timestamp = DateTime.UtcNow.AddMinutes(-5)
            },
            new TelemetryRecord
            {
                DeviceId = "ESP32-B2",
                Metric = "Power",
                DataType = "int",
                DisplayValue = "850",
                Unit = "W",
                Timestamp = DateTime.UtcNow.AddMinutes(-4)
            },
            new TelemetryRecord
            {
                DeviceId = "ESP32-C3",
                Metric = "ValveOpen",
                DataType = "bool",
                DisplayValue = "false",
                Unit = "",
                Timestamp = DateTime.UtcNow.AddMinutes(-3)
            }
        ]);
    }

    public bool RegisterSensor(SensorRegistration sensor)
    {
        if (sensor == null ||
            string.IsNullOrWhiteSpace(sensor.DeviceId))
        {
            return false;
        }

        sensor.DeviceId = sensor.DeviceId.Trim();

        if (!deviceRegistry.TryAdd(sensor.DeviceId, sensor))
        {
            return false;
        }

        Sensors.Add(sensor);
        return true;
    }

    public SensorRegistration? FindSensor(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return null;
        }

        deviceRegistry.TryGetValue(deviceId.Trim(), out var sensor);
        return sensor;
    }

    public bool IsRegistered(string deviceId)
    {
        return FindSensor(deviceId) != null;
    }

    public int RegisteredDeviceCount =>
        deviceRegistry.Count;
}
