namespace NoteTaker.Model

open Avalonia.Media


type Scheme =
    | Light
    | Dark

    member this.defaultTextColor : Color =
        match this with
        | Light -> Colors.Black
        | Dark -> Colors.White

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
