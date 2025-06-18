namespace NoteTaker.Model

open Thoth.Json.Net

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
