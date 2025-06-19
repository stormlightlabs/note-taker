namespace NoteTaker.Tests

open Expecto
open NoteTaker.Model

module ModelTests =

    let private testFilesLoadedWithoutMarkdown =
        testCase "FilesLoaded without markdown files should filter out non-markdown files"
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

            Expect.equal
                newState.Files
                []
                "Only markdown files should be kept, so result should be empty"

    let private testFilesLoadedWithMarkdown =
        testCase "FilesLoaded with markdown files should keep only markdown files"
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

            let filesWithMarkdown = [ "existing.md" ]
            let message = FilesLoaded filesWithMarkdown

            let newState, _cmd = Handlers.update message initialState

            Expect.equal newState.Files [ "existing.md" ] "Only markdown files should be kept"

    [<Tests>]
    let readmeCreationTests =
        testList "File loading" [ testFilesLoadedWithoutMarkdown; testFilesLoadedWithMarkdown ]
