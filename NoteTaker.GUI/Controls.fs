namespace NoteTaker.Controls

open NoteTaker
open TextMateSharp.Grammars
open Avalonia
open Avalonia.Controls
open Avalonia.Media
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Controls.Documents
open Avalonia.Controls.Primitives

module Syntax =
    open TextMateSharp.Registry

    let endIndex (token : IToken) (input : string) =
        let safeStart = max 0 token.StartIndex
        let safeEnd = min input.Length token.EndIndex

        max 0 (safeEnd - safeStart)

    let private getScope (input : string) (token : IToken) : (string * string) =
        input |> _.Substring(token.StartIndex, endIndex token input),
        token.Scopes |> List.ofSeq |> List.rev |> List.head

    let tokenizeLine (textMateScope : Option<string>) (registry : Registry) (inputLn : string) =
        match textMateScope with
        | None -> []
        | Some scope when registry.LoadGrammar scope |> isNull -> []
        | Some scope ->
            registry.LoadGrammar scope
            |> _.TokenizeLine(inputLn)
            |> _.Tokens
            |> Array.toList


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

/// TODO:
///     1. Load and register the Markdown grammar using an IRegistryOptions implementation ✅
///     2. Map TextMate scopes to Avalonia Brushes ✅
///     3. Write a custom control that renders each line ✅
///     4. Override OnKeyDown, OnTextInput, OnPointerPressed to
///           i. mutate lines
///          ii. track caret position
///         iii. track selection ranges
///     5. Virtualization: only render visible lines by clipping drawing to Bounds. ✅
///     6. Smooth scrolling: manage an offset and draw from y = -scrollY. ✅
///     7. Search, code folding, & bracket matching: leverage the same token streams
///        plus simple algorithms.
module EditorControl =
    let private totalHeight model =
        match model.Editor.Lines with
        | [] -> 100.0 // Minimum height for empty files
        | _ -> float model.Editor.Lines.Length * model.Editor.LineHeight

    let private wrapping model =
        match model.Editor.ShouldWrap with
        | true -> TextWrapping.Wrap
        | false -> TextWrapping.NoWrap

    let private renderLineNumbers model _ : IView =
        let renderBlock i : IView =
            TextBlock.create [
                TextBlock.text (i + 1 |> string)
                TextBlock.fontSize model.Editor.FontSize
                Canvas.top (float i * model.Editor.LineHeight)
                Canvas.left 4.0
                TextBlock.foreground model.AppTheme.Base03
                TextBlock.fontWeight FontWeight.Bold
                TextBlock.background Brushes.Transparent
                TextBlock.textAlignment TextAlignment.Right
                TextBlock.fontFamily model.Editor.FontFamily
            ]

        match model.Editor.Lines with
        | [] -> []
        | rest -> rest
        |> fun lines ->
            [ 0 .. lines.Length - 1 ]
            |> List.map renderBlock
            |> fun blocks ->
                Canvas.create [
                    Canvas.height (totalHeight model)
                    Canvas.clipToBounds true
                    Canvas.children blocks
                ]

    /// Calculate the Y position for this line (absolute position, not relative to scroll)
    let private yPos index lnHeight = float index * lnHeight

    let private renderContents model dispatch : IView =
        let renderLn lnIndex =
            let lineText =
                if lnIndex >= 0 && lnIndex < model.Editor.Lines.Length then
                    model.Editor.Lines.[lnIndex]
                else
                    ""

            let yPosition = yPos lnIndex model.Editor.LineHeight

            let tokens =
                lineText
                |> Syntax.tokenizeLine model.Editor.TmScope model.Editor.GrammarRegistry

            let getColor (scopes : seq<string>) (defaultColor : Color) colorMap =
                scopes
                |> Seq.toList
                |> List.rev
                |> List.tryPick (fun scope -> colorMap |> Map.tryFind scope)
                |> Option.defaultValue defaultColor

            let runs : IView list =
                match tokens with
                | [] -> [ Run.create [ Run.text lineText ] ]
                | _ ->
                    tokens
                    |> List.map (fun token ->
                        Run.create [
                            Run.text (
                                lineText
                                |> _.Substring(token.StartIndex, Syntax.endIndex token lineText)
                            )
                            Run.foreground (
                                model.Editor.ScopeColorMap
                                |> getColor token.Scopes model.defaultFg
                            )
                        ])

            TextBlock.create [
                TextBlock.inlines runs
                TextBlock.fontSize model.Editor.FontSize
                TextBlock.fontFamily model.Editor.FontFamily
                TextBlock.lineHeight model.Editor.LineHeight
                Canvas.top yPosition
                Canvas.left 4.0
                TextBlock.foreground Brushes.Black
                TextBlock.background Brushes.Transparent
                TextBlock.isHitTestVisible true
                TextBlock.textWrapping (wrapping model)
            ]

        let contentBlocks =
            match model.Editor.Lines with
            | [] -> [] // Minimum height for empty files
            | lines ->
                lines
                |> _.Length
                |> fun count ->
                    [ 0 .. count - 1 ] |> List.map renderLn |> List.map (fun x -> x :> IView)

        Canvas.create [
            Canvas.clipToBounds true
            Canvas.height (totalHeight model)
            Canvas.children contentBlocks
            Canvas.background model.AppTheme.Base00
            Canvas.isHitTestVisible true
            Canvas.onPointerPressed (fun args -> dispatch (OnPointerPressed args))
            Canvas.onKeyDown (fun args -> dispatch (OnKeyDown args))
            Canvas.onTextInput (fun args -> dispatch (OnTextInput args))
        ]

    let private renderTitle (model : Model) _ =
        let currentFile = model.Editor.CurrentFile

        let titleText =
            match currentFile with
            | Some filePath -> System.IO.Path.GetFileName filePath
            | None -> "No file open"

        let fg =
            match currentFile with
            | Some _ -> model.AppTheme.Base05
            | None -> model.AppTheme.Base03

        let fw =
            match currentFile with
            | Some _ -> FontWeight.Bold
            | None -> FontWeight.Normal

        TextBlock.create [
            TextBlock.dock Dock.Top
            TextBlock.text titleText
            TextBlock.foreground fg
            TextBlock.fontSize 20.0
            TextBlock.fontWeight fw
            TextBlock.margin (Thickness(8, 4))
        ]

    let private renderEditorBody model dispatch =
        ScrollViewer.create [
            ScrollViewer.verticalScrollBarVisibility ScrollBarVisibility.Auto
            ScrollViewer.horizontalScrollBarVisibility ScrollBarVisibility.Auto
            ScrollViewer.content (
                DockPanel.create [
                    DockPanel.lastChildFill true
                    DockPanel.children [
                        Border.create [
                            Border.dock Dock.Left
                            Border.maxWidth 50.0
                            Border.width 25.0
                            Border.background model.AppTheme.Base01
                            Border.child (renderLineNumbers model dispatch)
                        ]
                        Border.create [
                            Border.borderThickness 0
                            Border.padding (Thickness(0, 0, 8, 0))
                            Border.background Colors.Transparent
                            Border.child (renderContents model dispatch)
                        ]
                    ]
                ]
            )
        ]

    let render model (dispatch : Message -> unit) =
        DockPanel.create [
            DockPanel.lastChildFill true
            DockPanel.children [ renderTitle model dispatch; renderEditorBody model dispatch ]
        ]
