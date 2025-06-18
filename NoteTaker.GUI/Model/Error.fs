namespace NoteTaker.Model

type Error =
    | LoadFileError of string
    | DecoderError of string
    | SetupDirsError
    | LoadConfigError of string
    | ConfigDoesNotExistError
    | DirCreationError of (string * string)
    | Unexpected of string option

    member this.str : string = this.ToString()
