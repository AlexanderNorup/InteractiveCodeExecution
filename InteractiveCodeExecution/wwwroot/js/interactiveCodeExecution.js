"use strict";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/executorHub")
    .withHubProtocol(new signalR.protocols.msgpack.MessagePackHubProtocol())
    .build();

const payloadTypeSelector = document.getElementById("payloadTypeSelector");
const codeInput = document.getElementById("codeInput");
const codeInput2 = document.getElementById("codeInput2");
const execInput = document.getElementById("execInput");
const backgroundInput = document.getElementById("backgroundInput");
const buildInput = document.getElementById("buildInput");
const runBtn = document.getElementById("runBtn");
const clearLogsBtn = document.getElementById("clearLogsBtn");
const abortStreamingBtn = document.getElementById("abortStreamingBtn");

const logList = document.getElementById("log");

let currentStreaming;

function logMessage(message, severity = "information") {
    let timePill = document.createElement("span");
    timePill.className = "font-monospace me-2 ps-1 pe-1 border rounded text-white bg-" + getBootstrapColorFromSeverity(severity);
    timePill.textContent = (new Date()).toLocaleTimeString()
    var text = document.createElement("span");
    text.textContent = message;

    let logEntry = document.createElement("li");
    logEntry.className = "logEntry"
    logEntry.appendChild(timePill);
    logEntry.appendChild(text);
    logList.appendChild(logEntry);

    logList.parentElement.scrollTo({
        top: logList.scrollHeight,
        left: 0,
        behavior: "smooth",
    });
}

function abortStreaming() {
    if (stopAllStreaming != undefined) {
        stopAllStreaming();
    }

    if (currentStreaming != undefined) {
        currentStreaming.dispose();
        currentStreaming = undefined;
        logMessage("Execution aborted by user", "error");
    }
    runBtn.disabled = false;
    abortStreamingBtn.disabled = true;
}
abortStreamingBtn.addEventListener("click", abortStreaming);

connection.on("LogMessage", function (message) {
    logMessage(message);
});

runBtn.addEventListener("click", function (event) {
    let payload = {
        PayloadType: payloadTypeSelector.value,
        ExecCmd: execInput.value,
        BuildCmd: buildInput.value,
        BackgroundCmd: backgroundInput.value,
        Files: [
            {
                Filepath: "Program.cs",
                Content: codeInput.value,
                type: "Utf8TextFile"
            },
            {
                Filepath: "Project.csproj",
                Content: codeInput2.value,
                type: "Utf8TextFile"
            }
        ]
    };
    const startTime = performance.now();
    abortStreaming();

    runBtn.disabled = true;
    abortStreamingBtn.disabled = false;

    currentStreaming = connection.stream("ExecutePayloadByStream", payload)
        .subscribe({
            next: (item) => {
                logMessage(item.Message, item.Severity)
            },
            complete: () => {
                stopAllStreaming();
                logMessage("Stream completed from JavaScript-side!", "debug");
                runBtn.disabled = false;
                abortStreamingBtn.disabled = true;
                const endTime = performance.now();
                logMessage(`Total runtime: ${endTime - startTime} ms`, "debug");
                currentStreaming = undefined;
            },
            error: (err) => {
                stopAllStreaming();
                logMessage("Stream error: " + err, "error");
                console.error(error);
                runBtn.disabled = false;
                abortStreamingBtn.disabled = true;
                currentStreaming = undefined;
            },
        });
    event.preventDefault();
});

clearLogsBtn.addEventListener("click", function (event) {
    logList.innerHTML = "";
});

connection.start().then(function () {
    runBtn.disabled = false;
    logMessage("Connected to backend with SignalR");
}).catch(function (err) {
    logMessage(err.toString(), "error");
    return console.error(err);
});
