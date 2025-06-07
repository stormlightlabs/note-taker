module NoteTaker.GUI

open System.IO
open Elmish
open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Themes.Fluent
open Avalonia.FuncUI.Hosts
open Avalonia.Controls
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Elmish
open Avalonia.Layout

open Microsoft.Extensions.Logging
open NoteTaker

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

type ApplicationError =
    | SetupDirsError
    | LoadConfigError of string
    | DirCreationError of (string * string)
    | Unexpected

/// Application State
type Model =
    { Config : Config
      CurrentView : Views
      Error : ApplicationError option }

/// State Updates
type Message =
    | SelectView of Views
    | ToggleScheme

/// Creates data directories on application initialization
let ensureDirs () =
    let confPath = Config.getConfigPath

    match Config.loadConfig with
    | Ok cfg ->
        let baseDir = Path.GetDirectoryName confPath

        Logger.debug $"base dir: {baseDir}"

        Views.List
        |> List.map (fun view ->
            let dirPath = view.dirName |> fun name -> Path.Combine(baseDir, name)

            try
                Directory.CreateDirectory dirPath
                |> fun dirInfo -> Logger.info $"Created {dirInfo.Name}"
                |> Ok
            with exn ->
                Logger.error $"Creating {dirPath} failed with {exn.Message}"
                DirCreationError(dirPath, exn.Message) |> Error)
        |> List.filter _.IsError
        |> fun results ->
            match results with
            | [] -> Ok cfg
            | _ -> SetupDirsError |> Error
    | Error(err) -> err.ToString() |> LoadConfigError |> Error

/// Initialize application state
let init () : Model * Cmd<Message> =
    match ensureDirs () with
    | Ok cfg ->
        { Config = cfg
          CurrentView = Capture
          Error = None },
        Cmd.none
    | Error err ->
        { Config = Config.Default
          CurrentView = Capture
          Error = Some err },
        Cmd.none

/// State mutation handler
let update (msg : Message) (state : Model) =
    match msg with
    | SelectView view -> { state with CurrentView = view }, Cmd.none
    | ToggleScheme ->
        let scheme =
            match state.Config.Scheme with
            | Light -> Dark
            | Dark -> Light

        { state with
            Model.Config.Scheme = scheme },
        Cmd.none

type DispatchFn = Message -> unit

/// UI Rendering
/// TODO: Separate into Windows/Screens & Widgets within a UI specific namespace
let view (model : Model) (dispatch : DispatchFn) =
    let sidebarItems : List<Types.IView> =
        let handler v =
            Logger.debug $"View Selected: {v}"
            dispatch (SelectView v)

        Views.List
        |> List.map (fun view ->
            Button.create
                [ Button.content view.label; Button.onClick (fun _ -> handler view) ])

    // TODO: "Main" window
    DockPanel.create
        [ DockPanel.children
              [
                // TODO: "Sidebar" widget
                StackPanel.create [ StackPanel.children sidebarItems ]
                // TODO: "Content" widget
                TextBlock.create
                    [ TextBlock.text $"Current View: {model.CurrentView.label}"
                      TextBlock.verticalAlignment VerticalAlignment.Center
                      TextBlock.horizontalAlignment HorizontalAlignment.Center ] ] ]

module Program =
    type MainWindow() as this =
        inherit HostWindow()

        do
            base.Title <- "Stormlight Note Taker"
            base.Width <- 800.0
            base.Height <- 600.0

            Program.mkProgram init update view
            |> Program.withConsoleTrace
            |> Program.withHost this
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
