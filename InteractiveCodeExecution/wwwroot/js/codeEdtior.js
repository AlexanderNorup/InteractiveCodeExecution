"use strict";
let editorFiles = {}; // For debug reachability
let editor = null;
require(['vs/editor/editor.main'], function () {
    const newFileBtn = document.getElementById("newFileBtn");
    const uploadFileBtn = document.getElementById("uploadFileBtn");
    const editorContainer = document.getElementById("editorContainer");
    const editorElement = document.getElementById('editor');

    const runBtn = document.getElementById("runBtn");
    const abortBtn = document.getElementById("abortBtn");
    const watchBtn = document.getElementById("startStreamingButton");

    const fileListElement = document.getElementById("fileList");
    let currentFileLoaded = null;

    editorFiles = {
        "Program.cs": new EditorFile("Program.cs", "Console.WriteLine(\"Hello World\");", "csharp"),
        "Project.csproj": new EditorFile("Project.csproj", `<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
`, "xml"),
        "script.py": new EditorFile("script.py", "print(\"Hello World\")", "python"),
    };



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
                    logMessage("["+ fileError.AffectedFile + "]" +  fileError.ErrorCode + ": " + fileError.ErrorMessage, "error");
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
        abortBtn.classList.add("d-none");
        abortStreaming();
    };

    runBtn.onclick = function () {
        abortBtn.classList.remove("d-none");
        runBtn.classList.add("d-none");
        startCodeExecution(editorFiles, abortExecution);
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