namespace NoteTaker

open System
open System.IO
open Elmish
open Thoth.Json.Net
open Avalonia.Input
open TextMateSharp.Grammars
open Avalonia.Media
open Avalonia
open TextMateSharp.Registry
open Avalonia.Media.TextFormatting

module Data =
    module MD =
        let sample =
            """# Markdown Syntax Guide

Markdown is a lightweight markup language for formatting text.
Here are some common elements:

## Headings

Use `#` for headings:

```
# Heading 1
## Heading 2
### Heading 3
```

## Emphasis

- \*Italic\*: `*italic*` or `_italic_`
- \*\*Bold\*\*: `**bold**` or `__bold__`

## Lists

**Unordered list:**
```
- Item 1
- Item 2
  - Subitem
```

**Ordered list:**
```
1. First
2. Second
```

## Links

```
[Link text](https://example.com)
```

## Images

```
![Alt text](https://example.com/image.png)
```

## Code

Inline code: `` `code` ``

Code block:
```
```python
print("Hello, world!")
```
```

## Blockquotes

```
> This is a quote.
```

## Horizontal Rule

```
---
```

---

Try writing your own markdown using these elements!
"""


type Error =
    | LoadFileError of string
    | DecoderError of string
    | SetupDirsError
    | LoadConfigError of string
    | ConfigDoesNotExistError
    | DirCreationError of (string * string)
    | Unexpected of string option

    member this.str : string = this.ToString()

/// Represents base16 theme
type Theme = {
    Name : string
    Author : string
    IsDark : bool
    /// Default Background
    Base00 : Color
    /// Lighter Background (Status bars, line numbers)
    Base01 : Color
    /// Selection Background
    Base02 : Color
    /// Comments, Invisibles, Line Highlighting
    Base03 : Color
    /// Dark Foreground (Status bars)
    Base04 : Color
    /// Default Foreground, Caret, Delimiters, Operators
    Base05 : Color
    /// Light Foreground (Not often used)
    Base06 : Color
    /// Light Background (Not often used)
    Base07 : Color
    /// Variables, XML Tags, Markup Link Text, Markup Lists, Diff Deleted
    Base08 : Color
    /// Integers, Boolean, Constants, XML Attributes, Markup Link Url
    Base09 : Color
    /// Classes, Markup Bold, Search Text Background
    Base0A : Color
    /// Strings, Inherited Class, Markup Code, Diff Inserted
    Base0B : Color
    /// Support, Regular Expressions, Escape Characters, Markup Quotes
    Base0C : Color
    /// Functions, Methods, Attribute IDs, Headings
    Base0D : Color
    /// Keywords, Storage, Selector, Markup Italic, Diff Changed
    Base0E : Color
    /// Deprecated, Opening/Closing Embedded Language Tags
    Base0F : Color
}

module Theme =
    module Presets =
        let solarizedDark : Theme = {
            Name = "Solarized Dark"
            Author = "Ethan Schoonover"
            IsDark = true
            Base00 = "#002b36" |> Color.Parse
            Base01 = "#073642" |> Color.Parse
            Base02 = "#586e75" |> Color.Parse
            Base03 = "#657b83" |> Color.Parse
            Base04 = "#839496" |> Color.Parse
            Base05 = "#93a1a1" |> Color.Parse
            Base06 = "#eee8d5" |> Color.Parse
            Base07 = "#fdf6e3" |> Color.Parse
            Base08 = "#dc322f" |> Color.Parse
            Base09 = "#cb4b16" |> Color.Parse
            Base0A = "#b58900" |> Color.Parse
            Base0B = "#859900" |> Color.Parse
            Base0C = "#2aa198" |> Color.Parse
            Base0D = "#268bd2" |> Color.Parse
            Base0E = "#6c71c4" |> Color.Parse
            Base0F = "#d33682" |> Color.Parse
        }

        let solarizedLight : Theme = {
            Name = "Solarized Light"
            Author = "Ethan Schoonover"
            IsDark = false
            Base00 = "#fdf6e3" |> Color.Parse
            Base01 = "#eee8d5" |> Color.Parse
            Base02 = "#93a1a1" |> Color.Parse
            Base03 = "#839496" |> Color.Parse
            Base04 = "#657b83" |> Color.Parse
            Base05 = "#586e75" |> Color.Parse
            Base06 = "#073642" |> Color.Parse
            Base07 = "#002b36" |> Color.Parse
            Base08 = "#dc322f" |> Color.Parse
            Base09 = "#cb4b16" |> Color.Parse
            Base0A = "#b58900" |> Color.Parse
            Base0B = "#859900" |> Color.Parse
            Base0C = "#2aa198" |> Color.Parse
            Base0D = "#268bd2" |> Color.Parse
            Base0E = "#6c71c4" |> Color.Parse
            Base0F = "#d33682" |> Color.Parse
        }

    let mapping theme =
        [
            // Document structure
            "text.html.markdown", theme.Base05
            "meta.frontmatter.markdown", theme.Base0F
            "meta.embedded.block.frontmatter", theme.Base0F

            // Headings
            "markup.heading.markdown", theme.Base0D
            "markup.heading.setext.1.markdown", theme.Base0D
            "markup.heading.setext.2.markdown", theme.Base0D
            "punctuation.definition.heading.markdown", theme.Base04
            "entity.name.section.markdown", theme.Base0D

            // Emphasis and formatting
            "markup.bold.markdown", theme.Base0B
            "markup.italic.markdown", theme.Base0E
            "markup.bold.italic.markdown", theme.Base0B
            "punctuation.definition.bold.markdown", theme.Base04
            "punctuation.definition.italic.markdown", theme.Base04
            "punctuation.definition.emphasis.markdown", theme.Base04

            // Quotes
            "markup.quote.markdown", theme.Base0C
            "punctuation.definition.quote.markdown", theme.Base03
            "beginning.punctuation.definition.quote.markdown", theme.Base03

            // Lists
            "markup.list.unnumbered.markdown", theme.Base0A
            "markup.list.numbered.markdown", theme.Base0A
            "beginning.punctuation.definition.list.markdown", theme.Base03
            "punctuation.definition.list.begin.markdown", theme.Base03

            // Links
            "markup.underline.link.markdown", theme.Base09
            "markup.underline.link.image.markdown", theme.Base08
            "meta.link.inline.markdown", theme.Base09
            "meta.link.reference.markdown", theme.Base09
            "meta.link.reference.def.markdown", theme.Base09
            "meta.link.reference.literal.markdown", theme.Base09
            "meta.link.email.lt-gt.markdown", theme.Base09
            "meta.link.inet.markdown", theme.Base09
            "string.other.link.title.markdown", theme.Base0B
            "string.other.link.description.markdown", theme.Base0B
            "string.other.link.description.title.markdown", theme.Base0B
            "punctuation.definition.link.markdown", theme.Base04
            "punctuation.definition.link.begin.markdown", theme.Base04
            "punctuation.definition.link.end.markdown", theme.Base04
            "punctuation.separator.key-value.markdown", theme.Base04
            "constant.other.reference.link.markdown", theme.Base08
            "punctuation.definition.constant.markdown", theme.Base04
            "punctuation.definition.constant.begin.markdown", theme.Base04
            "punctuation.definition.constant.end.markdown", theme.Base04

            // Images
            "meta.image.inline.markdown", theme.Base08
            "meta.image.reference.markdown", theme.Base08
            "markup.image.markdown", theme.Base08
            "string.other.image.title.markdown", theme.Base08

            // Code - Inline
            "markup.inline.raw.string.markdown", theme.Base0B
            "markup.inline.raw.markdown", theme.Base0B
            "punctuation.definition.raw.markdown", theme.Base04
            "punctuation.definition.raw.begin.markdown", theme.Base04
            "punctuation.definition.raw.end.markdown", theme.Base04

            // Code - Fenced blocks
            "markup.fenced_code.block.markdown", theme.Base0B
            "punctuation.definition.markdown", theme.Base03
            "fenced_code.block.language", theme.Base0E
            "fenced_code.block.language.attributes", theme.Base0E
            "fenced_code.block.marker.backtick.markdown", theme.Base03

            // Embedded code blocks (various languages)
            "meta.embedded.block.css", theme.Base0B
            "meta.embedded.block.html", theme.Base0B
            "meta.embedded.block.ini", theme.Base0B
            "meta.embedded.block.java", theme.Base0B
            "meta.embedded.block.lua", theme.Base0B
            "meta.embedded.block.makefile", theme.Base0B
            "meta.embedded.block.perl", theme.Base0B
            "meta.embedded.block.r", theme.Base0B
            "meta.embedded.block.ruby", theme.Base0B
            "meta.embedded.block.php", theme.Base0B
            "meta.embedded.block.sql", theme.Base0B
            "meta.embedded.block.vs_net", theme.Base0B
            "meta.embedded.block.xml", theme.Base0B
            "meta.embedded.block.xsl", theme.Base0B
            "meta.embedded.block.yaml", theme.Base0B
            "meta.embedded.block.dosbatch", theme.Base0B
            "meta.embedded.block.clojure", theme.Base0B
            "meta.embedded.block.coffee", theme.Base0B
            "meta.embedded.block.c", theme.Base0B
            "meta.embedded.block.cpp", theme.Base0B
            "meta.embedded.block.diff", theme.Base0B
            "meta.embedded.block.dockerfile", theme.Base0B
            "meta.embedded.block.git_commit", theme.Base0B
            "meta.embedded.block.git_rebase", theme.Base0B
            "meta.embedded.block.go", theme.Base0B
            "meta.embedded.block.groovy", theme.Base0B
            "meta.embedded.block.jade", theme.Base0B
            "meta.embedded.block.javascript", theme.Base0B
            "meta.embedded.block.js_regexp", theme.Base0B
            "meta.embedded.block.json", theme.Base0B
            "meta.embedded.block.less", theme.Base0B
            "meta.embedded.block.objc", theme.Base0B
            "meta.embedded.block.scss", theme.Base0B
            "meta.embedded.block.perl6", theme.Base0B
            "meta.embedded.block.powershell", theme.Base0B
            "meta.embedded.block.python", theme.Base0B
            "meta.embedded.block.regexp_python", theme.Base0B
            "meta.embedded.block.rust", theme.Base0B
            "meta.embedded.block.scala", theme.Base0B
            "meta.embedded.block.shellscript", theme.Base0B
            "meta.embedded.block.typescript", theme.Base0B
            "meta.embedded.block.typescriptreact", theme.Base0B
            "meta.embedded.block.csharp", theme.Base0B
            "meta.embedded.block.fsharp", theme.Base0B

            // Raw blocks
            "markup.raw.block.markdown", theme.Base0B

            // Separators
            "meta.separator.markdown", theme.Base03

            // HTML
            "comment.block.html", theme.Base03
            "punctuation.definition.comment.html", theme.Base03

            // Paragraphs
            "meta.paragraph.markdown", theme.Base05

            // String delimiters
            "punctuation.definition.string.begin.markdown", theme.Base04
            "punctuation.definition.string.end.markdown", theme.Base04
            "punctuation.definition.string.markdown", theme.Base04
            "punctuation.definition.metadata.markdown", theme.Base04

            // Math (if supported)
            "markup.math.inline.markdown", theme.Base0C
            "punctuation.definition.math.begin.markdown", theme.Base04
            "punctuation.definition.math.end.markdown", theme.Base04

            // Tables
            "markup.table.markdown", theme.Base0A
            "punctuation.separator.table.markdown", theme.Base03

            // Footnotes
            "markup.footnote.definition.markdown", theme.Base09
            "markup.footnote.reference.markdown", theme.Base09

            // Special characters
            "meta.other.valid-ampersand.markdown", theme.Base05
            "meta.other.valid-bracket.markdown", theme.Base05
            "constant.character.escape.markdown", theme.Base08

            // YAML frontmatter
            "meta.block.yaml.markdown", theme.Base0F
            "meta.separator.metadata.markdown", theme.Base0F

            // Default fallbacks
            "source", theme.Base05
            "text", theme.Base05
        ]
        |> Map.ofList

type Scheme =
    | Light
    | Dark

module Scheme =
    let toggle (s : Scheme) : Scheme =
        match s with
        | Light -> Dark
        | Dark -> Light

    let decodeString (x : string) : Scheme =
        match x.ToLower() with
        | "dark" -> Dark
        | "light"
        | _ -> Light

type Config = {
    Scheme : Scheme
    RecentFiles : List<string>
} with

    static member Default : Config = { Scheme = Light; RecentFiles = [] }


    static member Decoder : Decoder<Config> =
        Decode.object (fun get -> {
            Scheme = get.Required.Field "scheme" Decode.string |> Scheme.decodeString
            RecentFiles =
                get.Optional.Field "recent_files" (Decode.list Decode.string)
                |> Option.defaultValue []
        })

    static member Encoder(conf : Config) =
        Encode.object [
            "scheme", Encode.string <| conf.Scheme.ToString()
            "recent_files", Encode.list <| (conf.RecentFiles |> List.map Encode.string)
        ]

module Config =
    let decode (text : string) : Result<Config, Error> =
        match Decode.fromString Config.Decoder text with
        | Ok cfg -> Ok(cfg)
        | Error message -> Error(DecoderError message)

    let encode (conf : Config) =
        Config.Encoder conf |> Encode.toString 2

/// A pluggable persistence strategy
type Store = {
    Load : unit -> Result<Config, Error>
    Save : Config -> Result<unit, Error>
}

/// Implementations of Store Record-of-Functions
module Store =
    module FileSystem =
        let getConfigDir : string =
            match Environment.OSVersion.Platform with
            | PlatformID.Unix
            | PlatformID.MacOSX ->
                Environment.GetEnvironmentVariable "HOME"
                |> fun h -> h :: [ ".config" ]
                |> List.toArray
                |> Path.Combine
            | _ -> Environment.GetFolderPath Environment.SpecialFolder.ApplicationData
            |> fun baseDir -> Path.Combine(baseDir, "note_taker")

        let make (baseDir : string) : Store =
            let configPath = Path.Combine(baseDir, "config.json")

            let saveConfig (conf : Config) : Result<unit, Error> =
                try
                    Config.encode conf |> (fun data -> File.WriteAllText(configPath, data)) |> Ok
                with err ->
                    DecoderError err.Message |> Error

            let loadConfig' =
                try
                    File.ReadAllText configPath |> Config.decode
                with e ->
                    Error(LoadFileError e.Message)

            let createConfig =
                Config.Default
                |> fun conf ->
                    match saveConfig conf with
                    | Ok _ -> Ok conf
                    | Error err -> Error err

            let loadConfig () : Result<Config, Error> =
                if File.Exists configPath then loadConfig' else createConfig

            { Save = saveConfig; Load = loadConfig }

    module TestFS =
        let make (initial : Config) =
            let cell = ref initial
            let load () = Ok(cell.Value)

            let save cfg =
                cell.Value <- cfg
                Ok()

            { Load = load; Save = save }

type Section =
    | Inbox
    | Capture
    | Next
    | Projects

    member this.label : string = this.ToString()

    member this.dirName : string =
        match this with
        | Inbox -> "inbox"
        | Capture -> "capture"
        | Next -> "tasks"
        | Projects -> "projects"

    static member List : List<Section> = [ Inbox; Capture; Next; Projects ]

type Note = {
    Id : Guid
    CreatedAt : DateTime
    UpdatedAt : DateTime
    Title : string
    Folder : string
    Filename : string
}

module Note =
    let private invalid = Path.GetInvalidFileNameChars()

    let private clean (t : string) =
        t.Trim() |> _.ToLowerInvariant() |> _.Replace(' ', '-') |> _.Replace(':', '-')

    let private newGuid = Guid.NewGuid()

    let private slugify (title : string) =
        clean title
        |> Seq.filter (fun ch -> not (invalid |> Array.contains ch))
        |> Seq.toArray
        |> String
        |> fun s -> if s.EndsWith ".md" then s else $"{s}.md"

    let create title dir : Note =
        DateTime.UtcNow
        |> fun now -> {
            Id = newGuid
            Title = title
            Folder = dir
            Filename = slugify title
            CreatedAt = now
            UpdatedAt = now
        }

    /// Get the absolute path for a note file
    let path (note : Note) =
        Path.Combine [| note.Folder; note.Filename |]

    /// Daily journal path
    let dailyPath (dir : string) (today : DateTime) =
        today.ToString "yyyy-MM-dd" |> fun name -> Path.Combine(dir, $"{name}.md")

type EditorState = {
    Content : string
    Lines : string list
    ShouldWrap : bool
    CurrentFile : string option // Track the currently open file path
    GrammarRegistry : Registry
    TmScope : string option
    ScopeColorMap : Map<string, Color>
    Caret : CaretPosition
    Selection : SelectionRange option
    VisibleRange : (int * int)
    ScrollY : float
    Cache : Map<int, LineMeasurement>
    /// Line No.: open, close
    BracketMatches : Map<int, (int * int)>
    /// Line, Column hits
    SearchQuery : string
    SearchResults : (int * int) List
    ViewportSize : Size
    Folds : FoldRange list
    LineHeight : float
    FontSize : float
    Mode : EditorMode
} with

    static member Default : EditorState = {
        Content = ""
        Lines = []
        CurrentFile = None // No file open initially
        GrammarRegistry = null
        TmScope = None
        ScopeColorMap = Theme.Presets.solarizedDark |> Theme.mapping
        Caret = { Line = 0; Column = 0 }
        Selection = None
        VisibleRange = (0, 0)
        ScrollY = 0.0
        Cache = Map.empty
        BracketMatches = Map.empty
        SearchQuery = ""
        SearchResults = []
        ViewportSize = Size(0.0, 0.0)
        Folds = []
        LineHeight = 16.0
        FontSize = 14.0
        ShouldWrap = true
        Mode = Markdown
    }

and EditorMode =
    | WYSIWYG
    | PlainText
    | Markdown

and CaretPosition = {
    Line : int
    Column : int
} with

    static member fromPair line col : CaretPosition = { Line = line; Column = col }

and SelectionRange = { Start : CaretPosition; End : CaretPosition }

and LineMeasurement = {
    Layout : TextLayout
    Width : float
} with

    static member from f : LineMeasurement = { Layout = f; Width = f.Width }

and FoldRange = { From : int; To : int; Folded : bool }

module Editor =
    let computePosition (text : string) (index : int) : CaretPosition =
        text.Substring(0, index)
        |> fun before ->
            before.Split "\n"
            |> _.Length
            |> CaretPosition.fromPair (index - before.LastIndexOf "\n")



/// State Updates
type Message =
    | SelectView of Section
    | ToggleScheme
    | FileSystemChanged
    | LoadFiles
    | FilesLoaded of string list
    | SetError of Error
    | ClearError
    | ToggleMenuButton of MenuButton
    | CreateSampleReadme
    // File Operations
    | OpenFile of string
    | FileOpened of string * string // filepath, content
    | NewFile
    | SaveFile
    | SaveFileAs of string
    // Editor Specific
    | LoadGrammar of string // Just pass the grammar file path
    | RegisterColorMap of (string * Color) list
    | OnKeyDown of KeyEventArgs
    | OnTextInput of TextInputEventArgs
    | OnPointerPressed of PointerPressedEventArgs
    | ScrollBy of float
    | SetVisibleRange of (int * int)
    | ResizeViewport of Size
    | UpdateCaret of CaretPosition
    | UpdateSelection of SelectionRange option
    | CacheLineMeasurement of int * LineMeasurement
    | ToggleFold of int
    | UpdateBracketMatches of Map<int, (int * int)>
    | PerformSearch of string
    | SetSearchReults of (int * int) list
    | ToggleWrapping
    | ChangeEditorMode of EditorMode

and MenuButton =
    | FileButton
    | EditButton
    | HelpButton

/// Runtime Application State
type Model = {
    Config : Config
    CurrentView : Section
    Error : Error option
    Editor : EditorState
    Files : string list
    AppTheme : Theme

}

module Watcher =
    open Store.FileSystem

    let private handler dispatch = fun _ -> dispatch FileSystemChanged

    let private initWatcher dispatch (watcher : FileSystemWatcher) =
        watcher.IncludeSubdirectories <- true
        watcher.EnableRaisingEvents <- true
        watcher.NotifyFilter <- NotifyFilters.FileName ||| NotifyFilters.LastWrite
        watcher.Created.Add(handler dispatch)
        watcher.Changed.Add(handler dispatch)
        watcher.Renamed.Add(handler dispatch)
        watcher.Deleted.Add(handler dispatch)

        watcher

    let setup (dispatch : Message -> unit) =
        new FileSystemWatcher(getConfigDir) |> initWatcher dispatch

    let command = Cmd.ofEffect (fun dispatch -> (setup dispatch) |> ignore)

type Renderer = Model -> (Message -> unit) -> FuncUI.Types.IView

type LRenderer = Model -> (Message -> unit) -> FuncUI.Types.IView list

module Model =
    let private withCommand (cmd : Cmd<Message> option) (state : Model) =
        match cmd with
        | Some c -> state, c
        | None -> state, Cmd.none

    let private toggleScheme state =
        state.Config.Scheme
        |> Scheme.toggle
        |> fun scheme -> { state with Model.Config.Scheme = scheme }

    /// Load files synchronously
    let private loadFilesSync path =
        try
            Directory.GetFiles path |> Array.toList
        with _ -> [] // Return empty list on error to avoid breaking the UI

    let private loadFilesCmd =
        Cmd.ofMsg (FilesLoaded(loadFilesSync Store.FileSystem.getConfigDir))

    /// Create a sample README.md file when no markdown files exist
    let private createSampleReadme path =
        try
            let filePath = Path.Combine(path, "README.md")
            File.WriteAllText(filePath, Data.MD.sample)
            LoadFiles // Reload files after creating README
        with ex ->
            SetError(Unexpected(Some ex.Message))

    let private createSampleReadmeCmd =
        Cmd.ofMsg (createSampleReadme Store.FileSystem.getConfigDir)

    /// Initialize TextMate grammar for syntax highlighting
    let private initGrammarCmd =
        let grammarPath =
            Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Assets",
                "markdown.tmLanguage.json"
            )

        Cmd.ofMsg (LoadGrammar grammarPath)

    let private filesLoaded files state =
        let updatedState = {
            state with
                Files =
                    (files |> List.filter (fun f -> f.EndsWith ".md" || f.EndsWith ".markdown"))
        }
        // Check if there are any markdown files
        let hasMarkdownFiles =
            files
            |> List.exists (fun file ->
                let ext = Path.GetExtension(file).ToLowerInvariant()
                ext = ".md" || ext = ".markdown")

        if hasMarkdownFiles then
            // Look for README.md and auto-open it
            let readmeFile =
                files
                |> List.tryFind (fun file ->
                    let fileName = Path.GetFileName(file).ToLowerInvariant()
                    fileName = "readme.md")

            match readmeFile with
            | Some readme -> updatedState, Some(Cmd.ofMsg (OpenFile readme))
            | None -> updatedState, None
        else
            // No markdown files found, create a sample README
            updatedState, Some createSampleReadmeCmd

    let private selectView view state = { state with CurrentView = view }
    let private handleError (err : Error option) state = { state with Error = err }

    /// State mutation handlers
    let update (msg : Message) (state : Model) : Model * Cmd<Message> =
        match msg with
        | SelectView view -> state |> selectView view |> withCommand None
        | ToggleScheme -> state |> toggleScheme |> withCommand None
        | ToggleWrapping ->
            {
                state with
                    Editor = {
                        state.Editor with
                            ShouldWrap = not state.Editor.ShouldWrap
                    }
            }
            |> withCommand None
        | ChangeEditorMode mode ->
            { state with Editor = { state.Editor with Mode = mode } } |> withCommand None
        | FileSystemChanged -> withCommand (Some loadFilesCmd) state
        | LoadFiles -> withCommand (Some loadFilesCmd) state
        | FilesLoaded files ->
            let newState, cmdOpt = filesLoaded files state
            withCommand cmdOpt newState
        | SetError err -> state |> handleError (Some err) |> withCommand None
        | ClearError -> state |> handleError None |> withCommand None
        | ToggleMenuButton button ->
            // Note: ActiveMenu field doesn't exist in EditorState, this might need to be added to the model
            withCommand None state
        | CreateSampleReadme -> withCommand (Some createSampleReadmeCmd) state
        // File Operations
        | OpenFile filePath ->
            // Load file content synchronously
            try
                let content = File.ReadAllText(filePath)

                let lines =
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
                },
                Cmd.none
            with ex ->
                state, Cmd.ofMsg (SetError(LoadFileError ex.Message))

        | FileOpened(filePath, content) ->
            // This message is now unused since we handle everything in OpenFile
            let lines = content.Split([| '\r'; '\n' |], StringSplitOptions.None) |> Array.toList

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
            }
            |> withCommand None
        | NewFile ->
            {
                state with
                    Editor = { state.Editor with CurrentFile = None; Content = "" }
            }
            |> withCommand None
        | SaveFile ->
            match state.Editor.CurrentFile with
            | Some filePath ->
                try
                    File.WriteAllText(filePath, state.Editor.Content)
                    state, Cmd.none
                with ex ->
                    state, Cmd.ofMsg (SetError(Unexpected(Some ex.Message)))
            | None ->
                // No file is currently open, cannot save
                state, Cmd.none
        | SaveFileAs newFilePath ->
            try
                File.WriteAllText(newFilePath, state.Editor.Content)
                // Update the model to reflect the new file
                {
                    state with
                        Editor = { state.Editor with CurrentFile = Some newFilePath }
                },
                Cmd.none
            with ex ->
                state, Cmd.ofMsg (SetError(Unexpected(Some ex.Message)))
        // Editor Specific Updates
        | LoadGrammar grammarPath ->
            // Create a simple registry that can load the markdown grammar
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
                // If grammar loading fails, continue without syntax highlighting
                state,
                Cmd.ofMsg (SetError(Unexpected(Some $"Failed to load grammar: {ex.Message}")))
        | RegisterColorMap colors ->
            {
                state with
                    Editor = { state.Editor with ScopeColorMap = Map.ofList colors }
            }
            |> withCommand None
        | OnKeyDown args ->
            // Handle key down events - could be used for shortcuts, navigation, etc.
            withCommand None state
        | OnTextInput args ->
            // Handle text input events - would update the document content
            withCommand None state
        | OnPointerPressed args ->
            // Handle mouse/pointer events - could update caret position or selection
            withCommand None state
        | ScrollBy offset ->
            {
                state with
                    Editor = {
                        state.Editor with
                            ScrollY = state.Editor.ScrollY + offset
                    }
            }
            |> withCommand None
        | SetVisibleRange(start, count) ->
            {
                state with
                    Editor = { state.Editor with VisibleRange = (start, count) }
            }
            |> withCommand None
        | ResizeViewport size ->
            {
                state with
                    Editor = { state.Editor with ViewportSize = size }
            }
            |> withCommand None
        | UpdateCaret position ->
            {
                state with
                    Editor = { state.Editor with Caret = position }
            }
            |> withCommand None
        | UpdateSelection selection ->
            {
                state with
                    Editor = { state.Editor with Selection = selection }
            }
            |> withCommand None
        | CacheLineMeasurement(lineNumber, measurement) ->
            {
                state with
                    Editor = {
                        state.Editor with
                            Cache = Map.add lineNumber measurement state.Editor.Cache
                    }
            }
            |> withCommand None
        | ToggleFold lineNumber ->
            let folds =
                state.Editor.Folds
                |> List.map (fun fold ->
                    if fold.From = lineNumber then
                        { fold with Folded = not fold.Folded }
                    else
                        fold)

            { state with Editor = { state.Editor with Folds = folds } } |> withCommand None
        | UpdateBracketMatches matches ->
            {
                state with
                    Editor = { state.Editor with BracketMatches = matches }
            }
            |> withCommand None
        | PerformSearch query ->
            {
                state with
                    Editor = { state.Editor with SearchQuery = query }
            }
            |> withCommand None
        | SetSearchReults results ->
            {
                state with
                    Editor = { state.Editor with SearchResults = results }
            }
            |> withCommand None

    let private store : Store = Store.FileSystem.getConfigDir |> Store.FileSystem.make

    /// Creates data directories on application initialization and returns full path to file
    let ensureDirs (baseDir : string) =
        Section.List
        |> List.map (fun view ->
            view.dirName
            |> fun name -> Path.Combine(baseDir, name) |> Logger.dbg "Path"
            |> Directory.CreateDirectory
            |> _.FullName
            |> Logger.dbg "Created Dir")

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
            }

            // Combine initialization commands:
            // 1. Set up file system watcher
            // 2. Load files initially
            // 3. Initialize TextMate grammar for syntax highlighting
            let initCommands = [ Watcher.command; loadFilesCmd; initGrammarCmd ]

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
            }

            // Even on error, set up basic functionality
            errorModel, Watcher.command
