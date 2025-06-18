namespace NoteTaker.Model

open System
open System.IO
open Elmish
open Avalonia.Input
open TextMateSharp.Grammars
open Avalonia.Media
open Avalonia
open TextMateSharp.Registry
open Avalonia.Media.TextFormatting
open NoteTaker.Logger

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
    FontFamily : string
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
        FontFamily = "JetBrains Mono, Consolas, Courier New, monospace"
    }

/// TODO: WYSIWYG mode
and EditorMode =
    | Preview
    | Markdown
    | RichText

    static member List = [ Preview; Markdown ]

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


type Buffer = {
    Content : string
    FilePath : Option<string>
} with

    static member create : Buffer = { Content = ""; FilePath = None }

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
    // FileBrowser operations
    | OpenFile of string
    | OpenFolder of string
    | CreateBuffer
    | SetDirty
    | SetClean
    | SaveBuffer of Option<string>
    | FileOpened of string * string // filepath, content
    | NewFile
    | SaveFile
    | SaveFileAs of string
    | CreateFile of string * string // title, folder
    | CreateFolder of string * Option<string>
    // PlainText editor operations
    | LoadGrammar
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
    | SetSearchResults of (int * int) list
    | ToggleWrapping
    | ChangeEditorMode of EditorMode
    // Advanced text editing operations
    | InsertText of string * CaretPosition
    | DeleteText of CaretPosition * CaretPosition
    | ReplaceText of CaretPosition * CaretPosition * string
    | SetContent of string

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
    IsDirty : bool
    ActiveMenu : MenuButton option
    CurrentFolder : string option
} with

    member this.Scheme : Scheme = this.Config.Scheme

    member this.defaultFg : Color = this.Scheme.defaultTextColor


type Renderer = Model -> (Message -> unit) -> FuncUI.Types.IView

type LRenderer = Model -> (Message -> unit) -> FuncUI.Types.IView list
