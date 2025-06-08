namespace NoteTaker

open System
open System.IO
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

    static member decodeString(x : string) : Scheme =
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

    static member decode(text : string) : Result<Config, Error> =
        match Decode.fromString Config.Decoder text with
        | Ok cfg -> Ok(cfg)
        | Error message -> Error(DecoderError message)

    static member encode(conf : Config) =
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
