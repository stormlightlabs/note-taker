namespace NoteTaker.Model

open System.IO

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
