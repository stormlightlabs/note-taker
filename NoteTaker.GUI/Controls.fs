namespace NoteTaker.Controls

open NoteTaker.Model
open TextMateSharp.Grammars
open Avalonia
open Avalonia.Controls
open Avalonia.Controls.Shapes
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

    let private renderCaret model : IView =
        let caretX = 4.0 + (float model.Editor.Caret.Column * 8.0) // Approximate character width
        let caretY = yPos model.Editor.Caret.Line model.Editor.LineHeight

        Rectangle.create [
            Rectangle.width 2.0
            Rectangle.height model.Editor.LineHeight
            Rectangle.fill model.AppTheme.Base05
            Canvas.left caretX
            Canvas.top caretY
        ]
        :> IView

    let private renderSelection model : IView list =
        match model.Editor.Selection with
        | None -> []
        | Some selection ->
            let startLine = min selection.Start.Line selection.End.Line
            let endLine = max selection.Start.Line selection.End.Line

            let startCol =
                if selection.Start.Line <= selection.End.Line then
                    selection.Start.Column
                else
                    selection.End.Column

            let endCol =
                if selection.Start.Line <= selection.End.Line then
                    selection.End.Column
                else
                    selection.Start.Column

            [ startLine..endLine ]
            |> List.map (fun lineIndex ->
                let y = yPos lineIndex model.Editor.LineHeight
                let x = 4.0

                let width =
                    if lineIndex < model.Editor.Lines.Length then
                        float model.Editor.Lines.[lineIndex].Length * 8.0 // Approximate
                    else
                        100.0

                Rectangle.create [
                    Rectangle.fill (
                        model.AppTheme.Base02.ToUInt32() |> Color.FromUInt32 |> SolidColorBrush
                    )
                    Rectangle.height model.Editor.LineHeight
                    Rectangle.width width
                    Canvas.left x
                    Canvas.top y
                    Rectangle.opacity 0.3
                ]
                :> IView)

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
                TextBlock.isHitTestVisible false // Let canvas handle input
                TextBlock.textWrapping (wrapping model)
            ]

        let contentBlocks =
            match model.Editor.Lines with
            | [] -> [ renderCaret model ] // Show caret even in empty files
            | lines ->
                let textBlocks =
                    lines
                    |> _.Length
                    |> fun count ->
                        [ 0 .. count - 1 ] |> List.map renderLn |> List.map (fun x -> x :> IView)

                let selectionBlocks = renderSelection model
                let caretBlock = [ renderCaret model ]

                selectionBlocks @ textBlocks @ caretBlock

        Canvas.create [
            Canvas.clipToBounds true
            Canvas.height (totalHeight model)
            Canvas.children contentBlocks
            Canvas.background model.AppTheme.Base00
            Canvas.isHitTestVisible true
            Canvas.focusable true
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
        let editorCanvas =
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

        ScrollViewer.create [
            ScrollViewer.verticalScrollBarVisibility ScrollBarVisibility.Auto
            ScrollViewer.horizontalScrollBarVisibility ScrollBarVisibility.Auto
            ScrollViewer.content editorCanvas
            ScrollViewer.focusable true
            ScrollViewer.onGotFocus (fun _ ->
                // Focus the canvas when scroll viewer gets focus
                dispatch (UpdateCaret model.Editor.Caret))
        ]

    let render model (dispatch : Message -> unit) =
        DockPanel.create [
            DockPanel.lastChildFill true
            DockPanel.children [ renderTitle model dispatch; renderEditorBody model dispatch ]
        ]
