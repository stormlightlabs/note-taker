namespace NoteTaker

open System
open System.IO
open Elmish
open Thoth.Json.Net

type Error =
    | LoadFileError of string
    | DecoderError of string
    | SetupDirsError
    | LoadConfigError of string
    | ConfigDoesNotExistError
    | DirCreationError of (string * string)
    | Unexpected of string option

    member this.str : string = this.ToString()

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
                    Config.encode conf
                    |> fun data -> File.WriteAllText(configPath, data)
                    |> Ok
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

type Editor = {
    Text : string
    Caret : int
    Position : Position
} with

    static member Default : Editor = { Text = ""; Caret = 0; Position = (1, 1) }

and Position = int * int

module Editor =
    let computePosition (text : string) (index : int) : Position =
        let before = text.Substring(0, index)
        let lns = before.Split "\n"
        (lns.Length, index - before.LastIndexOf "\n")

/// State Updates
type Message =
    | SelectView of Section
    | ToggleScheme
    | TextChanged of string
    | CaretMoved of int
    | FileSystemChanged
    | LoadFiles
    | FilesLoaded of string list
    | SetError of Error
    | ClearError
    | ToggleMenuButton of MenuButton

and MenuButton =
    | FileButton
    | EditButton
    | HelpButton

/// Runtime Application State
type Model = {
    Config : Config
    CurrentView : Section
    Error : Error option
    Editor : Editor
    Files : string list
} with

    member this.lineNums =
        this.Editor.Text.Split "\n" |> Array.mapi (fun i _ -> string (i + 1))

    member this.rawContents = this.Editor.Text

    member this.caretI = this.Editor.Caret

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

type Renderer = Model -> (Message -> unit) -> Avalonia.FuncUI.Types.IView

type LRenderer = Model -> (Message -> unit) -> Avalonia.FuncUI.Types.IView list

/// TODO: Some of the private state update functions could go in a Message module
module Model =
    let private updatePosition state pos = { state with Model.Editor.Position = pos }

    /// TODO: consider making this withMessage
    let private withCommand (cmd : Cmd<Message> option) (state : Model) =
        match cmd with
        | Some c -> state, c
        | None -> state, Cmd.none

    let private toggleScheme state =
        state.Config.Scheme
        |> Scheme.toggle
        |> fun scheme -> { state with Model.Config.Scheme = scheme }

    /// Wraps loading of files in async task to make IO call non-blocking
    let private loadFilesTask =
        fun path -> async { return Directory.GetFiles path |> Array.toList }

    let private loadFilesCmd =
        Cmd.OfAsync.either
            loadFilesTask
            Store.FileSystem.getConfigDir
            FilesLoaded
            (fun exn -> SetError(Unexpected(Some exn.Message)))

    let private filesLoaded files state = { state with Files = files }
    let private selectView view state = { state with CurrentView = view }
    let private handleError (err : Error option) state = { state with Error = err }

    /// State mutation handlers
    let update (msg : Message) (state : Model) : Model * Cmd<Message> =
        match msg with
        | SelectView view -> state |> selectView view |> withCommand None
        | ToggleScheme -> state |> toggleScheme |> withCommand None
        | TextChanged contents ->
            Editor.computePosition contents state.Editor.Caret
            |> updatePosition state
            |> withCommand None
        | CaretMoved index ->
            Editor.computePosition state.Editor.Text index
            |> updatePosition state
            |> withCommand None
        | LoadFiles -> withCommand (Some loadFilesCmd) state
        | FilesLoaded files -> state |> filesLoaded files |> withCommand None
        | SetError err -> state |> handleError (Some err) |> withCommand None
        | ClearError -> state |> handleError None |> withCommand None
        | _ -> withCommand None state

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
            {
                Config = cfg
                CurrentView = Capture
                Error = None
                Editor = Editor.Default
                Files = []
            },
            Cmd.none
        | Error err ->
            Logger.error $"Error: {err.ToString()}"

            {
                Config = Config.Default
                CurrentView = Capture
                Error = Some err
                Editor = Editor.Default
                Files = []
            },
            Cmd.none
