namespace NoteTaker.Model

/// A pluggable persistence strategy
type Store = {
    Load : unit -> Result<Config, Error>
    Save : Config -> Result<unit, Error>
}

/// Implementations of Store Record-of-Functions
module Store =
    module FileSystem =
        open System.IO
        open System

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
