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

let mouseX, mouseY;
let buttonsPressed;
let scrollDown, scrollUp = false;

let x11KeyMap = {};
async function fetchKeyMap() {
    let resp = await fetch("/JavaScriptToX11KeyboardKeyMap.json");
    x11KeyMap = await resp.json();
}

canvas.onmousemove = function (e) {
    mouseX = Math.round(e.offsetX * canvas.width / canvas.clientWidth);
    mouseY = Math.round(e.offsetY * canvas.width / canvas.clientWidth);
    canvasContainer.focus();
    updateMouse();
};

canvas.onmouseup = function (e) {
    buttonsPressed = e.buttons;
    e.preventDefault();
    updateMouse();
};

canvas.onmousedown = function (e) {
    buttonsPressed = e.buttons;
    e.preventDefault();
    updateMouse();
};

canvas.addEventListener("wheel", function (e) {
    if (e.deltaY > 0) {
        scrollDown = true;
    } else {
        scrollUp = true;
    }
    updateMouse();
    // Second update here is required due to the way VNC does scrolling
    // This is because VNC doesn't say how much to scroll (like JS does).
    // Instead it just says "scroll", and ignores the duration of the "scroll button being pressed"
    // So to continuously scroll, you must send "scroll, not scroll, scroll" etc.
    // Thus the weird double update
    updateMouse();
    e.preventDefault();
}, false);

canvas.oncontextmenu = function (e) {
    e.preventDefault();
}

canvasContainer.addEventListener("keydown", function (e) {
    updateKey(e, true);
    e.preventDefault();
}, false);

canvasContainer.addEventListener("keyup", function (e) {
    updateKey(e, false);
    e.preventDefault();
}, false);

function updateKey(keyEvent, pressed) {
    let key = keyEvent.key;

    let unicodeKey = 0;
    if (key.length == 1) {
        //Just an ordinary key. Convert to Unicode and send it in
        unicodeKey = key.charCodeAt(0);
    } else {
        //Perform lookup
        let x11Key = x11KeyMap[keyEvent.code];
        if (x11Key == undefined) {
            console.warn("Unsupported key", keyEvent);
            return;
        }
        unicodeKey = x11Key;
    }

    console.log("Sending key:", unicodeKey)

    let payload = [
        unicodeKey,
        pressed
    ];

    vncConnection.invoke("PerformKeyboardEvent", payload);
}

function updateMouse() {
    let payload = [
        mouseX,
        mouseY,
        (buttonsPressed & 1) != 0, //Left 
        (buttonsPressed & 2) != 0, //Right
        (buttonsPressed & 4) != 0, //Middle
        scrollDown,
        scrollUp
    ];

    scrollDown = false;
    scrollUp = false;
    vncConnection.invoke("PerformMouseEvent", payload);
}

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



startStreamingButton.disabled = true;
vncConnection.start().then(function () {
    console.log("VNC SignalR hub connected");
    startStreamingButton.disabled = false;
}).catch(function (err) {
    return console.error(err.toString());
});
fetchKeyMap();