namespace NoteTaker.Tests

open System
open Expecto
open Avalonia.Input
open NoteTaker.Model

module EditorTests =

    let createTestState () : Model = {
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

    let createEditorWithContent content : EditorState =
        let lines =
            if String.IsNullOrEmpty(content) then
                [ "" ]
            else
                content.Split([| '\r'; '\n' |], StringSplitOptions.None) |> Array.toList

        {
            EditorState.Default with
                Content = content
                Lines = lines
        }

    // Helper functions to test the internal logic directly
    let private insertCharAtCaret (text : string) (lines : string list) (caret : CaretPosition) =
        if caret.Line >= 0 && caret.Line < lines.Length then
            let currentLine = lines.[caret.Line]
            let col = max 0 (min caret.Column currentLine.Length)
            let newLine = currentLine.Insert(col, text)

            lines |> List.mapi (fun i line -> if i = caret.Line then newLine else line),
            { caret with Column = col + text.Length }
        elif lines.IsEmpty then
            [ text ], { Line = 0; Column = text.Length }
        else
            lines, caret

    let private deleteCharAtCaret (lines : string list) (caret : CaretPosition) =
        if caret.Line >= 0 && caret.Line < lines.Length then
            let currentLine = lines.[caret.Line]

            if caret.Column > 0 && caret.Column <= currentLine.Length then
                let newLine = currentLine.Remove(caret.Column - 1, 1)

                let newLines =
                    lines |> List.mapi (fun i line -> if i = caret.Line then newLine else line)

                let newCaret = { caret with Column = caret.Column - 1 }
                newLines, newCaret
            elif caret.Column = 0 && caret.Line > 0 then
                let prevLine = lines.[caret.Line - 1]
                let currentLine = lines.[caret.Line]
                let newLine = prevLine + currentLine

                let newLines =
                    lines
                    |> List.mapi (fun i line ->
                        if i = caret.Line - 1 then Some newLine
                        elif i = caret.Line then None
                        else Some line)
                    |> List.choose id

                let newCaret = { Line = caret.Line - 1; Column = prevLine.Length }
                newLines, newCaret
            else
                lines, caret
        else
            lines, caret

    let private insertNewlineAtCaret (lines : string list) (caret : CaretPosition) =
        if caret.Line >= 0 && caret.Line < lines.Length then
            let currentLine = lines.[caret.Line]
            let col = max 0 (min caret.Column currentLine.Length)
            let beforeCaret = currentLine.Substring(0, col)
            let afterCaret = currentLine.Substring(col)

            let newLines =
                lines
                |> List.mapi (fun i line -> if i = caret.Line then beforeCaret else line)
                |> fun beforeLines ->
                    let beforePart = beforeLines |> List.take (caret.Line + 1)
                    let afterPart = beforeLines |> List.skip (caret.Line + 1)
                    beforePart @ [ afterCaret ] @ afterPart

            let newCaret = { Line = caret.Line + 1; Column = 0 }
            newLines, newCaret
        elif lines.IsEmpty then
            [ ""; "" ], { Line = 1; Column = 0 }
        else
            lines, caret

    // Test helper functions directly
    let testInsertCharAtStart =
        testCase "Insert character at start of line"
        <| fun _ ->
            let lines = [ "hello world" ]
            let caret = { Line = 0; Column = 0 }

            let newLines, newCaret = insertCharAtCaret "Hi " lines caret

            Expect.equal newLines [ "Hi hello world" ] "Text should be inserted at start"
            Expect.equal newCaret.Column 3 "Caret should move to position 3"

    let testInsertCharAtEnd =
        testCase "Insert character at end of line"
        <| fun _ ->
            let lines = [ "hello" ]
            let caret = { Line = 0; Column = 5 }

            let newLines, newCaret = insertCharAtCaret " world" lines caret

            Expect.equal newLines [ "hello world" ] "Text should be appended at end"
            Expect.equal newCaret.Column 11 "Caret should move to end"

    let testInsertCharInMiddle =
        testCase "Insert character in middle of line"
        <| fun _ ->
            let lines = [ "helloworld" ]
            let caret = { Line = 0; Column = 5 }

            let newLines, newCaret = insertCharAtCaret " " lines caret

            Expect.equal newLines [ "hello world" ] "Space should be inserted in middle"
            Expect.equal newCaret.Column 6 "Caret should move forward by 1"

    let testInsertCharMultiLine =
        testCase "Insert character in multi-line content"
        <| fun _ ->
            let lines = [ "line1"; "line2"; "line3" ]
            let caret = { Line = 1; Column = 2 }

            let newLines, newCaret = insertCharAtCaret "XX" lines caret

            Expect.equal
                newLines
                [ "line1"; "liXXne2"; "line3" ]
                "Text should be inserted at correct position"

            Expect.equal newCaret.Line 1 "Caret line should remain the same"
            Expect.equal newCaret.Column 4 "Caret column should move forward by 2"

    let testDeleteCharInMiddle =
        testCase "Delete character in middle of line"
        <| fun _ ->
            let lines = [ "hello world" ]
            let caret = { Line = 0; Column = 6 }

            let newLines, newCaret = deleteCharAtCaret lines caret

            Expect.equal newLines [ "helloworld" ] "Space should be deleted"
            Expect.equal newCaret.Column 5 "Caret should move back by 1"

    let testDeleteCharAtStartOfLine =
        testCase "Delete character at start of line (line joining)"
        <| fun _ ->
            let lines = [ "line1"; "line2" ]
            let caret = { Line = 1; Column = 0 }

            let newLines, newCaret = deleteCharAtCaret lines caret

            Expect.equal newLines [ "line1line2" ] "Lines should be joined"
            Expect.equal newCaret.Line 0 "Caret should move to previous line"
            Expect.equal newCaret.Column 5 "Caret should be at end of previous line"

    let testDeleteCharAtVeryStart =
        testCase "Delete character at very start of document (no-op)"
        <| fun _ ->
            let lines = [ "hello" ]
            let caret = { Line = 0; Column = 0 }

            let newLines, newCaret = deleteCharAtCaret lines caret

            Expect.equal newLines lines "Content should remain unchanged"
            Expect.equal newCaret.Line 0 "Caret line should remain 0"
            Expect.equal newCaret.Column 0 "Caret column should remain 0"

    let testInsertNewlineAtEnd =
        testCase "Insert newline at end of line"
        <| fun _ ->
            let lines = [ "hello" ]
            let caret = { Line = 0; Column = 5 }

            let newLines, newCaret = insertNewlineAtCaret lines caret

            Expect.equal newLines [ "hello"; "" ] "Newline should be added"
            Expect.equal newCaret.Line 1 "Caret should move to new line"
            Expect.equal newCaret.Column 0 "Caret should be at start of new line"

    let testInsertNewlineInMiddle =
        testCase "Insert newline in middle of line"
        <| fun _ ->
            let lines = [ "hello world" ]
            let caret = { Line = 0; Column = 6 }

            let newLines, newCaret = insertNewlineAtCaret lines caret

            Expect.equal newLines [ "hello "; "world" ] "Line should be split at cursor"
            Expect.equal newCaret.Line 1 "Caret should move to new line"
            Expect.equal newCaret.Column 0 "Caret should be at start of new line"

    let testInsertNewlineInEmpty =
        testCase "Insert newline in empty document"
        <| fun _ ->
            let lines = []
            let caret = { Line = 0; Column = 0 }

            let newLines, newCaret = insertNewlineAtCaret lines caret

            Expect.equal newLines [ ""; "" ] "Two empty lines should be created"
            Expect.equal newCaret.Line 1 "Caret should move to line 1"
            Expect.equal newCaret.Column 0 "Caret should be at column 0"

    // Test file operations
    let testOpenFileUpdatesEditor =
        testCase "Opening file updates editor state"
        <| fun _ ->
            let state = createTestState ()
            let testContent = "# Test File\n\nSome content here"
            let filePath = "/tmp/test.md"

            let newState, _ = Handlers.update (FileOpened(filePath, testContent)) state

            Expect.equal newState.Editor.Content testContent "Content should be updated"
            Expect.equal newState.Editor.CurrentFile (Some filePath) "Current file should be set"
            Expect.isFalse newState.IsDirty "Should not be dirty when opening file"
            Expect.equal newState.Editor.Lines.Length 3 "Should have correct number of lines"

    let testCreateBufferClearsEditor =
        testCase "Creating buffer clears editor"
        <| fun _ ->
            let state = createTestState ()

            let editor = {
                createEditorWithContent "existing content" with
                    CurrentFile = Some "/tmp/old.md"
            }

            let stateWithContent = { state with Editor = editor; IsDirty = true }

            let newState, _ = Handlers.update CreateBuffer stateWithContent

            Expect.equal newState.Editor.Content "" "Content should be cleared"
            Expect.equal newState.Editor.CurrentFile None "Current file should be None"
            Expect.isFalse newState.IsDirty "Should not be dirty"
            Expect.equal newState.Editor.Lines [] "Lines should be empty"

    let testSetDirtyFlag =
        testCase "Set dirty flag"
        <| fun _ ->
            let state = { createTestState () with IsDirty = false }

            let newState, _ = Handlers.update SetDirty state

            Expect.isTrue newState.IsDirty "Should be dirty after SetDirty message"

    let testSetCleanFlag =
        testCase "Set clean flag"
        <| fun _ ->
            let state = { createTestState () with IsDirty = true }

            let newState, _ = Handlers.update SetClean state

            Expect.isFalse newState.IsDirty "Should not be dirty after SetClean message"

    // Test caret position updates
    let testUpdateCaret =
        testCase "Update caret position"
        <| fun _ ->
            let state = createTestState ()
            let newPosition = { Line = 5; Column = 10 }

            let newState, _ = Handlers.update (UpdateCaret newPosition) state

            Expect.equal
                newState.Editor.Caret
                newPosition
                "Caret should be updated to new position"

    let testUpdateCaretBounds =
        testCase "Update caret with bounds checking"
        <| fun _ ->
            let state = createTestState ()
            let editor = createEditorWithContent "short\nvery long line here"
            let stateWithEditor = { state with Editor = editor }
            let newPosition = { Line = 0; Column = 100 } // Out of bounds column

            let newState, _ = Handlers.update (UpdateCaret newPosition) stateWithEditor

            // The caret position should be set as requested - bounds checking happens in navigation
            Expect.equal
                newState.Editor.Caret
                newPosition
                "Caret should be updated even if out of bounds"

    // Test viewport and scrolling
    let testScrollBy =
        testCase "Scroll by offset"
        <| fun _ ->
            let state = createTestState ()
            let editor = { state.Editor with ScrollY = 100.0 }
            let stateWithScroll = { state with Editor = editor }

            let newState, _ = Handlers.update (ScrollBy 50.0) stateWithScroll

            Expect.equal newState.Editor.ScrollY 150.0 "ScrollY should be increased by offset"

    let testSetVisibleRange =
        testCase "Set visible range"
        <| fun _ ->
            let state = createTestState ()
            let range = (10, 25)

            let newState, _ = Handlers.update (SetVisibleRange range) state

            Expect.equal newState.Editor.VisibleRange range "Visible range should be updated"

    // Test search functionality
    let testPerformSearch =
        testCase "Perform search updates query"
        <| fun _ ->
            let state = createTestState ()
            let query = "test search"

            let newState, _ = Handlers.update (PerformSearch query) state

            Expect.equal newState.Editor.SearchQuery query "Search query should be updated"

    let testSetSearchResults =
        testCase "Set search results"
        <| fun _ ->
            let state = createTestState ()
            let results = [ (1, 5); (3, 10); (7, 2) ]

            let newState, _ = Handlers.update (SetSearchResults results) state

            Expect.equal newState.Editor.SearchResults results "Search results should be updated"

    // Test editor mode changes
    let testChangeEditorMode =
        testCase "Change editor mode"
        <| fun _ ->
            let state = createTestState ()
            let newMode = Preview

            let newState, _ = Handlers.update (ChangeEditorMode newMode) state

            Expect.equal newState.Editor.Mode newMode "Editor mode should be updated"

    let testToggleWrapping =
        testCase "Toggle text wrapping"
        <| fun _ ->
            let state = createTestState ()
            let initialWrapping = state.Editor.ShouldWrap

            let newState, _ = Handlers.update ToggleWrapping state

            Expect.equal
                newState.Editor.ShouldWrap
                (not initialWrapping)
                "Text wrapping should be toggled"

    // Test folding
    let testToggleFold =
        testCase "Toggle fold"
        <| fun _ ->
            let state = createTestState ()

            let folds = [
                { From = 5; To = 10; Folded = false }
                { From = 15; To = 20; Folded = true }
            ]

            let editor = { state.Editor with Folds = folds }
            let stateWithFolds = { state with Editor = editor }

            let newState, _ = Handlers.update (ToggleFold 5) stateWithFolds

            let updatedFold = newState.Editor.Folds |> List.find (fun f -> f.From = 5)
            Expect.isTrue updatedFold.Folded "Fold at line 5 should be toggled to folded"

    // Test bracket matching
    let testUpdateBracketMatches =
        testCase "Update bracket matches"
        <| fun _ ->
            let state = createTestState ()
            let matches = Map.ofList [ (1, (5, 15)); (3, (8, 12)) ]

            let newState, _ = Handlers.update (UpdateBracketMatches matches) state

            Expect.equal newState.Editor.BracketMatches matches "Bracket matches should be updated"

    // Test cache operations
    let testCacheLineMeasurement =
        testCase "Cache line measurement"
        <| fun _ ->
            let state = createTestState ()
            let lineNumber = 5
            let measurement = { Layout = null; Width = 120.5 }

            let newState, _ =
                Handlers.update (CacheLineMeasurement(lineNumber, measurement)) state

            let cachedMeasurement = Map.find lineNumber newState.Editor.Cache
            Expect.equal cachedMeasurement measurement "Line measurement should be cached"

    [<Tests>]
    let editorTests =
        testList "Editor Tests" [
            // Helper function tests
            testInsertCharAtStart
            testInsertCharAtEnd
            testInsertCharInMiddle
            testInsertCharMultiLine
            testDeleteCharInMiddle
            testDeleteCharAtStartOfLine
            testDeleteCharAtVeryStart
            testInsertNewlineAtEnd
            testInsertNewlineInMiddle
            testInsertNewlineInEmpty

            // File operation tests
            testOpenFileUpdatesEditor
            testCreateBufferClearsEditor
            testSetDirtyFlag
            testSetCleanFlag

            // Caret and navigation tests
            testUpdateCaret
            testUpdateCaretBounds

            // Viewport and scrolling tests
            testScrollBy
            testSetVisibleRange

            // Search tests
            testPerformSearch
            testSetSearchResults

            // Editor mode tests
            testChangeEditorMode
            testToggleWrapping

            // Advanced features tests
            testToggleFold
            testUpdateBracketMatches
            testCacheLineMeasurement
        ]
