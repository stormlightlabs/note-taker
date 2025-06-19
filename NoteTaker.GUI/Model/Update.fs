namespace NoteTaker.Model

open System
open System.IO
open Elmish
open Avalonia.Input
open TextMateSharp.Grammars
open TextMateSharp.Registry
open NoteTaker.Logger

/// Update Handlers
module Update =
    let noOp (state : Model) : Model * Cmd<Message> = state, Cmd.none

    let withCommand (cmd : Cmd<Message> option) (state : Model) =
        state,
        (match cmd with
         | Some c -> c
         | None -> Cmd.none)

    let selectView (view : Section) (state : Model) =
        { state with CurrentView = view } |> withCommand None

    let toggleScheme (state : Model) =
        state.Config.Scheme
        |> Scheme.toggle
        |> fun scheme -> {
            state with
                Config = { state.Config with Scheme = scheme }
        }
        |> withCommand None

    let toggleWrapping state =
        {
            state with
                Editor = {
                    state.Editor with
                        ShouldWrap = not state.Editor.ShouldWrap
                }
        }
        |> withCommand None

    let changeMode mode state =
        { state with Editor = { state.Editor with Mode = mode } } |> withCommand None

    /// Load files synchronously
    let private loadFilesSync path =
        try
            Directory.GetFiles path |> Array.toList
        with _ -> [] // Return empty list on error to avoid breaking the UI

    let loadFiles (store : Store) (state : Model) =
        state, loadFilesSync Store.FileSystem.getConfigDir |> FilesLoaded |> Cmd.ofMsg

    let private checkMDFiles (f : string) =
        try
            let ext = Path.GetExtension(f).ToLowerInvariant()
            ext = ".md" || ext = ".markdown"

        with _ ->
            f.EndsWith ".md" || f.EndsWith ".markdown"

    let filesLoaded (files : string list) (state : Model) =
        let updatedState = { state with Files = files |> List.filter checkMDFiles }

        if files |> List.exists checkMDFiles then
            match
                files
                |> List.tryFind (fun file ->
                    let fileName = Path.GetFileName(file).ToLowerInvariant()
                    fileName = "readme.md")
            with
            | Some readme -> updatedState, Cmd.ofMsg (OpenFile readme)
            | None -> updatedState, Cmd.none
        else
            // No markdown files found, create a sample README
            updatedState, Cmd.ofMsg CreateSampleReadme

    /// Handle errors by updating the state and then clears the error after 5s
    let handleError (err : Error option) state =
        { state with Error = err },
        Cmd.OfAsync.perform
            (fun _ -> async {
                do! Async.Sleep 5000
                return err
            })
            ()
            (fun _ -> ClearError)

    let toggleMenuButton (button : MenuButton) (state : Model) =
        {
            state with
                ActiveMenu =
                    match state.ActiveMenu with
                    | Some activeButton when activeButton = button -> None
                    | _ -> Some button
        },
        Cmd.none

    /// Create a sample README.md file when no markdown files exist
    let createSampleReadme path (state : Model) =
        try
            let filePath = Path.Combine(path, "README.md")
            File.WriteAllText(filePath, Data.MD.sample)
            LoadFiles // Reload files after creating README
        with ex ->
            SetError(Unexpected(Some ex.Message))
        |> Cmd.ofMsg
        |> fun c -> state, c

    let openFolder folderPath (state : Model) =
        try
            if Directory.Exists folderPath then
                { state with CurrentFolder = Some folderPath }, Cmd.ofMsg LoadFiles
            else
                state, Cmd.ofMsg (SetError(Unexpected(Some "Folder does not exist")))
        with ex ->
            state, Cmd.ofMsg (SetError(Unexpected(Some ex.Message)))

    let initGrammar (state : Model) = state, Cmd.ofMsg LoadGrammar

    let createBuffer (state : Model) =
        {
            state with
                Editor = {
                    state.Editor with
                        CurrentFile = None
                        Content = ""
                        Lines = []
                }
                IsDirty = false
        },
        Cmd.none

    let touchBuffer (dirty : bool) (state : Model) =
        { state with IsDirty = dirty }, Cmd.none

    let saveBuffer (filePath : Option<string>) (state : Model) =
        match filePath with
        | Some path ->
            try
                File.WriteAllText(path, state.Editor.Content)

                let updatedConfig = {
                    state.Config with
                        RecentFiles =
                            path
                            :: (state.Config.RecentFiles |> List.filter ((<>) path) |> List.take 9)
                }

                {
                    state with
                        Editor = { state.Editor with CurrentFile = Some path }
                        IsDirty = false
                        Config = updatedConfig
                },
                Cmd.ofMsg LoadFiles
            with ex ->
                state, Cmd.ofMsg (SetError(Unexpected(Some ex.Message)))
        | None ->
            // Need to prompt for save location
            state, Cmd.none

    let createFile (title : string) (folder : string) (state : Model) =
        try
            let note = Note.create title folder
            let filePath = Note.path note
            let dateStr = note.CreatedAt.ToString("yyyy-MM-dd HH:mm")
            let content = sprintf "# %s\n\nCreated: %s\n\n" title dateStr
            File.WriteAllText(filePath, content)

            {
                state with
                    Editor = {
                        state.Editor with
                            CurrentFile = Some filePath
                            Content = content
                            Lines =
                                content.Split([| '\r'; '\n' |], StringSplitOptions.None)
                                |> Array.toList
                    }
                    IsDirty = false
            },
            Cmd.ofMsg LoadFiles
        with ex ->
            state, Cmd.ofMsg (SetError(Unexpected(Some ex.Message)))

    let createFolder (folderName : string) (parentFolder : Option<string>) (state : Model) =
        try
            let basePath =
                parentFolder
                |> Option.defaultValue (
                    state.CurrentFolder |> Option.defaultValue Store.FileSystem.getConfigDir
                )

            let fullPath = Path.Combine(basePath, folderName)
            Directory.CreateDirectory(fullPath) |> ignore
            state, Cmd.ofMsg LoadFiles
        with ex ->
            state, Cmd.ofMsg (SetError(DirCreationError(folderName, ex.Message)))

    let openFile filePath (state : Model) =
        // Load file content synchronously
        try
            let content = File.ReadAllText(filePath)

            let lines =
                if String.IsNullOrEmpty(content) then
                    [ "" ] // Ensure at least one empty line for empty files
                else
                    content.Split([| '\r'; '\n' |], StringSplitOptions.None) |> Array.toList

            {
                state with
                    Editor = {
                        state.Editor with
                            CurrentFile = Some filePath
                            Content = content
                            Lines = lines
                            Cache = Map.empty // Clear cache when loading new content
                            Caret = { Line = 0; Column = 0 } // Reset caret position
                            Selection = None // Clear selection
                    }
                    IsDirty = false
            },
            Cmd.none
        with ex ->
            state, Cmd.ofMsg (SetError(LoadFileError ex.Message))

    let lines (contents : string) =
        match contents |> Seq.toList with
        | [] -> [ "" ]
        | _ -> contents.Split([| '\r'; '\n' |], StringSplitOptions.None) |> Array.toList

    let fileOpened (filePath : string) (content : string) (state : Model) =
        {
            state with
                Editor = {
                    state.Editor with
                        CurrentFile = Some filePath
                        Content = content
                        Lines = lines content
                        Cache = Map.empty
                        Caret = { Line = 0; Column = 0 }
                        Selection = None
                }
                IsDirty = false
        },
        Cmd.none

    let saveFile state =
        match state.Editor.CurrentFile with
        | Some filePath ->
            try
                File.WriteAllText(filePath, state.Editor.Content)
                { state with IsDirty = false }, Cmd.none
            with ex ->
                state, Cmd.ofMsg (SetError(Unexpected(Some ex.Message)))
        | None -> state, Cmd.none

    let saveFileAs newFilePath (state : Model) =
        try
            File.WriteAllText(newFilePath, state.Editor.Content)

            {
                state with
                    Editor = { state.Editor with CurrentFile = Some newFilePath }
                    IsDirty = false
            },
            Cmd.none
        with ex ->
            state, Cmd.ofMsg (SetError(Unexpected(Some ex.Message)))

    let loadGrammar (state : Model) =
        try
            let options = new RegistryOptions(ThemeName.DarkPlus)
            let tmScope = options.GetScopeByLanguageId "markdown"
            let registry = new Registry(options)

            {
                state with
                    Editor = {
                        state.Editor with
                            GrammarRegistry = registry
                            TmScope = Some tmScope
                    }
            }
            |> withCommand None
        with ex ->
            state, Cmd.ofMsg (SetError(Unexpected(Some $"Failed to load grammar: {ex.Message}")))

    let registerColorMap colors state =
        {
            state with
                Editor = { state.Editor with ScopeColorMap = Map.ofList colors }
        }
        |> withCommand None

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

    let handleKeyDown (args : KeyEventArgs) state =
        let caret = state.Editor.Caret
        let lines = state.Editor.Lines

        let newLines, newCaret =
            match args.Key with
            | Key.Left when caret.Column > 0 -> (lines, { caret with Column = caret.Column - 1 })
            | Key.Left when caret.Line > 0 ->
                let prevLineLength =
                    if caret.Line - 1 < lines.Length then
                        lines.[caret.Line - 1].Length
                    else
                        0

                lines, { Line = caret.Line - 1; Column = prevLineLength }
            | Key.Right when caret.Line < lines.Length && caret.Column < lines.[caret.Line].Length ->
                lines, { caret with Column = caret.Column + 1 }
            | Key.Right when caret.Line < lines.Length - 1 ->
                lines, { Line = caret.Line + 1; Column = 0 }
            | Key.Up when caret.Line > 0 ->
                let targetLine = caret.Line - 1

                let maxCol =
                    if targetLine < lines.Length then
                        lines.[targetLine].Length
                    else
                        0

                lines, { Line = targetLine; Column = min caret.Column maxCol }
            | Key.Down when caret.Line < lines.Length - 1 ->
                let targetLine = caret.Line + 1

                let maxCol =
                    if targetLine < lines.Length then
                        lines.[targetLine].Length
                    else
                        0

                (lines, { Line = targetLine; Column = min caret.Column maxCol })
            | Key.Home -> (lines, { caret with Column = 0 })
            | Key.End when caret.Line < lines.Length ->
                (lines, { caret with Column = lines.[caret.Line].Length })
            | Key.Back -> deleteCharAtCaret lines caret
            | Key.Delete when caret.Line < lines.Length && caret.Column < lines.[caret.Line].Length ->
                let currentLine = lines.[caret.Line]
                let newLine = currentLine.Remove(caret.Column, 1)

                let newLines =
                    lines |> List.mapi (fun i line -> if i = caret.Line then newLine else line)

                (newLines, caret)
            | Key.Enter -> insertNewlineAtCaret lines caret
            | _ -> (lines, caret)

        let newContent = String.concat "\n" newLines

        {
            state with
                Editor = {
                    state.Editor with
                        Lines = newLines
                        Content = newContent
                        Caret = newCaret
                        Cache = Map.empty // Clear cache when content changes
                }
                IsDirty = if newLines <> lines then true else state.IsDirty
        }
        |> withCommand None

    let handleTextInput (args : TextInputEventArgs) state =
        let text = args.Text

        if not (String.IsNullOrEmpty(text)) && text <> "\r" && text <> "\n" then
            let caret = state.Editor.Caret
            let lines = state.Editor.Lines

            let (newLines, newCaret) = insertCharAtCaret text lines caret
            let newContent = String.concat "\n" newLines

            {
                state with
                    Editor = {
                        state.Editor with
                            Content = newContent
                            Lines = newLines
                            Caret = newCaret
                            Cache = Map.empty // Clear cache when content changes
                    }
                    IsDirty = true
            }
            |> withCommand None
        else
            state |> withCommand None

    let handlePointerPressed (args : PointerPressedEventArgs) state =
        // Calculate approximate caret position from click
        let position = args.GetPosition(null)
        let lineIndex = int (position.Y / state.Editor.LineHeight)
        let columnIndex = max 0 (int ((position.X - 4.0) / 8.0)) // Approximate character width

        let clampedLine = max 0 (min lineIndex (state.Editor.Lines.Length - 1))

        let clampedColumn =
            if clampedLine < state.Editor.Lines.Length then
                max 0 (min columnIndex state.Editor.Lines.[clampedLine].Length)
            else
                0

        let newCaret = { Line = clampedLine; Column = clampedColumn }

        {
            state with
                Editor = {
                    state.Editor with
                        Caret = newCaret
                        Selection = None // Clear selection on click
                }
        }
        |> withCommand None

    let scrollBy offset state =
        {
            state with
                Editor = {
                    state.Editor with
                        ScrollY = state.Editor.ScrollY + offset
                }
        },
        Cmd.none

    let setVisibleRange (start, count) state =
        {
            state with
                Editor = { state.Editor with VisibleRange = (start, count) }
        }
        |> withCommand None

    let resizeViewport size state =
        {
            state with
                Editor = { state.Editor with ViewportSize = size }
        }
        |> withCommand None

    let updateCaret position state =
        {
            state with
                Editor = { state.Editor with Caret = position }
        }
        |> withCommand None

    let updateSelection selection state =
        {
            state with
                Editor = { state.Editor with Selection = selection }
        }
        |> withCommand None

    let cacheLineMeasurement lineNumber measurement state =
        {
            state with
                Editor = {
                    state.Editor with
                        Cache = Map.add lineNumber measurement state.Editor.Cache
                }
        }
        |> withCommand None

    let toggleFold lineNumber state =
        let folds =
            state.Editor.Folds
            |> List.map (fun fold ->
                if fold.From = lineNumber then
                    { fold with Folded = not fold.Folded }
                else
                    fold)

        { state with Editor = { state.Editor with Folds = folds } } |> withCommand None

    let updateBracketMatches matches state =
        {
            state with
                Editor = { state.Editor with BracketMatches = matches }
        }
        |> withCommand None

    let performSearch query state =
        {
            state with
                Editor = { state.Editor with SearchQuery = query }
        }
        |> withCommand None

    let setSearchResults results state =
        {
            state with
                Editor = { state.Editor with SearchResults = results }
        }
        |> withCommand None

module Commands =
    let setupWatcher = Cmd.ofEffect (fun dispatch -> Watcher.setup dispatch |> ignore)
    let loadFiles = Cmd.ofMsg LoadFiles
    let initGrammarCmd = Cmd.ofMsg LoadGrammar

module Handlers =
    /// Creates data directories on application initialization and returns full path to file
    let ensureDirs (baseDir : string) =
        Section.List
        |> List.map (fun view ->
            view.dirName
            |> fun name -> Path.Combine(baseDir, name) |> Logger.dbg "Path"
            |> Directory.CreateDirectory
            |> _.FullName
            |> Logger.dbg "Created Dir")

    let private store : Store = Store.FileSystem.getConfigDir |> Store.FileSystem.make

    /// State mutation handlers
    let update (msg : Message) (state : Model) : Model * Cmd<Message> =
        match msg with
        | SelectView view -> state |> Update.selectView view
        | ToggleScheme -> state |> Update.toggleScheme
        | ToggleWrapping -> state |> Update.toggleWrapping
        | ChangeEditorMode mode -> state |> Update.changeMode mode
        | FileSystemChanged
        | LoadFiles -> Update.loadFiles store state
        | FilesLoaded files -> state |> Update.filesLoaded files
        | SetError err -> state |> Update.handleError (Some err)
        | ClearError -> state |> Update.handleError None
        | ToggleMenuButton button -> state |> Update.toggleMenuButton button
        | CreateSampleReadme -> state |> Update.createSampleReadme Store.FileSystem.getConfigDir
        | OpenFolder folderPath -> state |> Update.openFolder folderPath
        | CreateBuffer -> state |> Update.createBuffer
        | SetDirty -> state |> Update.touchBuffer true
        | SetClean -> state |> Update.touchBuffer false
        | SaveBuffer filePath -> state |> Update.saveBuffer filePath
        | CreateFile(title, folder) -> state |> Update.createFile title folder
        | CreateFolder(name, parent) -> state |> Update.createFolder name parent
        | OpenFile filePath -> state |> Update.openFile filePath
        | FileOpened(path, content) -> state |> Update.fileOpened path content
        | NewFile -> state |> Update.createFile "New Note" Store.FileSystem.getConfigDir
        | SaveFile -> state |> Update.saveFile
        | SaveFileAs newFilePath -> state |> Update.saveFileAs newFilePath
        | LoadGrammar -> state |> Update.loadGrammar
        | RegisterColorMap colors -> state |> Update.registerColorMap colors
        | OnKeyDown args -> state |> Update.handleKeyDown args
        | OnTextInput args -> state |> Update.handleTextInput args
        | OnPointerPressed args -> state |> Update.handlePointerPressed args
        | ScrollBy offset -> state |> Update.scrollBy offset
        | SetVisibleRange(start, count) -> state |> Update.setVisibleRange (start, count)
        | ResizeViewport size -> state |> Update.resizeViewport size
        | UpdateCaret position -> state |> Update.updateCaret position
        | UpdateSelection selection -> state |> Update.updateSelection selection
        | CacheLineMeasurement(lN, meas) -> state |> Update.cacheLineMeasurement lN meas
        | ToggleFold lineNumber -> state |> Update.toggleFold lineNumber
        | UpdateBracketMatches matches -> state |> Update.updateBracketMatches matches
        | PerformSearch query -> state |> Update.performSearch query
        | SetSearchResults results -> state |> Update.setSearchResults results
        | InsertText(_text, _position) -> Update.withCommand None state
        | DeleteText(_start, _end) -> Update.withCommand None state
        | ReplaceText(_start, _end, _text) -> Update.withCommand None state
        | SetContent content -> Update.withCommand None state

    /// Initialize application state
    let init () : Model * Cmd<Message> =
        Logger.info "Initializing application state"

        ensureDirs Store.FileSystem.getConfigDir |> ignore

        match store.Load() with
        | Ok cfg ->
            let initialModel = {
                Config = cfg
                CurrentView = Capture
                Error = None
                Editor = EditorState.Default
                Files = []
                AppTheme = Theme.Presets.solarizedDark
                IsDirty = false
                ActiveMenu = None
                CurrentFolder = Some Store.FileSystem.getConfigDir
            }

            // Combine initialization commands:
            // 1. Set up file system watcher
            // 2. Load files initially
            // 3. Initialize TextMate grammar for syntax highlighting
            let initCommands = [
                Commands.setupWatcher
                Commands.loadFiles
                Commands.initGrammarCmd
            ]

            initialModel, Cmd.batch initCommands
        | Error err ->
            Logger.error $"Error: {err.ToString()}"

            let errorModel = {
                Config = Config.Default
                CurrentView = Capture
                Error = Some err
                Editor = EditorState.Default
                AppTheme = Theme.Presets.solarizedDark
                Files = []
                IsDirty = false
                ActiveMenu = None
                CurrentFolder = Some Store.FileSystem.getConfigDir
            }

            // Even on error, set up basic functionality
            errorModel, Commands.setupWatcher
