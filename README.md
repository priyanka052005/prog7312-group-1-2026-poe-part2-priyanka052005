# Smart-X Part 1 — Data Ingestion and Validation Gateway

## Overview

This project implements Part 1 of the Smart-X IoT Mesh Ecosystem.

The application contains:

- A .NET 10 ASP.NET Core backend API.
- A browser-based frontend dashboard.
- Sensor registration.
- Simulated multi-type telemetry ingestion.
- `TelemetryPacket<T>` generic wrapper.
- Operator overloading through `PowerReading`.
- Jagged array telemetry batching.
- Conversion into `List<T>`.
- Recursive deployment validation.
- Multipart media/log upload.
- Dynamic anomaly and disconnect alerts.
- Mock seeded sensor and telemetry data.

## Requirements

Install:

- Visual Studio 2022
- ASP.NET and web development workload
- .NET 8 SDK
- Git

## Restore and Run

### Visual Studio 2022

1. Open `SmartX.VisualStudio.sln`.
2. Allow Visual Studio to restore packages.
3. Select the `https` or `http` launch profile.
4. Press **Ctrl+F5** to run without debugging, or **F5** to debug.

### Command line alternative

```bash
dotnet restore
dotnet build
dotnet run
```

The terminal will display a local URL such as:

```text
http://localhost:5000
```

or:

```text
https://localhost:7000
```

Open that URL in your browser.

## Main API Endpoints

### API health

```http
GET /api/health
```

### Get sensors

```http
GET /api/sensors
```

### Register sensor

```http
POST /api/sensors
Content-Type: application/json
```

Example:

```json
{
  "deviceId": "ESP32-D4",
  "location": "Facility A / Zone 2",
  "category": "Environmental"
}
```

### Get telemetry

```http
GET /api/telemetry
```

### Submit telemetry

```http
POST /api/telemetry
Content-Type: application/json
```

Example double:

```json
{
  "deviceId": "ESP32-A1",
  "metric": "Temperature",
  "dataType": "double",
  "value": "24.8",
  "unit": "°C"
}
```

Example integer:

```json
{
  "deviceId": "ESP32-B2",
  "metric": "Power",
  "dataType": "int",
  "value": "950",
  "unit": "W"
}
```

Example boolean:

```json
{
  "deviceId": "ESP32-C3",
  "metric": "ValveOpen",
  "dataType": "bool",
  "value": "true",
  "unit": ""
}
```

### File upload

```http
POST /api/sensors/{deviceId}/upload
Content-Type: multipart/form-data
```

Allowed extensions:

- `.txt`
- `.log`
- `.json`
- `.jpg`
- `.jpeg`
- `.png`
- `.pdf`

Maximum file size: 5 MB.

## Advanced C# Requirement Demonstrations

### Generics

`Models/TelemetryPacket.cs`

The class is generic and supports different telemetry data types without one fixed value type.

### Operator Overloading

`Models/PowerReading.cs`

Test endpoint:

```http
GET /api/demo/operator-overload
```

### Jagged Arrays and Lists

Test endpoint:

```http
POST /api/telemetry/demo-batch
```

This creates `double[][]` batches and moves the values into a `List<double>`.

### Recursion

`Services/DeploymentValidator.cs`

Test endpoint:

```http
GET /api/demo/recursive-validation
```

The recursive method validates the deployment path from Facility -> Zone -> Sub-Zone -> Sensor.

## Dynamic Engagement Strategy

The dashboard uses a live anomaly and sensor-disconnect alert panel.

It highlights:

- High temperatures.
- Low moisture values.
- Power spikes.
- Sensors that have stopped sending telemetry.

This gives operators immediate visual feedback and helps them identify problematic sensors quickly.

## GitHub

Create a GitHub repository and commit regularly while developing the project.

Example commit messages:

```text
Initial Smart-X project setup
Add sensor registration model
Add sensor validation
Add telemetry generic packet
Add telemetry API endpoint
Add operator overload demonstration
Add recursive deployment validation
Add jagged telemetry batch demo
Add file upload endpoint
Create dashboard layout
Add live sensor cards
Add anomaly alert engine
Add telemetry form
Add file upload interface
Improve responsive styling
Add seeded IoT telemetry data
Improve input validation
Add README setup instructions
Fix telemetry display
Final Part 1 testing
```
---

# Smart-X Part 2 — Advanced Data Structures and Predictive Intelligence

## Overview

Part 2 extends the Smart-X IoT Mesh Ecosystem with advanced C# collections, priority-based telemetry processing, command management, and predictive recommendations.

The application consists of an ASP.NET Core REST API and a browser-based frontend developed using HTML, CSS and JavaScript.

## Part 2 Features

- Navigation between Part 1 and Part 2, with Part 3 reserved.
- FIFO telemetry processing using `Queue<T>`.
- Critical telemetry prioritisation using `PriorityQueue<TElement, TPriority>`.
- Command undo functionality using `Stack<T>`.
- Device registration and lookup using `Dictionary<TKey, TValue>`.
- Chronological telemetry storage using `SortedDictionary<TKey, TValue>`.
- Duplicate disconnect-alert prevention using `HashSet<T>`.
- Telemetry trend detection and predictive recommendations.
- Cross-device recommendations for related IoT devices.

## Advanced Data Structures

### Queue and PriorityQueue

**File:** `Services/TelemetryQueueService.cs`

`Queue<T>` processes normal telemetry in First-In, First-Out order.

`PriorityQueue<TElement, TPriority>` prioritises critical telemetry, allowing urgent readings to be processed before normal readings.

Critical conditions include:

- Moisture below 20%.
- Temperature above 30°C.
- Power above 1200 W.

### Stack

**File:** `Services/CommandService.cs`

`Stack<T>` maintains an undo history for simulated device commands.

When a command is reversed, the previous device state is restored using Last-In, First-Out processing.

### Dictionary

**File:** `Services/SmartXStore.cs`

`Dictionary<TKey, TValue>` stores registered devices by their unique device IDs, allowing efficient lookup and validation.

### SortedDictionary

**File:** `Services/TelemetryHistoryService.cs`

`SortedDictionary<TKey, TValue>` maintains telemetry records in chronological order for history retrieval and trend analysis.

### HashSet

**File:** `Services/DisconnectAlertService.cs`

`HashSet<T>` tracks devices with active disconnect alerts and prevents duplicate notifications for the same device.

## Predictive Recommendation Engine

**File:** `Services/PredictionService.cs`

The predictive engine analyses telemetry trends, device search activity, command history and critical thresholds to generate suggested actions.

### Trend Detection

The system identifies patterns such as continuously decreasing moisture readings.

For example:

`65 → 55 → 45 → 35 → 25`

This pattern can produce a `MONITOR` recommendation.

### Critical Predictions

When a moisture reading falls below 20%, the system can generate an `INSPECT` recommendation.

### Cross-Device Predictions

When environmental sensor `ESP32-A1` detects critically low moisture, the system can recommend checking whether irrigation actuator `ESP32-C3` should be started.

Recommendations are advisory and do not automatically execute commands.

## Part 2 API Endpoints

### Telemetry Queue

```http
GET /api/telemetry/queue
POST /api/telemetry/queue/process-next
GET /api/telemetry/queue/stats
GET /api/telemetry/queue/history
POST /api/telemetry/queue/demo
```

### Disconnect Alerts

```http
GET /api/disconnect-alerts
GET /api/disconnect-alerts/history
GET /api/disconnect-alerts/stats
POST /api/disconnect-alerts/test/{deviceId}
POST /api/disconnect-alerts/reconnect/{deviceId}
```

### Predictions

```http
GET /api/predictions
```

## Running and Testing Part 2

### Requirements

- .NET 8 SDK
- Visual Studio Code or Visual Studio 2022
- Web browser
- PowerShell for optional API testing

### Start the Application

Open the project folder and run:

```bash
dotnet restore
dotnet build
dotnet run
```

Open the local URL displayed in the terminal.

### Test 1 — Priority Queue

1. Open the Part 2 dashboard.
2. Generate demo telemetry using the queue demonstration feature.
3. View the queue statistics.
4. Process the queued readings.
5. Confirm that critical telemetry is processed before normal telemetry.

### Test 2 — Command Undo

1. Open the device command section.
2. Execute a simulated command.
3. Check the updated device state.
4. Use the undo functionality.
5. Confirm that the previous device state is restored.

### Test 3 — Duplicate Disconnect Prevention

Run the following commands in PowerShell while Smart-X is running:

```powershell
Invoke-RestMethod -Method Post -Uri "http://localhost:5188/api/disconnect-alerts/test/ESP32-A1"

Invoke-RestMethod -Method Post -Uri "http://localhost:5188/api/disconnect-alerts/test/ESP32-A1"
```

Expected result:

- First request: `alertCreated = true`.
- Second request: `alertCreated = false`.

### Test 4 — Predictive Recommendations

Submit decreasing moisture readings for `ESP32-A1` and open:

```http
GET /api/predictions
```

Expected result: A recommendation identifying the declining moisture trend.

### Test 5 — Cross-Device Predictions

Submit a moisture reading of 12% for `ESP32-A1`.

Open:

```http
GET /api/predictions
```

Expected recommendations:

- `ESP32-A1` — `INSPECT`.
- `ESP32-C3` — `START`.

The second recommendation is advisory and relates to the simulated irrigation actuator.

## Project Structure

```text
SmartX/
Models/
Services/
CommandService.cs
DisconnectAlertService.cs
PredictionService.cs
SmartXStore.cs
TelemetryHistoryService.cs
TelemetryQueueService.cs
wwwroot/
app.js
index.html
styles.css
smartx-logo.png
Program.cs
README.md
SmartX.VisualStudio.csproj
SmartX.VisualStudio.sln
```

## Technologies Used

- C#
- .NET 8
- ASP.NET Core
- REST APIs
- HTML
- CSS
- JavaScript
- Git and GitHub

## Conclusion

Smart-X Part 2 demonstrates the practical application of advanced C# collections within a simulated IoT environment. The system supports prioritised telemetry processing, efficient device lookup, command undo, duplicate-alert prevention, and predictive recommendations based on device activity and telemetry trends.

## Part 2 — Testing Instructions

### Testing the Queue and PriorityQueue
1. Run the application using `dotnet run`.
2. Open the Smart-X dashboard and navigate to **Part 2**.
3. Select the **Queue Demo** option to generate sample telemetry.
4. Click **Process Next** to process the readings.
5. Verify that critical readings are processed before normal readings.

### Simulating a High-Priority Alert
1. Navigate to **Part 1**.
2. Submit a moisture reading of **12%** for sensor `ESP32-A1`.
3. Return to **Part 2**.
4. Check the telemetry queue.
5. Verify that the critical reading receives priority.

### Testing the Dictionary
1. Navigate to **Part 1**.
2. View the registered sensors.
3. Submit telemetry for an existing sensor, such as `ESP32-A1`.
4. Verify that the telemetry is accepted.
5. Try submitting telemetry for an unregistered sensor.
6. Verify that the application rejects the unregistered device.

### Expected Results
- `Queue<T>` processes normal readings in FIFO order.
- `PriorityQueue<TElement, TPriority>` prioritises critical readings.
- `Dictionary<TKey, TValue>` supports registered device lookup and validation.
- `SortedDictionary<TKey, TValue>` maintains chronological telemetry history.