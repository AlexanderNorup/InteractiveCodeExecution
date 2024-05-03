"use strict";
const AssignmentApiUrl = "/api/assignments";
let editorFiles = {}; // For debug reachability
let editor = null;
require(['vs/editor/editor.main'], function () {
    const startAssignmentBtn = document.getElementById("startAssignmentBtn");
    const assignmentSelector = document.getElementById("assignmentSelector");

    const newFileBtn = document.getElementById("newFileBtn");
    const uploadFileBtn = document.getElementById("uploadFileBtn");
    const editorContainer = document.getElementById("editorContainer");
    const editorElement = document.getElementById('editor');

    const runBtn = document.getElementById("runBtn");
    const buildBtn = document.getElementById("buildBtn");
    const abortBtn = document.getElementById("abortBtn");
    const watchBtn = document.getElementById("startStreamingButton");

    const submitCodeBtn = document.getElementById("submitCodeBtn");

    const fileListElement = document.getElementById("fileList");
    let currentFileLoaded = null;

    let currentAssignment = null;

    editorFiles = {};

    const initEditor = function () {
        editor = monaco.editor.create(editorElement, {
            model: null
        });
        editor.layout();

        // Load a file into the editor by default (so it isn't blank)
        let firstFile = Object.entries(editorFiles)[0];
        if (firstFile !== undefined) {
            setFileView(firstFile[1]);
        }

        // So we can recieve source errors from the backend
        registerSourceErrorHandler(handleSourceErrors);

        // no point in keeping this around.
        window.removeEventListener("load", initEditor);
    };

    function handleSourceErrors(errors) {
        console.log("Got source errors", errors);
        const groupedByFile = Object.groupBy(errors, ({ AffectedFile }) => AffectedFile);

        for (let errorGroup of Object.entries(groupedByFile)) {
            let err = errorGroup[0];
            let file = editorFiles[err];
            if (file === undefined) {
                continue;
            }

            let markersForThisFile = [];
            for (let fileError of errorGroup[1]) {
                if (fileError.Line === null) {
                    // Generic error. We don't have the line-number for this one..
                    logMessage("[" + fileError.AffectedFile + "]" + fileError.ErrorCode + ": " + fileError.ErrorMessage, "error");
                    continue;
                }
                markersForThisFile.push({
                    message: fileError.ErrorCode + ": " + fileError.ErrorMessage,
                    severity: fileError.Type == "warning" ? monaco.MarkerSeverity.Warning : monaco.MarkerSeverity.Error,
                    startLineNumber: fileError.Line,
                    endLineNumber: fileError.Line,
                    startColumn: fileError.Column,
                });
            }
            monaco.editor.setModelMarkers(file.monacoModel, "owner", markersForThisFile);
        }
    }

    function clearSourceMakers() {
        for (let file of Object.entries(editorFiles)) {
            monaco.editor.setModelMarkers(file[1].monacoModel, "owner", []);
        }
    }

    function redrawFileList() {
        fileListElement.innerHTML = "";
        for (let file of Object.entries(editorFiles)) {
            let elem = document.createElement("li");
            elem.setAttribute("file", file[1].filename)
            if (file[1].filename === currentFileLoaded?.filename) {
                elem.classList.add("active");
            }
            elem.innerText = file[0];

            fileListElement.appendChild(elem);
        }
    }

    function setFileView(file) {
        if (currentFileLoaded !== null) {
            currentFileLoaded.saveViewState(editor);
        }

        file.setAsActiveModel(editor);
        currentFileLoaded = file;
        redrawFileList();
    }

    async function setBackingAssignment(newAssignmentId) {
        try {
            const newAssignmentResp = await fetch(AssignmentApiUrl + "/" + newAssignmentId);
            const newAssignmentJson = await newAssignmentResp.json();

            console.info("Starting new assignment", newAssignmentJson);
            editorFiles = {}; // Clear current editor!
            if (newAssignmentJson.InitialPayload) {
                for (let initialFile of newAssignmentJson.InitialPayload) {
                    editorFiles[initialFile.Filepath] = new EditorFile(initialFile.Filepath, initialFile.Content);
                }
            }

            currentAssignment = newAssignmentJson;
            redrawFileList();
        } catch (e) {
            console.error(e);
            logMessage("Failed to start assignment! Invalid response from backend!", "error");
        }
    }

    startAssignmentBtn.addEventListener("click", function () {
        setBackingAssignment(assignmentSelector.value);
    });

    submitCodeBtn.addEventListener("click", function () {
        submitAssignment(currentAssignment, editorFiles);
    });

    fileListElement.addEventListener("click", function (e) {
        if (editor === null) {
            console.warn("Editor is still null");
            return;
        }

        let target = e.target;
        let fileTarget = target.getAttribute("file");

        if (fileTarget === null) {
            return null;
        }

        let targetEditorFile = editorFiles[fileTarget];
        if (targetEditorFile === undefined) {
            return null;
        }

        setFileView(targetEditorFile);
    });

    newFileBtn.onclick = function () {
        let newName = prompt("New filename (including extension):"); //TODO make a prettier prompt
        if (newName) {
            editorFiles[newName] = new EditorFile(newName, "", "csharp");
        }
        redrawFileList();
    };

    uploadFileBtn.onclick = function () {
        alert("Not implemented yet, sorry :(");
        redrawFileList();
    };

    const abortExecution = function () {
        runBtn.classList.remove("d-none");
        buildBtn.classList.remove("d-none");
        abortBtn.classList.add("d-none");
        abortStreaming();
    };

    function runOrBuild(build = false) {
        if (currentAssignment === null) {
            logMessage("Select an assignment in order to run code!", "error");
            return;
        }

        abortBtn.classList.remove("d-none");
        runBtn.classList.add("d-none");
        buildBtn.classList.add("d-none");
        clearSourceMakers();
        startCodeExecution(currentAssignment.AssignmentId, editorFiles, build, abortExecution);
    }

    buildBtn.onclick = function () {
        runOrBuild(true);
    };

    runBtn.onclick = function () {
        runOrBuild(false);
    };

    abortBtn.onclick = abortExecution;

    window.addEventListener("resize", function () {
        if (editor !== null) {
            editor.layout();
        }
    });
    document.addEventListener('keydown', e => {
        if (e.ctrlKey && e.key === 's') {
            // Prevent the Save dialog to open on CTRL+S
            e.preventDefault();
            console.log('CTRL + S');
        }
    });
    window.addEventListener("load", initEditor);
    redrawFileList();
});