const api = "";

let registeredSensors = [];
let commandHistory = [];
let searchTimer = null;
let lastRecordedSearch = "";
let activeDashboard = "sensors";
let refreshInProgress = false;

document.addEventListener("DOMContentLoaded", () => {
    document.getElementById("sensorForm")?.addEventListener("submit", registerSensor);
    document.getElementById("telemetryForm")?.addEventListener("submit", sendTelemetry);
    document.getElementById("uploadForm")?.addEventListener("submit", uploadFile);
    document.getElementById("commandForm")?.addEventListener("submit", submitCommand);
    document.getElementById("undoCommandBtn")?.addEventListener("click", undoLastCommand);

    document.getElementById("commandSearch")?.addEventListener("input", () => {
        clearTimeout(searchTimer);
        loadCommandStream();
        searchTimer = setTimeout(trackCommandSearch, 700);
    });

    showSensorDashboard();

    setInterval(async () => {
        if (document.hidden || refreshInProgress) return;

        refreshInProgress = true;

        try {
            await checkApi();

            if (activeDashboard === "commands") {
                await Promise.allSettled([
                    loadCommandStream(),
                    loadCommandStatistics(),
                    loadSuggestions(),
                    loadDeviceStates(),
                    loadTelemetryQueue()
                ]);
            } else {
                await Promise.allSettled([
                    loadTelemetry(),
                    loadAlerts()
                ]);
            }
        } finally {
            refreshInProgress = false;
        }
    }, 12000);
});

function setText(id, value) {
    const element = document.getElementById(id);

    if (element) {
        element.textContent = value ?? "";
    }
}

function escapeHtml(value) {
    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#39;");
}

function formatDate(value) {
    if (!value) return "—";

    const date = new Date(value);

    return Number.isNaN(date.getTime())
        ? escapeHtml(value)
        : date.toLocaleString();
}

async function requestJson(url, options = {}) {
    const response = await fetch(`${api}${url}`, options);
    const contentType = response.headers.get("content-type") || "";

    const data = contentType.includes("application/json")
        ? await response.json()
        : {};

    if (!response.ok) {
        let message = data.message || "The request could not be completed.";

        if (Array.isArray(data.errors)) {
            message = data.errors.map(error =>
                typeof error === "string"
                    ? error
                    : JSON.stringify(error)
            ).join(", ");
        }

        throw new Error(message);
    }

    return data;
}

function jsonOptions(method, body) {
    return {
        method,
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify(body)
    };
}

function showSensorDashboard() {
    activeDashboard = "sensors";

    document.getElementById("sensorDashboard").hidden = false;
    document.getElementById("commandDashboard").hidden = true;

    document.getElementById("sensorPillar").classList.add("active");
    document.getElementById("commandPillar").classList.remove("active");

    setText("pageTitle", "Smart-X Gateway Dashboard");

    setText(
        "pageDescription",
        "Register simulated IoT sensors, send multi-type telemetry, attach device files, and monitor anomaly alerts."
    );

    loadAll();
}

function showCommandStream() {
    activeDashboard = "commands";

    document.getElementById("sensorDashboard").hidden = true;
    document.getElementById("commandDashboard").hidden = false;

    document.getElementById("sensorPillar").classList.remove("active");
    document.getElementById("commandPillar").classList.add("active");

    setText("pageTitle", "Real-Time Command Stream & History");

    setText(
        "pageDescription",
        "Manage device commands, prioritise critical telemetry, review processing history, and explore predictive actions."
    );

    loadCommandDashboard();
}

async function loadAll() {
    await Promise.allSettled([
        checkApi(),
        loadSensors(),
        loadTelemetry(),
        loadAlerts()
    ]);
}

async function checkApi() {
    try {
        await requestJson("/api/health");
        setText("apiStatus", "API Online");
    } catch {
        setText("apiStatus", "API Offline");
    }
}

async function loadSensors() {
    try {
        const response = await requestJson("/api/sensors");

        registeredSensors = Array.isArray(response)
            ? response
            : [];

        setText("sensorCount", registeredSensors.length);
        setText("commandDeviceCount", registeredSensors.length);

        renderSensors();
        populateDeviceSelects();
    } catch (error) {
        setText("sensorMessage", error.message);
    }
}

function renderSensors() {
    const container = document.getElementById("sensorCards");

    if (!container) return;

    container.innerHTML = registeredSensors.length
        ? registeredSensors.map(sensor => `
            <article class="sensor-card">
                <h4>${escapeHtml(sensor.deviceId)}</h4>
                <p>${escapeHtml(sensor.location)}</p>
                <p>${escapeHtml(sensor.category)}</p>
                <small>${escapeHtml(sensor.status || "Online")}</small>
            </article>
        `).join("")
        : "<p>No sensors registered yet.</p>";
}

function populateDeviceSelects() {
    const selectIds = [
        "telemetryDevice",
        "uploadDevice",
        "commandDevice"
    ];

    for (const id of selectIds) {
        const select = document.getElementById(id);

        if (!select) continue;

        const previous = select.value;

        select.replaceChildren();
        select.add(new Option("Select a device", ""));

        for (const sensor of registeredSensors) {
            select.add(
                new Option(sensor.deviceId, sensor.deviceId)
            );
        }

        if (registeredSensors.some(sensor =>
            sensor.deviceId === previous
        )) {
            select.value = previous;
        }
    }
}

async function registerSensor(event) {
    event.preventDefault();

    const sensor = {
        deviceId: document.getElementById("deviceId").value.trim(),
        location: document.getElementById("location").value.trim(),
        category: document.getElementById("category").value
    };

    try {
        await requestJson(
            "/api/sensors",
            jsonOptions("POST", sensor)
        );

        setText("sensorMessage", "Sensor registered successfully.");

        event.target.reset();

        await loadSensors();
    } catch (error) {
        setText("sensorMessage", error.message);
    }
}

async function sendTelemetry(event) {
    event.preventDefault();

    const input = {
        deviceId: document.getElementById("telemetryDevice").value,
        metric: document.getElementById("metric").value.trim(),
        dataType: document.getElementById("dataType").value,
        value: document.getElementById("value").value.trim(),
        unit: document.getElementById("unit").value.trim()
    };

    try {
        await requestJson(
            "/api/telemetry",
            jsonOptions("POST", input)
        );

        setText(
            "telemetryMessage",
            "Telemetry submitted successfully."
        );

        await Promise.allSettled([
            loadTelemetry(),
            loadAlerts(),
            loadSuggestions()
        ]);
    } catch (error) {
        setText("telemetryMessage", error.message);
    }
}

async function loadTelemetry() {
    try {
        const response = await requestJson("/api/telemetry");

        const records = Array.isArray(response)
            ? response
            : [];

        setText("telemetryCount", records.length);

        const table = document.getElementById("telemetryTable");

        if (!table) return;

        table.innerHTML = records.length
            ? records.map(record => `
                <tr>
                    <td>${formatDate(record.timestamp)}</td>
                    <td>${escapeHtml(record.deviceId)}</td>
                    <td>${escapeHtml(record.metric)}</td>
                    <td>${escapeHtml(record.dataType || "")}</td>
                    <td>${escapeHtml(record.displayValue ?? record.value ?? "")}</td>
                </tr>
            `).join("")
            : '<tr><td colspan="5">No telemetry records yet.</td></tr>';
    } catch (error) {
        console.error("Telemetry error:", error);
    }
}

async function loadAlerts() {
    try {
        const response = await requestJson("/api/alerts");

        const alerts = Array.isArray(response)
            ? response
            : [];

        setText("alertCount", alerts.length);

        const container = document.getElementById("alerts");

        if (!container) return;

        container.innerHTML = alerts.length
            ? alerts.map(alert => `
                <div class="alert-item">
                    ${escapeHtml(
                        typeof alert === "string"
                            ? alert
                            : alert.message ||
                              alert.description ||
                              JSON.stringify(alert)
                    )}
                </div>
            `).join("")
            : "<p>No active alerts.</p>";
    } catch (error) {
        console.error("Alerts error:", error);
    }
}

async function uploadFile(event) {
    event.preventDefault();

    const deviceId = document.getElementById("uploadDevice").value;
    const file = document.getElementById("fileInput").files[0];

    if (!deviceId || !file) {
        setText(
            "uploadMessage",
            "Select a device and a file."
        );
        return;
    }

    const formData = new FormData();
    formData.append("file", file);

    try {
        const response = await requestJson(
            `/api/sensors/${encodeURIComponent(deviceId)}/upload`,
            {
                method: "POST",
                body: formData
            }
        );

        setText(
            "uploadMessage",
            response.message || "File uploaded successfully."
        );

        event.target.reset();
    } catch (error) {
        setText("uploadMessage", error.message);
    }
}

async function runDemo(endpoint, method = "GET") {
    try {
        const response = await requestJson(
            endpoint,
            { method }
        );

        setText(
            "demoOutput",
            JSON.stringify(response, null, 2)
        );
    } catch (error) {
        setText("demoOutput", error.message);
    }
}

async function loadCommandDashboard() {
    await loadSensors();

    ensureTelemetryQueuePanel();

    await Promise.allSettled([
        loadCommandStream(),
        loadCommandStatistics(),
        loadSuggestions(),
        loadDeviceStates(),
        loadTelemetryQueue()
    ]);
}

async function submitCommand(event) {
    event.preventDefault();

    const request = {
        deviceId: document.getElementById("commandDevice").value,
        action: document.getElementById("commandAction").value,
        priority: document.getElementById("commandPriority").value
    };

    if (!request.deviceId || !request.action) {
        setText(
            "commandMessage",
            "Select a device and command."
        );
        return;
    }

    try {
        const response = await requestJson(
            "/api/commands",
            jsonOptions("POST", request)
        );

        setText(
            "commandMessage",
            `${response.command.action} queued for ${response.command.deviceId}.`
        );

        await refreshCommandData();
    } catch (error) {
        setText("commandMessage", error.message);
    }
}

async function processNextCommand() {
    try {
        const response = await requestJson(
            "/api/commands/process-next",
            { method: "POST" }
        );

        setText(
            "commandMessage",
            `${response.command.action} executed for ${response.command.deviceId}.`
        );

        await refreshCommandData();
        await loadSensors();
        await loadAlerts();
    } catch (error) {
        setText("commandMessage", error.message);
    }
}

async function undoLastCommand() {
    try {
        const response = await requestJson(
            "/api/commands/undo",
            { method: "POST" }
        );

        setText(
            "commandMessage",
            `Previous simulated state restored for ${response.command.deviceId}.`
        );

        await refreshCommandData();
        await loadSensors();
        await loadAlerts();
    } catch (error) {
        setText("commandMessage", error.message);
    }
}

async function refreshCommandData() {
    await Promise.allSettled([
        loadCommandStream(),
        loadCommandStatistics(),
        loadSuggestions(),
        loadDeviceStates(),
        loadTelemetryQueue()
    ]);
}

async function loadCommandStream() {
    const search = document
        .getElementById("commandSearch")
        ?.value.trim() || "";

    try {
        const endpoint = search
            ? `/api/commands?search=${encodeURIComponent(search)}`
            : "/api/commands";

        const response = await requestJson(endpoint);

        commandHistory = Array.isArray(response)
            ? response
            : [];

        renderCommandHistory();

        setText(
            "commandSearchMessage",
            `${commandHistory.length} matching command(s).`
        );
    } catch (error) {
        setText("commandSearchMessage", error.message);
    }
}

function renderCommandHistory() {
    const table = document.getElementById("commandTable");

    if (!table) return;

    table.innerHTML = commandHistory.length
        ? commandHistory.map(command => `
            <tr>
                <td>${formatDate(command.timestamp)}</td>
                <td>${escapeHtml(command.deviceId)}</td>
                <td>${escapeHtml(command.action)}</td>
                <td>${escapeHtml(command.priority)}</td>
                <td>${escapeHtml(command.status)}</td>
            </tr>
        `).join("")
        : '<tr><td colspan="5">No commands found.</td></tr>';
}

async function loadCommandStatistics() {
    try {
        const stats = await requestJson("/api/commands/stats");

        setText("commandCount", stats.totalCommands);
        setText("criticalCommandCount", stats.criticalCommands);

        renderQueueControls(stats);
    } catch (error) {
        setText("commandMessage", error.message);
    }
}

function renderQueueControls(stats) {
    const form = document.getElementById("commandForm");

    if (!form) return;

    let controls = document.getElementById("queueControls");

    if (!controls) {
        controls = document.createElement("div");
        controls.id = "queueControls";
        controls.style.marginTop = "18px";

        form.insertAdjacentElement("afterend", controls);
    }

    const normal = Number(stats.normalQueueCount || 0);
    const critical = Number(stats.priorityQueueCount || 0);
    const waiting = normal + critical;

    controls.replaceChildren();

    const total = document.createElement("p");
    total.textContent = `Waiting commands: ${waiting}`;

    const breakdown = document.createElement("p");
    breakdown.textContent =
        `Normal: ${normal} | Critical: ${critical}`;

    const button = document.createElement("button");
    button.type = "button";
    button.className = "primary-btn";
    button.textContent = "Process Next Command";
    button.disabled = waiting === 0;
    button.addEventListener("click", processNextCommand);

    controls.append(total, breakdown, button);
}

async function loadSuggestions() {
    const container = document.getElementById("suggestions");

    if (!container) return;

    try {
        const response = await requestJson("/api/predictions");

        const suggestions = Array.isArray(response)
            ? response
            : [];

        setText(
            "suggestionMessage",
            suggestions.length
                ? `${suggestions.length} proactive recommendation(s) available.`
                : "No recommendations currently. Submit telemetry or process commands to generate insights."
        );

        container.innerHTML = suggestions.length
            ? suggestions.map(suggestion => {
                const action = String(
                    suggestion.recommendedAction || ""
                ).toUpperCase();

                const advisory = [
                    "INSPECT",
                    "MONITOR"
                ].includes(action);

                return `
                    <div class="alert-item">
                        <strong>${escapeHtml(suggestion.deviceId)}</strong>
                        <p>
                            Recommended action:
                            <strong>${escapeHtml(action)}</strong>
                        </p>
                        <p>${escapeHtml(suggestion.reason)}</p>
                        <small>
                            Confidence: ${escapeHtml(suggestion.confidence)}
                        </small>
                        <p>
                            <small>
                                ${advisory
                                    ? "Advisory recommendation — manual review required."
                                    : "Suggested device command — requires manual submission."}
                            </small>
                        </p>
                    </div>
                `;
            }).join("")
            : "<p>No suggested actions yet.</p>";
    } catch (error) {
        setText("suggestionMessage", error.message);
        container.replaceChildren();
    }
}

async function trackCommandSearch() {
    const search = document
        .getElementById("commandSearch")
        ?.value.trim() || "";

    if (!search) {
        lastRecordedSearch = "";
        return;
    }

    const sensor = registeredSensors.find(item =>
        item.deviceId.toLowerCase() === search.toLowerCase()
    );

    if (!sensor) return;

    if (
        lastRecordedSearch.toLowerCase() ===
        sensor.deviceId.toLowerCase()
    ) {
        return;
    }

    try {
        await requestJson(
            "/api/predictions/search",
            jsonOptions("POST", {
                deviceId: sensor.deviceId
            })
        );

        lastRecordedSearch = sensor.deviceId;

        await loadSuggestions();
    } catch (error) {
        console.error("Search tracking error:", error);
    }
}

async function loadDeviceStates() {
    const dashboard = document.getElementById("commandDashboard");

    if (!dashboard) return;

    let panel = document.getElementById("deviceStatesPanel");

    if (!panel) {
        panel = document.createElement("section");
        panel.id = "deviceStatesPanel";
        panel.className = "panel";

        const heading = document.createElement("div");
        heading.className = "panel-heading";

        const title = document.createElement("h3");
        title.textContent = "Live Device States";

        heading.appendChild(title);
        panel.appendChild(heading);

        const content = document.createElement("div");
        content.id = "deviceStatesContent";
        content.className = "sensor-grid";

        panel.appendChild(content);
        dashboard.appendChild(panel);
    }

    const content = document.getElementById("deviceStatesContent");

    try {
        const states = await requestJson(
            "/api/commands/device-states"
        );

        content.replaceChildren();

        if (registeredSensors.length === 0) {
            content.textContent = "No registered devices yet.";
            return;
        }

        for (const sensor of registeredSensors) {
            const matchingKey = Object.keys(states).find(key =>
                key.toLowerCase() === sensor.deviceId.toLowerCase()
            );

            const state = matchingKey
                ? states[matchingKey]
                : sensor.status || "Online";

            const card = document.createElement("article");
            card.className = "sensor-card";

            const title = document.createElement("h4");
            title.textContent = sensor.deviceId;

            const status = document.createElement("p");
            status.textContent = `Status: ${state}`;

            const location = document.createElement("small");
            location.textContent = sensor.location || "";

            card.append(title, status, location);
            content.appendChild(card);
        }
    } catch (error) {
        content.textContent =
            `Unable to load device states: ${error.message}`;
    }
}

function ensureTelemetryQueuePanel() {
    const dashboard = document.getElementById("commandDashboard");

    if (!dashboard) return;

    if (document.getElementById("telemetryQueuePanel")) {
        return;
    }

    const panel = document.createElement("section");
    panel.id = "telemetryQueuePanel";
    panel.className = "panel";

    panel.innerHTML = `
        <div class="panel-heading">
            <div>
                <h3>Telemetry Processing Queue</h3>
                <p>
                    FIFO sensor telemetry with critical-alert prioritisation.
                </p>
            </div>
        </div>

        <div class="sensor-grid">
            <article class="sensor-card">
                <h4 id="telemetryNormalCount">0</h4>
                <p>Normal Packets</p>
            </article>

            <article class="sensor-card">
                <h4 id="telemetryCriticalCount">0</h4>
                <p>Critical Packets</p>
            </article>

            <article class="sensor-card">
                <h4 id="telemetryProcessedCount">0</h4>
                <p>Processed Packets</p>
            </article>

            <article class="sensor-card">
                <h4 id="telemetryFailedCount">0</h4>
                <p>Failed Packets</p>
            </article>
        </div>

        <div id="telemetryQueueButtons" style="display:flex;gap:12px;flex-wrap:wrap;margin-top:18px;">
            <button type="button" class="primary-btn" id="queueDemoBtn">
                Queue Demo Packets
            </button>

            <button type="button" class="primary-btn" id="processTelemetryBtn">
                Process Next Packet
            </button>

            <button type="button" class="primary-btn" id="refreshTelemetryBtn">
                Refresh Queue
            </button>
        </div>

        <p id="telemetryQueueMessage" role="status" aria-live="polite"></p>

        <h4 style="margin-top:22px;">Processing History</h4>

        <div style="overflow-x:auto;">
            <table>
                <thead>
                    <tr>
                        <th>Time</th>
                        <th>Device</th>
                        <th>Metric</th>
                        <th>Priority</th>
                        <th>Status</th>
                    </tr>
                </thead>

                <tbody id="telemetryQueueHistory">
                    <tr>
                        <td colspan="5">No packets processed yet.</td>
                    </tr>
                </tbody>
            </table>
        </div>
    `;

    dashboard.appendChild(panel);

    document.getElementById("queueDemoBtn")
        .addEventListener("click", queueDemoPackets);

    document.getElementById("processTelemetryBtn")
        .addEventListener("click", processNextTelemetry);

    document.getElementById("refreshTelemetryBtn")
        .addEventListener("click", loadTelemetryQueue);
}

async function loadTelemetryQueue() {
    ensureTelemetryQueuePanel();

    try {
        const [stats, history] = await Promise.all([
            requestJson("/api/telemetry/queue/stats"),
            requestJson("/api/telemetry/queue/history")
        ]);

        setText(
            "telemetryNormalCount",
            stats.normalQueueCount ?? 0
        );

        setText(
            "telemetryCriticalCount",
            stats.criticalQueueCount ?? 0
        );

        setText(
            "telemetryProcessedCount",
            stats.totalProcessed ?? 0
        );

        setText(
            "telemetryFailedCount",
            stats.totalFailed ?? 0
        );

        const processButton = document.getElementById(
            "processTelemetryBtn"
        );

        if (processButton) {
            processButton.disabled =
                Number(stats.totalPending || 0) === 0;
        }

        const table = document.getElementById(
            "telemetryQueueHistory"
        );

        if (!table) return;

        const records = Array.isArray(history)
            ? history
            : [];

        table.innerHTML = records.length
            ? records.slice().reverse().map(record => `
                <tr>
                    <td>${formatDate(record.timestamp)}</td>
                    <td>${escapeHtml(record.deviceId)}</td>
                    <td>${escapeHtml(record.metric)}</td>
                    <td>${escapeHtml(record.priority)}</td>
                    <td>${escapeHtml(record.status)}</td>
                </tr>
            `).join("")
            : '<tr><td colspan="5">No packets processed yet.</td></tr>';
    } catch (error) {
        setText(
            "telemetryQueueMessage",
            `Unable to load telemetry queue: ${error.message}`
        );
    }
}

async function queueDemoPackets() {
    const button = document.getElementById("queueDemoBtn");

    if (button) button.disabled = true;

    try {
        const response = await requestJson(
            "/api/telemetry/queue/demo",
            { method: "POST" }
        );

        setText(
            "telemetryQueueMessage",
            response.message || "Demo packets queued successfully."
        );

        await loadTelemetryQueue();
    } catch (error) {
        setText("telemetryQueueMessage", error.message);
    } finally {
        if (button) button.disabled = false;
    }
}

async function processNextTelemetry() {
    const button = document.getElementById(
        "processTelemetryBtn"
    );

    if (button) button.disabled = true;

    try {
        const result = await requestJson(
            "/api/telemetry/queue/process-next",
            { method: "POST" }
        );

        const packet = result.result || result;

        setText(
            "telemetryQueueMessage",
            `${packet.priority || "Telemetry"} packet: ${packet.deviceId || "Device"} — ${packet.status || "Processed"}.`
        );

        await Promise.allSettled([
            loadTelemetryQueue(),
            loadTelemetry(),
            loadAlerts(),
            loadSuggestions()
        ]);
    } catch (error) {
        setText("telemetryQueueMessage", error.message);
        await loadTelemetryQueue();
    }
}