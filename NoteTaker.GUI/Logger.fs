namespace NoteTaker.Logger

open Serilog

module Logger =
    open Microsoft.Extensions.Logging

    Log.Logger <- LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console().CreateLogger()

    let private factory =
        LoggerFactory.Create(fun bridge -> bridge.AddSerilog() |> ignore)

    let private create (name : string option) =
        match name with
        | Some n -> factory.CreateLogger n
        | None -> factory.CreateLogger "NoteTaker"

    let private appLogger = create None

    let debug args = appLogger.LogDebug args

    let dbg label value =
        appLogger.LogDebug $"{label}: {value.ToString()}"

        value

    let info args = appLogger.LogInformation args
    let warn args = appLogger.LogWarning args
    let error args = appLogger.LogError args
    let trace args = appLogger.LogTrace args
    let crit args = appLogger.LogCritical args
