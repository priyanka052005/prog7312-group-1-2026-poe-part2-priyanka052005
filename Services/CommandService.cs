
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartX_Part1.Services;

public class DeviceCommand
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DeviceId { get; set; } = "";
    public string Action { get; set; } = "";
    public string Priority { get; set; } = "Normal";
    public string Status { get; set; } = "Pending";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public DateTime? ExecutedAt { get; set; }
    public string? PreviousDeviceState { get; set; }
    public string? ResultingDeviceState { get; set; }
}

public class CommandRequest
{
    public string DeviceId { get; set; } = "";
    public string Action { get; set; } = "";
    public string Priority { get; set; } = "Normal";
}

public class CommandService
{
    private readonly Queue<DeviceCommand> normalQueue = new();

    private readonly PriorityQueue<DeviceCommand, int> priorityQueue = new();

    private readonly Stack<DeviceCommand> undoStack = new();

    private readonly Dictionary<Guid, DeviceCommand> commandLookup = new();

    private readonly Dictionary<string, string> deviceStates =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, Guid> lastStateChange =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly SortedDictionary<DateTime, List<DeviceCommand>> commandTimeline = new();

    private readonly HashSet<string> disconnectedDevices =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly List<DeviceCommand> commandHistory = new();

    private readonly object sync = new();

    public DeviceCommand Submit(CommandRequest request)
    {
        lock (sync)
        {
            var command = new DeviceCommand
            {
                DeviceId = request.DeviceId.Trim(),
                Action = request.Action.Trim().ToUpperInvariant(),
                Priority = string.Equals(
                    request.Priority,
                    "Critical",
                    StringComparison.OrdinalIgnoreCase
                ) ? "Critical" : "Normal"
            };

            if (!deviceStates.ContainsKey(command.DeviceId))
            {
                deviceStates[command.DeviceId] = "Online";
            }

            if (command.Priority == "Critical")
            {
                priorityQueue.Enqueue(command, 0);
            }
            else
            {
                normalQueue.Enqueue(command);
            }

            commandLookup[command.Id] = command;
            commandHistory.Add(command);

            if (!commandTimeline.TryGetValue(command.Timestamp, out var commands))
            {
                commands = new List<DeviceCommand>();
                commandTimeline[command.Timestamp] = commands;
            }

            commands.Add(command);

            return command;
        }
    }

    public DeviceCommand? ProcessNext()
    {
        lock (sync)
        {
            DeviceCommand? command = null;

            if (priorityQueue.Count > 0)
            {
                command = priorityQueue.Dequeue();
            }
            else if (normalQueue.Count > 0)
            {
                command = normalQueue.Dequeue();
            }

            if (command is null)
            {
                return null;
            }

            var previousState = deviceStates.GetValueOrDefault(
                command.DeviceId,
                "Online"
            );

            var resultingState = command.Action switch
            {
                "START" => "Online",
                "STOP" => "Stopped",
                "RESTART" => "Online",
                _ => previousState
            };

            command.PreviousDeviceState = previousState;
            command.ResultingDeviceState = resultingState;
            command.Status = "Executed";
            command.ExecutedAt = DateTime.UtcNow;

            if (command.Action != "STATUS")
            {
                deviceStates[command.DeviceId] = resultingState;
                lastStateChange[command.DeviceId] = command.Id;

                if (resultingState == "Stopped")
                {
                    disconnectedDevices.Add(command.DeviceId);
                }
                else
                {
                    disconnectedDevices.Remove(command.DeviceId);
                }

                undoStack.Push(command);
            }

            return command;
        }
    }

    public DeviceCommand? UndoLast()
    {
        lock (sync)
        {
            if (undoStack.Count == 0)
            {
                return null;
            }

            var command = undoStack.Peek();

            if (command.Status != "Executed")
            {
                return null;
            }

            if (!lastStateChange.TryGetValue(
                command.DeviceId,
                out var latestCommandId
            ) || latestCommandId != command.Id)
            {
                return null;
            }

            undoStack.Pop();

            var previousState = command.PreviousDeviceState ?? "Online";

            deviceStates[command.DeviceId] = previousState;

            if (previousState == "Stopped")
            {
                disconnectedDevices.Add(command.DeviceId);
            }
            else
            {
                disconnectedDevices.Remove(command.DeviceId);
            }

            command.Status = "Undone";
            command.ResultingDeviceState = previousState;

            var previousExecutedCommand = undoStack
                .FirstOrDefault(item =>
                    item.DeviceId.Equals(
                        command.DeviceId,
                        StringComparison.OrdinalIgnoreCase
                    ) &&
                    item.Status == "Executed"
                );

            if (previousExecutedCommand is null)
            {
                lastStateChange.Remove(command.DeviceId);
            }
            else
            {
                lastStateChange[command.DeviceId] = previousExecutedCommand.Id;
            }

            return command;
        }
    }

    public List<DeviceCommand> GetHistory(string? search = null)
    {
        lock (sync)
        {
            IEnumerable<DeviceCommand> result = commandHistory;

            if (!string.IsNullOrWhiteSpace(search))
            {
                result = result.Where(command =>
                    command.DeviceId.Contains(
                        search,
                        StringComparison.OrdinalIgnoreCase
                    ) ||
                    command.Action.Contains(
                        search,
                        StringComparison.OrdinalIgnoreCase
                    ) ||
                    command.Status.Contains(
                        search,
                        StringComparison.OrdinalIgnoreCase
                    )
                );
            }

            return result
                .OrderByDescending(command => command.Timestamp)
                .ToList();
        }
    }

    public DeviceCommand? FindCommand(Guid id)
    {
        lock (sync)
        {
            return commandLookup.GetValueOrDefault(id);
        }
    }

    public string GetDeviceState(string deviceId)
    {
        lock (sync)
        {
            return deviceStates.GetValueOrDefault(deviceId, "Online");
        }
    }

    public Dictionary<string, string> GetDeviceStates()
    {
        lock (sync)
        {
            return new Dictionary<string, string>(
                deviceStates,
                StringComparer.OrdinalIgnoreCase
            );
        }
    }

    public object GetStatistics()
    {
        lock (sync)
        {
            return new
            {
                totalCommands = commandHistory.Count,
                criticalCommands = commandHistory.Count(
                    command => command.Priority == "Critical"
                ),
                normalQueueCount = normalQueue.Count,
                priorityQueueCount = priorityQueue.Count,
                undoStackCount = undoStack.Count,
                disconnectedDeviceCount = disconnectedDevices.Count,
                chronologicalEntries = commandTimeline.Count,
                trackedDeviceStates = deviceStates.Count
            };
        }
    }

    public List<string> GetDisconnectedDevices()
    {
        lock (sync)
        {
            return disconnectedDevices.OrderBy(id => id).ToList();
        }
    }
}
