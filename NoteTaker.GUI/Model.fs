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
    | Unexpected

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
                |> fun h -> h :: [ ".config" ] |> List.toArray |> Path.Combine
            | _ -> Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
            |> fun baseDir -> Path.Combine(baseDir, "note_taker")

        let make (baseDir : string) : Store =
            let configPath = Path.Combine(baseDir, "config.json")

            let saveConfig (conf : Config) : Result<unit, Error> =
                try
                    Config.encode conf
                    |> (fun data -> File.WriteAllText(configPath, data))
                    |> Ok
                with err ->
                    DecoderError err.Message |> Error

            let loadConfig () : Result<Config, Error> =
                if File.Exists configPath then
                    try
                        File.ReadAllText configPath |> Config.decode
                    with e ->
                        Error(LoadFileError e.Message)
                else
                    Error ConfigDoesNotExistError

            { Save = saveConfig; Load = loadConfig }

    module TestFS =
        let make (initial : Config) =
            let cell = ref initial
            let load () = Ok(cell.Value)

            let save cfg =
                cell.Value <- cfg
                Ok()

            { Load = load; Save = save }

type Views =
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

    static member List : List<Views> = [ Inbox; Capture; Next; Projects ]

/// Application State
type Model = {
    Config : Config
    CurrentView : Views
    Error : Error option
}

/// State Updates
type Message =
    | SelectView of Views
    | ToggleScheme

module Model =
    /// State mutation handlers
    let update (msg : Message) (state : Model) : Model * Cmd<Message> =
        match msg with
        | SelectView view -> { state with CurrentView = view }, Cmd.none
        | ToggleScheme ->
            {
                state with
                    Model.Config.Scheme = state.Config.Scheme |> Scheme.toggle
            },
            Cmd.none

    let private store : Store = Store.FileSystem.getConfigDir |> Store.FileSystem.make

    /// Creates data directories on application initialization and returns full path to file
    let private ensureDirs (baseDir : string) =
        Views.List
        |> List.map (fun view ->
            view.dirName
            |> fun name -> Path.Combine(baseDir, name)
            |> Directory.CreateDirectory
            |> _.FullName)

    /// Initialize application state
    let init () : Model * Cmd<Message> =
        ensureDirs Store.FileSystem.getConfigDir |> ignore

        match store.Load() with
        | Ok cfg -> { Config = cfg; CurrentView = Capture; Error = None }, Cmd.none
        | Error err ->
            {
                Config = Config.Default
                CurrentView = Capture
                Error = Some err
            },
            Cmd.none
