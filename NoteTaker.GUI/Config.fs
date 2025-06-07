namespace NoteTaker

open System
open System.IO
open Thoth.Json.Net

type Scheme =
    | Light
    | Dark

    static member decodeString(x : string) : Scheme =
        match x.ToLower() with
        | "dark" -> Dark
        | "light"
        | _ -> Light

type Config =
    { Scheme : Scheme
      RecentFiles : List<string> }

    static member Default : Config = { Scheme = Light; RecentFiles = [] }

    static member Decoder : Decoder<Config> =
        Decode.object (fun get ->
            { Scheme = get.Required.Field "theme" Decode.string |> Scheme.decodeString
              RecentFiles =
                get.Optional.Field "recent_files" (Decode.list Decode.string)
                |> Option.defaultValue [] })

    static member Encoder(conf : Config) =
        Encode.object
            [ "scheme", Encode.string <| conf.Scheme.ToString()
              "recent_files",
              Encode.list <| (conf.RecentFiles |> List.map Encode.string) ]

module Config =
    type Error =
        | LoadFileError
        | DecoderError of string

    let getConfigDir : string =
        let platform = Environment.OSVersion.Platform

        Logger.debug $"Platform: {platform.ToString()}"

        match platform with
        | PlatformID.Unix
        | PlatformID.MacOSX ->
            let home = Environment.GetEnvironmentVariable "HOME"
            Path.Combine(home, ".config")
        | _ -> Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
        |> fun baseDir -> Path.Combine(baseDir, "note_taker")

    let getConfigPath : string =
        getConfigDir
        |> fun dir ->
            Directory.CreateDirectory dir |> ignore
            Path.Combine(dir, "config.json")

    let loadConfig : Result<Config, Error> =
        let confPath = getConfigPath

        if File.Exists confPath then
            try
                File.ReadAllText confPath
                |> fun text ->
                    match Decode.fromString Config.Decoder text with
                    | Ok cfg -> Ok(cfg)
                    | Error message -> Error(DecoderError message)
            with e ->
                Error LoadFileError
        else
            Ok Config.Default

    let saveConfig (conf : Config) : Result<unit, Error> =
        try
            Config.Encoder conf
            |> Encode.toString 2
            |> fun data -> File.WriteAllText(data, getConfigPath)
            |> Ok
        with err ->
            DecoderError err.Message |> Error
