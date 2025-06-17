(*
    TODO: Markdown syntax highlighting
*)
module NoteTaker.GUI

open Elmish
open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Themes.Fluent
open Avalonia.FuncUI.Hosts
open Avalonia.FuncUI.Elmish
open Avalonia.FuncUI.AvaloniaExtensions

open NoteTaker
open NoteTaker.Views

/// View layer entry point
let view (model : Model) (dispatch : Message -> unit) = Windows.Main.render model dispatch

module Program =
    type MainWindow() as this =
        inherit HostWindow()

        do
            base.Title <- "Stormlight Note Taker"
            base.Width <- 800.0
            base.Height <- 600.0

            Program.mkProgram Model.init Model.update view
            |> Program.withConsoleTrace
            |> Program.withHost this
            |> Program.withSubscription (fun _ -> [
                [ "sub:fileWatcher" ],
                fun dispatch -> Watcher.setup dispatch :> System.IDisposable
            ])
            |> Program.run

    type App() =
        inherit Application()

        override this.Initialize() =
            this.Styles.Add(FluentTheme())
            this.RequestedThemeVariant <- Styling.ThemeVariant.Dark

        override this.OnFrameworkInitializationCompleted() =
            match this.ApplicationLifetime with
            | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime ->
                let mainWindow = MainWindow()
                desktopLifetime.MainWindow <- mainWindow
            | _ -> ()

    [<EntryPoint>]
    let main argv =
        AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .UseSkia()
            .StartWithClassicDesktopLifetime(argv)
