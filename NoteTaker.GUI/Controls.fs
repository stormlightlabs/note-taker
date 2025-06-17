namespace NoteTaker.Controls

open NoteTaker
open TextMateSharp.Grammars
open TextMateSharp.Registry
open Avalonia
open Avalonia.Controls
open Avalonia.Input
open Avalonia.Media
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Controls.Documents
open Avalonia.Media.TextFormatting
open Avalonia.Controls.Primitives

module Syntax =

    let private getScope (input : string) (token : IToken) : (string * string) =
        let tRange =
            let safeStart = max 0 token.StartIndex
            let safeEnd = min input.Length token.EndIndex

            max 0 (safeEnd - safeStart)

        input |> _.Substring(token.StartIndex, tRange),
        token.Scopes |> List.ofSeq |> List.rev |> List.head

    let tokenizeLine (model : Model) (inputLn : string) =
        let grammar =
            match model.Editor.TmScope with
            | Some v -> model.Editor.GrammarRegistry.LoadGrammar v
            | None -> null

        if isNull grammar then
            // Fallback when grammar loading fails
            [ (inputLn, "text") ]
        else

            let tokens = grammar.TokenizeLine(inputLn).Tokens |> Seq.toList

            if tokens.IsEmpty then
                []
            else
                tokens |> List.map (getScope inputLn)

    let renderScope model (text, scope) : IView =


        Run.create [
            Run.text text
            Run.foreground (
                model.Editor.ScopeColorMap
                |> Map.tryFind scope
                |> Option.map (fun color -> SolidColorBrush color :> IBrush)
                |> Option.defaultValue (Brushes.Black :> IBrush)
            )
        ]

    let private makeTextLayout (text : string) tf fs lh =
        new TextLayout(
            text = text,
            typeface = tf,
            fontSize = fs,
            foreground = Brushes.Black,
            textAlignment = TextAlignment.Left,
            textWrapping = TextWrapping.NoWrap,
            textTrimming = TextTrimming.None,
            textDecorations = TextDecorationCollection(),
            maxWidth = System.Double.PositiveInfinity,
            maxHeight = System.Double.PositiveInfinity,
            lineHeight = lh,
            maxLines = System.Int32.MaxValue,
            textStyleOverrides = null
        )

    let getOrMeasureLine model i : LineMeasurement =
        match model.Editor.Cache |> Map.tryFind i with
        | Some meas -> meas
        | None ->
            // Safety check: ensure the line index is valid
            let lineText =
                if i >= 0 && i < model.Editor.Lines.Length then
                    model.Editor.Lines.[i]
                else
                    "" // Return empty string for invalid indices

            let typeface = Typeface("Consolas, Courier New, monospace")

            let layout =
                makeTextLayout lineText typeface model.Editor.FontSize model.Editor.LineHeight

            LineMeasurement.from layout


/// TODO:
///     1. Load and register the Markdown grammar using an IRegistryOptions implementation
///     2. Map TextMate scopes to Avalonia Brushes
///     3. Write a custom control that renders each line
///     4. Override OnKeyDown, OnTextInput, OnPointerPressed to
///           i. mutate lines
///          ii. track caret position
///         iii. track selection ranges
///     5. Virtualization: only render visible lines by clipping drawing to Bounds.
///     6. Line measuring cache: reuse FormattedText objects where possible
///     7. Smooth scrolling: manage an offset and draw from y = -scrollY.
///     8. Search, code folding, & bracket matching: leverage the same token streams plus simple
///        algorithms.
module EditorControl =
    let private upperBound model i =
        let y =
            i - fst model.Editor.VisibleRange
            |> float
            |> fun a -> a * model.Editor.LineHeight
            |> fun b -> b - model.Editor.ScrollY

        if i < 10 then // Only debug first 10 lines to avoid spam
            $"Debug upperBound: line {i}"
            :: $"VisibleRange={model.Editor.VisibleRange}"
            :: $"LineHeight={model.Editor.LineHeight}"
            :: $"ScrollY={model.Editor.ScrollY}, result={y}"
            :: []
            |> List.reduce (fun acc item -> $"{acc}, {item}")
            |> fun msg -> printfn $"{msg}"

        y

    let private visibleRange model =
        if model.Editor.Lines.IsEmpty then
            []
        else
            let maxLineIndex = model.Editor.Lines.Length - 1
            let startLine = fst model.Editor.VisibleRange
            let endLine = snd model.Editor.VisibleRange

            printfn
                $"Debug visibleRange: Lines.Length={model.Editor.Lines.Length}, maxLineIndex={maxLineIndex}, VisibleRange=({startLine}, {endLine})"

            // If no specific range is set, show all lines
            let actualStartLine = if startLine = 0 && endLine = 0 then 0 else startLine

            let actualEndLine =
                if startLine = 0 && endLine = 0 then
                    maxLineIndex // Show all lines when no specific range is set
                else
                    min endLine maxLineIndex

            let result = [ max 0 actualStartLine .. actualEndLine ]

            printfn
                $"Debug visibleRange result: {result.Length} lines, range [{actualStartLine}..{actualEndLine}]"

            result

    let private lineNumbers model dispatch =
        let renderBlock i : IView =
            TextBlock.create [
                TextBlock.text (i + 1 |> string)
                TextBlock.fontSize model.Editor.FontSize
                Canvas.top (float i * model.Editor.LineHeight)
                Canvas.left 4.0
                TextBlock.foreground Brushes.Gray
                TextBlock.fontFamily "Consolas, Courier New, monospace"
            ]

        let allLines =
            if model.Editor.Lines.IsEmpty then
                []
            else
                [ 0 .. model.Editor.Lines.Length - 1 ]

        let totalHeight =
            if model.Editor.Lines.IsEmpty then
                100.0 // Minimum height for empty files
            else
                float model.Editor.Lines.Length * model.Editor.LineHeight

        allLines
        |> List.map renderBlock
        |> fun blocks ->
            Canvas.create [
                Canvas.width 60.0
                Canvas.height totalHeight
                Canvas.clipToBounds true
                Canvas.children blocks
                Canvas.background Brushes.LightGray
            ]

    let private contentLine model dispatch : IView =
        /// Renders a single content line with syntax highlighting
        let renderContentLine lineIndex =
            // Get the line text safely
            let lineText =
                if lineIndex >= 0 && lineIndex < model.Editor.Lines.Length then
                    model.Editor.Lines.[lineIndex]
                else
                    ""

            // Calculate the Y position for this line (absolute position, not relative to scroll)
            let yPosition = float lineIndex * model.Editor.LineHeight

            // Tokenize the line for syntax highlighting
            let tokens = Syntax.tokenizeLine model lineText

            // Create runs for each token with appropriate styling
            let runs = tokens |> List.map (Syntax.renderScope model)

            // Create the text block with syntax-highlighted content
            TextBlock.create [
                TextBlock.inlines runs
                TextBlock.fontSize model.Editor.FontSize
                TextBlock.fontFamily "Consolas, Courier New, monospace"
                TextBlock.lineHeight model.Editor.LineHeight
                Canvas.top yPosition
                Canvas.left 4.0
                TextBlock.foreground Brushes.Black
                TextBlock.background Brushes.Transparent
                TextBlock.isHitTestVisible true
                TextBlock.textWrapping TextWrapping.NoWrap
            ]

        // Render all lines (not just visible ones - let ScrollViewer handle virtualization)
        let allLines =
            if model.Editor.Lines.IsEmpty then
                []
            else
                [ 0 .. model.Editor.Lines.Length - 1 ]

        let contentBlocks = allLines |> List.map renderContentLine

        // Calculate total content height
        let totalHeight =
            if model.Editor.Lines.IsEmpty then
                100.0 // Minimum height for empty files
            else
                float model.Editor.Lines.Length * model.Editor.LineHeight

        // Create the canvas container for all content lines
        Canvas.create [
            Canvas.clipToBounds true
            Canvas.width 800.0 // Set a reasonable width
            Canvas.height totalHeight // Set the total height for proper scrolling
            Canvas.children (contentBlocks |> List.map (fun x -> x :> IView))
            Canvas.background Brushes.White
            Canvas.isHitTestVisible true
            Canvas.onPointerPressed (fun args -> dispatch (OnPointerPressed args))
            Canvas.onKeyDown (fun args -> dispatch (OnKeyDown args))
            Canvas.onTextInput (fun args -> dispatch (OnTextInput args))
        ]

    let private control model dispatch =
        ScrollViewer.create [
            ScrollViewer.dock Dock.Right
            ScrollViewer.verticalScrollBarVisibility ScrollBarVisibility.Auto
            ScrollViewer.content (
                Canvas.create [
                    Canvas.onKeyDown (fun args -> dispatch (OnKeyDown args))
                    Canvas.onTextInput (fun args -> dispatch (OnTextInput args))
                    Canvas.onPointerPressed (fun args -> dispatch (OnPointerPressed args))
                    Canvas.children [ contentLine model dispatch ]
                ]
            )
            ScrollViewer.onScrollChanged (fun args -> dispatch (ScrollBy args.OffsetDelta.Y))
        ]

    let render model (dispatch : Message -> unit) =
        let titleSection =

            TextBlock.create [
                TextBlock.dock Dock.Top
                TextBlock.text (
                    match model.Editor.CurrentFile with
                    | Some filePath -> System.IO.Path.GetFileName filePath
                    | None -> "No file open"
                )
                TextBlock.foreground (
                    match model.Editor.CurrentFile with
                    | Some _ -> Brushes.White
                    | None -> Brushes.LightGray
                )
                TextBlock.fontSize 16.0
                TextBlock.fontWeight (
                    match model.Editor.CurrentFile with
                    | Some _ -> FontWeight.Bold
                    | None -> FontWeight.Normal
                )
                TextBlock.margin (Thickness(8, 4))
            ]

        // Create a shared scroll viewer for synchronized scrolling
        let sharedScrollViewer =
            ScrollViewer.create [
                ScrollViewer.verticalScrollBarVisibility ScrollBarVisibility.Auto
                ScrollViewer.horizontalScrollBarVisibility ScrollBarVisibility.Auto
                ScrollViewer.content (
                    DockPanel.create [
                        DockPanel.lastChildFill true
                        DockPanel.children [
                            // Line numbers on the left
                            Border.create [
                                Border.dock Dock.Left
                                Border.width 60.0
                                Border.background Brushes.LightGray
                                Border.child (lineNumbers model dispatch)
                            ]
                            // Content on the right
                            contentLine model dispatch
                        ]
                    ]
                )
            ]

        DockPanel.create [
            DockPanel.lastChildFill true
            DockPanel.children [ titleSection; sharedScrollViewer ]
        ]
