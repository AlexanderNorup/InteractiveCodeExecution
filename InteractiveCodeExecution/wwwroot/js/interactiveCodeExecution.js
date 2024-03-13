"use strict";

const connection = new signalR.HubConnectionBuilder().withUrl("/executorHub").build();

const payloadTypeSelector = document.getElementById("payloadTypeSelector");
const codeInput = document.getElementById("codeInput");
const codeInput2 = document.getElementById("codeInput2");
const execInput = document.getElementById("execInput");
const buildInput = document.getElementById("buildInput");
const runBtn = document.getElementById("runBtn");
const clearLogsBtn = document.getElementById("clearLogsBtn");

const logList = document.getElementById("log");

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

connection.on("LogMessage", function (message) {
    logMessage(message);
});

runBtn.addEventListener("click", function (event) {
    let payload = {
        PayloadType: payloadTypeSelector.value,
        ExecCmd: execInput.value,
        BuildCmd: buildInput.value,
        Files: [
            {
                Filepath: "Program.cs",
                Content: codeInput.value
            },
            {
                Filepath: "Project.csproj",
                Content: codeInput2.value
            }
        ]
    };
    const startTime = performance.now();

    connection.stream("ExecutePayloadByStream", payload)
        .subscribe({
            next: (item) => {
                logMessage(item.message, item.severity)
            },
            complete: () => {
                logMessage("Stream completed from JavaScript-side!", "debug");
                runBtn.disabled = false;
                const endTime = performance.now();
                logMessage(`Total runtime: ${endTime - startTime} ms`, "debug");
            },
            error: (err) => {
                logMessage("Stream error: " + err, "error");
                console.error(error);
                runBtn.disabled = false;
            },
        });
    runBtn.disabled = true;
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
