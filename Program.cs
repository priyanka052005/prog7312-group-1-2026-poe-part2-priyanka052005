using SmartX_Part1.Models;
using SmartX_Part1.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<SmartXStore>();
builder.Services.AddSingleton<TelemetryService>();
builder.Services.AddSingleton<TelemetryQueueService>();
builder.Services.AddSingleton<CommandService>();
builder.Services.AddSingleton<PredictionService>();
builder.Services.AddSingleton<DisconnectAlertService>();
builder.Services.AddSingleton<TelemetryHistoryService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

Directory.CreateDirectory(
    Path.Combine(app.Environment.ContentRootPath, "Uploads")
);

app.MapGet("/api/health", () =>
{
    return Results.Ok(new
    {
        status = "Online",
        time = DateTime.UtcNow
    });
});

app.MapGet("/api/sensors", (SmartXStore store) =>
{
    return Results.Ok(store.Sensors);
});

app.MapPost("/api/sensors", (
    SensorRegistration sensor,
    SmartXStore store) =>
{
    var errors = SensorValidator.Validate(sensor);

    if (errors.Count > 0)
    {
        return Results.BadRequest(new
        {
            message = "Sensor validation failed.",
            errors
        });
    }

    if (store.IsRegistered(sensor.DeviceId))
    {
        return Results.BadRequest(new
        {
            message = "A sensor with this device ID already exists."
        });
    }

    sensor.CreatedAt = DateTime.UtcNow;
    sensor.Status = "Online";

    if (!store.RegisterSensor(sensor))
    {
        return Results.BadRequest(new
        {
            message = "A sensor with this device ID already exists."
        });
    }

    return Results.Created(
        $"/api/sensors/{Uri.EscapeDataString(sensor.DeviceId)}",
        sensor
    );
});

app.MapGet("/api/telemetry", (SmartXStore store) =>
{
    return Results.Ok(
        store.Telemetry
            .OrderByDescending(reading => reading.Timestamp)
            .ToList()
    );
});

app.MapPost("/api/telemetry", (
    TelemetryInput input,
    SmartXStore store,
    TelemetryService service,
    TelemetryHistoryService history) =>
{
    if (string.IsNullOrWhiteSpace(input.DeviceId) ||
        !store.IsRegistered(input.DeviceId))
    {
        return Results.BadRequest(new
        {
            message = "Register this sensor before submitting telemetry."
        });
    }

    try
    {
        var reading = service.ProcessInput(input);

        history.AddReading(
            reading.DeviceId,
            reading.Metric,
            reading.DataType,
            reading.DisplayValue,
            reading.Unit,
            reading.Timestamp
        );

        return Results.Ok(reading);
    }
    catch (Exception exception)
    {
        return Results.BadRequest(new
        {
            message = exception.Message
        });
    }
});

app.MapPost("/api/telemetry/queue", (
    TelemetryInput input,
    SmartXStore store,
    TelemetryQueueService queue) =>
{
    if (string.IsNullOrWhiteSpace(input.DeviceId) ||
        !store.IsRegistered(input.DeviceId))
    {
        return Results.BadRequest(new
        {
            message = "Register this sensor before submitting telemetry."
        });
    }

    try
    {
        var queued = queue.Enqueue(input);
        return Results.Accepted("/api/telemetry/queue/stats", queued);
    }
    catch (ArgumentException exception)
    {
        return Results.BadRequest(new { message = exception.Message });
    }
});

app.MapPost("/api/telemetry/queue/process-next", (
    TelemetryQueueService queue,
    TelemetryService telemetry,
    TelemetryHistoryService history) =>
{
    var result = queue.ProcessNext(telemetry, history);

    if (result is null)
    {
        return Results.BadRequest(new
        {
            message = "There are no pending telemetry packets."
        });
    }

    return result.Status == "Failed"
        ? Results.BadRequest(result)
        : Results.Ok(result);
});

app.MapGet("/api/telemetry/queue/stats", (
    TelemetryQueueService queue) =>
{
    return Results.Ok(queue.GetStatistics());
});

app.MapGet("/api/telemetry/queue/history", (
    TelemetryQueueService queue) =>
{
    return Results.Ok(queue.GetHistory());
});

app.MapPost("/api/telemetry/queue/demo", (
    SmartXStore store,
    TelemetryQueueService queue) =>
{
    var samples = new[]
    {
        new TelemetryInput
        {
            DeviceId = "ESP32-A1",
            Metric = "Temperature",
            DataType = "double",
            Value = "24.5",
            Unit = "°C"
        },
        new TelemetryInput
        {
            DeviceId = "ESP32-B2",
            Metric = "Power",
            DataType = "int",
            Value = "1500",
            Unit = "W"
        },
        new TelemetryInput
        {
            DeviceId = "ESP32-A1",
            Metric = "Moisture",
            DataType = "double",
            Value = "15",
            Unit = "%"
        }
    };

    if (samples.Any(sample => !store.IsRegistered(sample.DeviceId)))
    {
        return Results.BadRequest(new
        {
            message = "The demo devices must be registered first."
        });
    }

    var queued = samples.Select(queue.Enqueue).ToList();
    return Results.Ok(new
    {
        message = "Demo packets queued. Process the critical packets first.",
        queued,
        statistics = queue.GetStatistics()
    });
});

app.MapGet("/api/telemetry/history", (
    TelemetryHistoryService history) =>
{
    return Results.Ok(history.GetChronologicalHistory());
});

app.MapGet("/api/telemetry/history/newest", (
    TelemetryHistoryService history) =>
{
    return Results.Ok(history.GetNewestFirst());
});

app.MapGet("/api/telemetry/history/stats", (
    TelemetryHistoryService history) =>
{
    return Results.Ok(history.GetStatistics());
});

app.MapGet("/api/telemetry/history/device/{deviceId}", (
    string deviceId,
    TelemetryHistoryService history) =>
{
    return Results.Ok(history.GetDeviceHistory(deviceId));
});

app.MapPost("/api/telemetry/demo-batch", (
    TelemetryService service) =>
{
    return Results.Ok(
        service.ProcessHistoricalBatches(
            new double[][]
            {
                new double[] { 22.5, 23.1, 24.0 },
                new double[] { 21.9, 22.7 },
                new double[] { 23.4, 24.2, 25.1, 26.0 }
            }
        )
    );
});

app.MapGet("/api/demo/operator-overload", () =>
{
    var first = new PowerReading("Meter A", 10);
    var second = new PowerReading("Meter B", 15);
    var combined = first + second;

    return Results.Ok(new
    {
        first,
        second,
        combined
    });
});

app.MapGet("/api/demo/recursive-validation", () =>
{
    var root = DeploymentNode.CreateDemoHierarchy();

    static bool ValidateNode(DeploymentNode node)
    {
        if (!node.IsConfigured || string.IsNullOrWhiteSpace(node.Name) ||
            string.IsNullOrWhiteSpace(node.NodeType))
        {
            return false;
        }

        return node.Child is null || ValidateNode(node.Child);
    }

    return Results.Ok(new
    {
        root,
        isValid = ValidateNode(root)
    });
});

app.MapPost("/api/sensors/{deviceId}/upload", async (
    string deviceId,
    HttpRequest request,
    SmartXStore store,
    IWebHostEnvironment environment) =>
{
    var sensor = store.FindSensor(deviceId);

    if (sensor is null)
    {
        return Results.NotFound(new
        {
            message = "Sensor not found."
        });
    }

    if (!request.HasFormContentType)
    {
        return Results.BadRequest(new
        {
            message = "A multipart form is required."
        });
    }

    var form = await request.ReadFormAsync();
    var file = form.Files.GetFile("file");

    if (file is null || file.Length == 0)
    {
        return Results.BadRequest(new
        {
            message = "Select a valid file."
        });
    }

    if (file.Length > 5 * 1024 * 1024)
    {
        return Results.BadRequest(new
        {
            message = "The maximum file size is 5 MB."
        });
    }

    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

    var allowedExtensions = new HashSet<string>
    {
        ".txt", ".log", ".json", ".jpg", ".jpeg", ".png", ".pdf"
    };

    if (!allowedExtensions.Contains(extension))
    {
        return Results.BadRequest(new
        {
            message = "Unsupported file type."
        });
    }

    var uploadDirectory = Path.Combine(
        environment.ContentRootPath,
        "Uploads",
        sensor.DeviceId
    );

    Directory.CreateDirectory(uploadDirectory);

    var storedFileName = $"{Guid.NewGuid():N}{extension}";
    var fullPath = Path.Combine(uploadDirectory, storedFileName);

    await using (var stream = File.Create(fullPath))
    {
        await file.CopyToAsync(stream);
    }

    return Results.Ok(new
    {
        message = "File uploaded successfully.",
        deviceId = sensor.DeviceId,
        originalFileName = file.FileName,
        storedFileName,
        size = file.Length
    });
});

app.MapGet("/api/alerts", (
    SmartXStore store,
    DisconnectAlertService disconnectAlerts) =>
{
    var anomalyAlerts = AlertEngine.BuildAlerts(store);

    var disconnectMessages = disconnectAlerts
        .GetActiveAlerts()
        .Select(alert => new
        {
            message = alert.Message,
            deviceId = alert.DeviceId,
            timestamp = alert.Timestamp,
            type = "Disconnection"
        });

    var combined = anomalyAlerts
        .Select(alert => (object)alert)
        .Concat(disconnectMessages.Select(alert => (object)alert))
        .ToList();

    return Results.Ok(combined);
});

app.MapGet("/api/commands", (
    CommandService commands,
    string? search) =>
{
    return Results.Ok(commands.GetHistory(search));
});

app.MapPost("/api/commands", (
    CommandRequest request,
    CommandService commands,
    SmartXStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.DeviceId))
    {
        return Results.BadRequest(new
        {
            message = "Select a device."
        });
    }

    if (!store.IsRegistered(request.DeviceId))
    {
        return Results.BadRequest(new
        {
            message = "The selected device is not registered."
        });
    }

    var validActions = new HashSet<string>(
        StringComparer.OrdinalIgnoreCase)
    {
        "START", "STOP", "RESTART", "STATUS"
    };

    if (string.IsNullOrWhiteSpace(request.Action) ||
        !validActions.Contains(request.Action))
    {
        return Results.BadRequest(new
        {
            message = "Invalid command action."
        });
    }

    if (!string.Equals(
            request.Priority,
            "Normal",
            StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(
            request.Priority,
            "Critical",
            StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new
        {
            message = "Priority must be Normal or Critical."
        });
    }

    var command = commands.Submit(request);

    return Results.Ok(new
    {
        message = "Command queued successfully.",
        command
    });
});

app.MapPost("/api/commands/process-next", (
    CommandService commands,
    DisconnectAlertService disconnectAlerts,
    SmartXStore store) =>
{
    var command = commands.ProcessNext();

    if (command is null)
    {
        return Results.BadRequest(new
        {
            message = "There are no pending commands."
        });
    }

    var state = commands.GetDeviceState(command.DeviceId);

    var sensor = store.FindSensor(command.DeviceId);

    if (sensor is not null)
    {
        sensor.Status = state;
    }

    if (state == "Stopped")
    {
        disconnectAlerts.RecordDisconnection(command.DeviceId);
    }
    else
    {
        disconnectAlerts.RecordReconnection(command.DeviceId);
    }

    return Results.Ok(new
    {
        message = "Command executed successfully.",
        command
    });
});

app.MapPost("/api/commands/undo", (
    CommandService commands,
    DisconnectAlertService disconnectAlerts,
    SmartXStore store) =>
{
    var command = commands.UndoLast();

    if (command is null)
    {
        return Results.BadRequest(new
        {
            message = "There are no commands available to undo."
        });
    }

    var state = commands.GetDeviceState(command.DeviceId);

    var sensor = store.FindSensor(command.DeviceId);

    if (sensor is not null)
    {
        sensor.Status = state;
    }

    if (state == "Stopped")
    {
        disconnectAlerts.RecordDisconnection(command.DeviceId);
    }
    else
    {
        disconnectAlerts.RecordReconnection(command.DeviceId);
    }

    return Results.Ok(new
    {
        message = "Command undone successfully.",
        command
    });
});

app.MapGet("/api/commands/stats", (
    CommandService commands) =>
{
    return Results.Ok(commands.GetStatistics());
});

app.MapGet("/api/commands/disconnected", (
    CommandService commands) =>
{
    return Results.Ok(commands.GetDisconnectedDevices());
});

app.MapGet("/api/commands/device-states", (
    CommandService commands) =>
{
    return Results.Ok(commands.GetDeviceStates());
});

app.MapGet("/api/commands/{id:guid}", (
    Guid id,
    CommandService commands) =>
{
    var command = commands.FindCommand(id);

    return command is null
        ? Results.NotFound(new
        {
            message = "Command not found."
        })
        : Results.Ok(command);
});
app.MapPost("/api/disconnect-alerts/test/{deviceId}", (
    string deviceId,
    DisconnectAlertService disconnectAlerts) =>
{
    bool created = disconnectAlerts.RecordDisconnection(deviceId);

    return Results.Ok(new
    {
        deviceId,
        alertCreated = created,
        message = created
            ? "Disconnect alert created."
            : "Duplicate disconnect alert prevented."
    });
});

app.MapPost("/api/disconnect-alerts/reconnect/{deviceId}", (
    string deviceId,
    DisconnectAlertService disconnectAlerts) =>
{
    disconnectAlerts.RecordReconnection(deviceId);

    return Results.Ok(new
    {
        deviceId,
        message = "Device reconnected and active alert cleared."
    });
});

app.MapGet("/api/disconnect-alerts", (
    DisconnectAlertService disconnectAlerts) =>
{
    return Results.Ok(disconnectAlerts.GetActiveAlerts());
});

app.MapGet("/api/disconnect-alerts/history", (
    DisconnectAlertService disconnectAlerts) =>
{
    return Results.Ok(disconnectAlerts.GetHistory());
});

app.MapGet("/api/disconnect-alerts/stats", (
    DisconnectAlertService disconnectAlerts) =>
{
    return Results.Ok(disconnectAlerts.GetStatistics());
});

app.MapGet("/api/predictions", (
    PredictionService predictions,
    CommandService commands,
    SmartXStore store) =>
{
    return Results.Ok(
        predictions.GetSuggestions(
            commands.GetHistory(),
            commands.GetDeviceStates(),
            store.Telemetry
        )
    );
});

app.MapGet("/api/predictions/stats", (
    PredictionService predictions,
    CommandService commands) =>
{
    return Results.Ok(
        predictions.GetLearningStatistics(
            commands.GetHistory()
        )
    );
});

app.MapPost("/api/predictions/search", (
    SearchActivity activity,
    PredictionService predictions,
    SmartXStore store) =>
{
    if (string.IsNullOrWhiteSpace(activity.DeviceId))
    {
        return Results.BadRequest(new
        {
            message = "A device ID is required."
        });
    }

    if (!store.IsRegistered(activity.DeviceId))
    {
        return Results.BadRequest(new
        {
            message = "Device not found."
        });
    }

    predictions.RecordSearch(activity.DeviceId);

    return Results.Ok(new
    {
        message = "Search activity recorded."
    });
});

app.MapFallbackToFile("index.html");

app.Run();

public record SearchActivity(string DeviceId);
