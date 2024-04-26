// The implementation of this file is inspired by how the Playground on https://microsoft.github.io/monaco-editor works.
// Source: https://github.com/Microsoft/monaco-editor/blob/bad3c34056624dca34ac8be5028ae3454172125c/website/playground/playground.js#L108

class EditorFile {
    constructor(filename, contents = "", editorType = null) {
        this.filename = filename;
        this.contents = contents;
        this.editorType = editorType;

        this.monacoViewState = null; // Used to store scroll-position, cursor data etc.
        this.monacoModel = monaco.editor.createModel(this.contents, this.editorType)
    }

    saveViewState(editor) {
        this.monacoViewState = editor.saveViewState();
    }

    setAsActiveModel(editor) {
        editor.setModel(this.monacoModel);
        if (this.monacoViewState !== null) {
            editor.restoreViewState(this.monacoViewState);
        }
        editor.focus();
    }

    getCurrentContent() {
        return this.monacoModel.getValue();
    }

}