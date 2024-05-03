"use strict";
const logList = document.getElementById("log");
const debugOutputEnabledChk = document.getElementById('debugOutputEnabled');

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
        if (severity === "debug") {
            logEntry.classList.add("debug");
        }

        logList.appendChild(logEntry);

        logList.parentElement.scrollTo({
            top: logList.scrollHeight,
            left: 0,
            behavior: "smooth",
        });
    }
}

function checkDebugLogging() {
    if (debugOutputEnabledChk.checked) {
        logList.classList.add("showDebug");
    } else {
        logList.classList.remove("showDebug");
    }
}

debugOutputEnabledChk.addEventListener("click", checkDebugLogging);

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

function startCodeExecution(assignmentId, files, buildOnly, onExecutionFinished) {
    let payload = {
        AssignmentId: assignmentId,
        BuildOnly: buildOnly,
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


function registerSourceErrorHandler(handler) {
    if (connection !== null && connection.state == "Connected") {
        connection.on("SourceErrors", handler);
    }
}

connection.start().then(function () {
    runBtn.disabled = false;
    checkDebugLogging();
    logMessage("Connected to backend with SignalR");
}).catch(function (err) {
    logMessage(err.toString(), "error");
    return console.error(err);
});