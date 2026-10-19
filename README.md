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
