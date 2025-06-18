namespace NoteTaker.Tests

open Expecto
open NoteTaker.Model

module ModelTests =

    let private testFilesLoadedWithoutMarkdown =
        testCase "FilesLoaded without markdown files should update files"
        <| fun _ ->
            let initialState : Model = {
                Config = Config.Default
                CurrentView = Capture
                Error = None
                Editor = EditorState.Default
                AppTheme = Theme.Presets.solarizedDark
                Files = []
                IsDirty = false
                ActiveMenu = None
                CurrentFolder = None
            }

            let nonMarkdownFiles = [ "config.json"; "app.exe"; "data.txt" ]
            let message = FilesLoaded nonMarkdownFiles

            let newState, _cmd = Handlers.update message initialState

            Expect.equal newState.Files nonMarkdownFiles "Files should be updated"

    let private testFilesLoadedWithMarkdown =
        testCase "FilesLoaded with markdown files should update files"
        <| fun _ ->
            let initialState = {
                Config = Config.Default
                CurrentView = Capture
                Error = None
                Editor = EditorState.Default
                AppTheme = Theme.Presets.solarizedDark
                IsDirty = false
                ActiveMenu = None
                Files = []
                CurrentFolder = None
            }

            let filesWithMarkdown = [ "config.json"; "existing.md"; "data.txt" ]
            let message = FilesLoaded filesWithMarkdown

            let newState, _cmd = Handlers.update message initialState

            Expect.equal newState.Files filesWithMarkdown "Files should be updated"

    [<Tests>]
    let readmeCreationTests =
        testList "File loading" [ testFilesLoadedWithoutMarkdown; testFilesLoadedWithMarkdown ]
