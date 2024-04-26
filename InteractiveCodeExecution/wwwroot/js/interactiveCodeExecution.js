"use strict";
const logList = document.getElementById("log");

// Temp:
const userIdInput = document.getElementById("uniqueUserIdInput");
const execInput = document.getElementById("execInput");
const backgroundInput = document.getElementById("backgroundInput");
const buildInput = document.getElementById("buildInput");
const payloadTypeSelector = document.getElementById("payloadTypeSelector");

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/executorHub")
    .withHubProtocol(new signalR.protocols.msgpack.MessagePackHubProtocol())
    .build();

let currentStreaming;

function logMessage(message, severity = "information") {
    for (let singleMsg of message.split("\n")) {
        let timePill = document.createElement("span");
        timePill.className = "font-monospace me-2 ps-1 pe-1 border rounded text-white bg-" + getBootstrapColorFromSeverity(severity);
        timePill.textContent = (new Date()).toLocaleTimeString()
        var text = document.createElement("span");
        text.textContent = singleMsg;

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
}

function startCodeExecution(files, onExecutionFinished) {
    let payload = {
        PayloadType: payloadTypeSelector.value,
        ExecCmd: execInput.value,
        BuildCmd: buildInput.value,
        BackgroundCmd: backgroundInput.value,
        Files: []
    };

    for (let file of Object.entries(files)) {
        payload.Files.push({
            Filepath: file[0],
            Content: file[1].getCurrentContent(),
            type: "Utf8TextFile"
        });
    }

    const startTime = performance.now();
    abortStreaming();
    logList.innerHTML = "";

    currentStreaming = connection.stream("ExecutePayloadByStream", payload)
        .subscribe({
            next: (item) => {
                logMessage(item.Message, item.Severity)
            },
            complete: () => {
                stopAllStreaming();
                logMessage("Stream completed from JavaScript-side!", "debug");
                const endTime = performance.now();
                logMessage(`Total runtime: ${endTime - startTime} ms`, "debug");
                currentStreaming = undefined;
                if (onExecutionFinished) {
                    onExecutionFinished();
                }
            },
            error: (err) => {
                stopAllStreaming();
                logMessage("Stream error: " + err, "error");
                console.error(error);
                currentStreaming = undefined;
                if (onExecutionFinished) {
                    onExecutionFinished();
                }
            },
        });
}

connection.on("LogMessage", function (message) {
    logMessage(message);
});


connection.start().then(function () {
    runBtn.disabled = false;
    logMessage("Connected to backend with SignalR");
}).catch(function (err) {
    logMessage(err.toString(), "error");
    return console.error(err);
});