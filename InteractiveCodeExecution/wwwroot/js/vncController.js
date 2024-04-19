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

function getBase64UrlStringFromByteArray(imgData, mimeType) {
    let i = imgData.length;
    let binaryString = [i];
    while (i--) {
        binaryString[i] = String.fromCharCode(imgData[i]);
    }
    let data = binaryString.join('');

    let base64 = window.btoa(data);
    let url = "data:" + mimeType + ";base64," + base64;
    return url;
}

function writeImageData(imgData) {
    // The easiest way to draw an image from a bytearray is to convert it to base64 and display that.
    let url = getBase64UrlStringFromByteArray(imgData, "image/jpeg");
    let image = new Image();
    image.onload = function () {
        ctx.drawImage(image, 0, 0, canvas.width, canvas.height);
    };
    image.src = url;
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