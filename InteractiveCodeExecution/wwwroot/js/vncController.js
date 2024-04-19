"use strict";

var vncConnection = new signalR.HubConnectionBuilder()
    .withUrl("/vncHub")
    .withHubProtocol(new signalR.protocols.msgpack.MessagePackHubProtocol())
    .build();

const canvasContainer = document.getElementById("canvasContainer");
const startStreamingButton = document.getElementById("startStreamingButton");
const canvas = document.getElementById("streamCanvas");
const ctx = canvas.getContext("2d");

let currentVncStream;

let mousePressed = false;
canvas.onmousemove = function (e) {
    vncConnection.invoke("PerformMouseEvent", e.offsetX, e.offsetY, mousePressed).catch(function (err) {
        return console.error(err.toString());
    });
};

canvas.onmouseup = function (e) {
    mousePressed = false;
    vncConnection.invoke("PerformMouseEvent", e.offsetX, e.offsetY, false).catch(function (err) {
        return console.error(err.toString());
    });
};

canvas.onmousedown = function (e) {
    mousePressed = true;
    vncConnection.invoke("PerformMouseEvent", e.offsetX, e.offsetY, true).catch(function (err) {
        return console.error(err.toString());
    });
};

function writeImageData(imgData) {
    let image = new Image();
    image.onload = function () {
        ctx.drawImage(image, 0, 0, canvas.width, canvas.height);
    };
    image.src = imgData;
}

vncConnection.on("ReceiveMessage", function (message) {
    logMessage(message);
});

vncConnection.on("ReceiveScreenshot", function (data) {
    writeImageData(data);
});

startStreamingButton.disabled = true;
vncConnection.start().then(function () {
    startStreamingButton.disabled = false;
}).catch(function (err) {
    return console.error(err.toString());
});

startStreamingButton.addEventListener("click", function (event) {
    let userId = prompt("[Temporary]: Please insert your user-id here: ");
    if (userId) {
        vncConnection.invoke("StartConnection", userId).then(function (data) {
            setTimeout(startVncStreaming, 5000); // Temp
        }).catch(function (err) {
            logMessage("Can't connect to screen!", "error");
            return console.error(err.toString());
        });
    }
    event.preventDefault();
});

function grabSingleScreenshot() {
    vncConnection.invoke("GetScreenshot").catch(function (err) {
        return console.error(err.toString());
    });
}

function startVncStreaming() {
    canvasContainer.classList.remove("d-none");
    currentVncStream = vncConnection.stream("StartLivestream")
        .subscribe({
            next: (item) => {
                writeImageData(item);
            },
            complete: () => {
                logMessage("Stream completed!");
                canvasContainer.classList.add("d-none");
                currentVncStream = undefined;
            },
            error: (err) => {
                logMessage("Stream error: " + err, "error");
                console.error(error);
                canvasContainer.classList.add("d-none");
                currentVncStream = undefined;
            },
        });
}

function stopAllStreaming() {
    if (currentVncStream != undefined) {
        currentVncStream.dispose();
        currentVncStream = undefined;
    }

    canvasContainer.classList.add("d-none");
    vncConnection.invoke("StopStreaming"); //Fire and forget
}