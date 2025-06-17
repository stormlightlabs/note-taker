(*
    TODO: Merge Editor State into rest of Application state
*)
namespace EditorControl

open TextMateSharp.Registry
open TextMateSharp.Grammars
open Avalonia
open Avalonia.Controls
open Avalonia.Input
open Avalonia.Media
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Controls.Documents
open Avalonia.Controls.Shapes
open Avalonia.Media.TextFormatting
open Avalonia.Controls.Primitives

type Base16 = {
    Base00 : Color
    Base01 : Color
    Base02 : Color
    Base03 : Color
    Base04 : Color
    Base05 : Color
    Base06 : Color
    Base07 : Color
    Base08 : Color
    Base09 : Color
    Base0A : Color
    Base0B : Color
    Base0C : Color
    Base0D : Color
    Base0E : Color
    Base0F : Color
}

module Base16 =
    let mapping theme =
        [
            "punctuation.definition.heading.markdown", SolidColorBrush theme.Base04 :> IBrush
            "markup.heading.markdown", SolidColorBrush theme.Base0D
            "markup.heading.setext.1.markdown", SolidColorBrush theme.Base0D
            "markup.heading.setext.2.markdown", SolidColorBrush theme.Base0D
            "markup.bold.markdown", SolidColorBrush theme.Base0B
            "markup.italic.markdown", SolidColorBrush theme.Base0E
            "markup.bold.italic.markdown", SolidColorBrush theme.Base0B
            "punctuation.definition.blockquote.markdown", SolidColorBrush theme.Base03
            "markup.quote.markdown", SolidColorBrush theme.Base0C
            "punctuation.definition.list.begin.markdown", SolidColorBrush theme.Base03
            "markup.list.unnumbered.markdown", SolidColorBrush theme.Base0A
            "markup.list.numbered.markdown", SolidColorBrush theme.Base0A
            "punctuation.definition.list.number.markdown", SolidColorBrush theme.Base03
            "markup.underline.link.markdown", SolidColorBrush theme.Base09
            "markup.link.inline.markdown", SolidColorBrush theme.Base09
            "markup.link.reference.markdown", SolidColorBrush theme.Base09
            "string.other.link.title.markdown", SolidColorBrush theme.Base0B
            "string.other.link.description.markdown", SolidColorBrush theme.Base0B
            "punctuation.definition.string.begin.markdown", SolidColorBrush theme.Base04
            "punctuation.definition.string.end.markdown", SolidColorBrush theme.Base04
            "punctuation.definition.link.begin.markdown", SolidColorBrush theme.Base04
            "punctuation.definition.link.end.markdown", SolidColorBrush theme.Base04
            "markup.image.markdown", SolidColorBrush theme.Base08
            "string.other.image.title.markdown", SolidColorBrush theme.Base08

            // Inline code
            "markup.inline.raw.markdown", SolidColorBrush theme.Base0B
            "punctuation.definition.raw.begin.markdown", SolidColorBrush theme.Base04
            "punctuation.definition.raw.end.markdown", SolidColorBrush theme.Base04

            "markup.fenced_code.markdown", SolidColorBrush theme.Base0B
            "fenced_code.block.marker.backtick.markdown", SolidColorBrush theme.Base03
            "fenced_code.block.language.markdown", SolidColorBrush theme.Base0E

            // Horizontal rules
            "meta.separator.markdown", SolidColorBrush theme.Base03
            "markup.table.markdown", SolidColorBrush theme.Base0A
            "punctuation.separator.table.markdown", SolidColorBrush theme.Base03
            "markup.footnote.definition.markdown", SolidColorBrush theme.Base09
            "markup.footnote.reference.markdown", SolidColorBrush theme.Base09

            // Emphasis markers
            "punctuation.definition.emphasis.markdown", SolidColorBrush theme.Base04

            // YAML front matter
            "meta.block.yaml.markdown", SolidColorBrush theme.Base0F
            "punctuation.definition.metadata.markdown", SolidColorBrush theme.Base0F
            "meta.separator.metadata.markdown", SolidColorBrush theme.Base0F

            // Math expr
            "markup.math.inline.markdown", SolidColorBrush theme.Base0C
            "punctuation.definition.math.begin.markdown", SolidColorBrush theme.Base04
            "punctuation.definition.math.end.markdown", SolidColorBrush theme.Base04
        ]
        |> Map.ofList

type EditorState = {
    Content : string
    Lines : string list
    GrammarRegistry : Registry
    ScopeBrushMap : Map<string, IBrush>
    Caret : CaretPosition
    Selection : SelectionRange option
    VisibleRange : (int * int)
    ScrollY : float
    Cache : Map<int, LineMeasurement>
    /// Line No.: open, close
    BracketMatches : Map<int, (int * int)>
    /// Line, Column hits
    SearchQuery : string
    SearchResults : (int * int) List
    ViewportSize : Size
    Folds : FoldRange list
    LineHeight : float
    FontSize : float
}

and CaretPosition = { Line : int; Column : int }
and SelectionRange = { Start : CaretPosition; End : CaretPosition }

and LineMeasurement = {
    Layout : TextLayout
    Width : float
} with

    static member from f : LineMeasurement = { Layout = f; Width = f.Width }

and FoldRange = { From : int; To : int; Folded : bool }

type EditorMessage =
    | LoadGrammar of RegistryOptions
    | RegisterBrushMap of (string * IBrush) list
    | OnKeyDown of KeyEventArgs
    | OnTextInput of TextInputEventArgs
    | OnPointerPressed of PointerPressedEventArgs
    | ScrollBy of float
    | SetVisibleRange of (int * int)
    | ResizeViewport of Size
    | UpdateCaret of CaretPosition
    | UpdateSelection of SelectionRange option
    | CacheLineMeasurement of int * LineMeasurement
    | ToggleFold of int
    | UpdateBracketMatches of Map<int, (int * int)>
    | PerformSearch of string
    | SetSearchReults of (int * int) list

module Syntax =
    let private tRange (tok : IToken) = tok.EndIndex - tok.StartIndex

    let private getScope (input : string) (token : IToken) : (string * string) =
        input |> _.Substring(token.StartIndex, tRange token),
        token.Scopes |> List.ofSeq |> List.rev |> List.head

    let tokenizeLine (registry : Registry) (inputLn : string) =
        registry.LoadGrammar "markdown"
        |> _.TokenizeLine(inputLn)
        |> _.Tokens
        |> Seq.map (getScope inputLn)
        |> Seq.toList

    let renderScope model (text, scope) : IView =
        Run.create [
            Run.text text
            Run.foreground (
                model.ScopeBrushMap |> Map.tryFind scope |> Option.defaultValue Brushes.Black
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

    let getOrMeasureLine (model : EditorState) i : LineMeasurement =
        match model.Cache |> Map.tryFind i with
        | Some meas -> meas
        | None ->
            makeTextLayout model.Lines.[i]
            |> fun addTypeface -> addTypeface (Typeface "monospace")
            |> fun setFontsize -> setFontsize 12.0
            |> fun setLineHeight -> setLineHeight 18.0
            |> LineMeasurement.from



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
module Editor =
    let private upperBound model i =
        i - fst model.VisibleRange
        |> float
        |> fun a -> a * model.LineHeight
        |> fun b -> b - model.ScrollY

    let private visibleRange model = [ fst model.VisibleRange .. snd model.VisibleRange ]

    let private lineNumbers model _dispatch =
        let renderBlock i : IView =
            TextBlock.create [
                TextBlock.text (i + 1 |> string)
                TextBlock.fontSize model.FontSize
                TextBlock.renderTransform (TranslateTransform(0, i |> upperBound model))
            ]

        visibleRange model
        |> List.map renderBlock
        |> fun blocks ->
            Canvas.create [ Canvas.width 40.0; Canvas.clipToBounds true; Canvas.children blocks ]

    let private contentLines model _dispatch =
        let renderLine i : IView =
            let registry = model.GrammarRegistry
            let input = model.Lines.[i]
            let segments = input |> Syntax.tokenizeLine registry
            let measurement = i |> Syntax.getOrMeasureLine model
            let inlines = segments |> List.map (Syntax.renderScope model)

            let highlights =
                let renderRect (hit : Rect) : IView =
                    Rectangle.create [
                        Rectangle.fill (Colors.Yellow |> _.ToUInt32() |> SolidColorBrush)
                        Canvas.left hit.X
                        Canvas.top 0.0
                        Rectangle.width hit.Width
                        Rectangle.height hit.Height
                    ]

                model.SearchResults
                |> List.collect (fun (ln, col) ->
                    if ln = i then
                        measurement.Layout.HitTestTextRange(col, model.SearchQuery.Length)
                        |> Seq.toList
                        |> List.map renderRect
                        |> Seq.toList
                    else
                        [])

            let bracketMatches : IView list =
                let layout = measurement.Layout

                match model.BracketMatches |> Map.tryFind i with
                | Some(a, b) ->
                    let p1 = layout.HitTestTextPosition a
                    let p2 = layout.HitTestTextPosition b

                    Rectangle.create [
                        Canvas.left p1.X
                        Canvas.top 0.0
                        Rectangle.width (p2.X - p1.X)
                        Rectangle.height layout.Height
                        Rectangle.fill (SolidColorBrush Colors.LightGray)
                    ]
                    :: []
                | None -> []

            Canvas.create [
                Canvas.clipToBounds true
                Canvas.children [
                    Span.create [ Span.inlines (inlines @ highlights @ bracketMatches) ]
                ]
            ]

        model |> visibleRange |> List.map renderLine


    let private control model dispatch =
        ScrollViewer.create [
            ScrollViewer.verticalScrollBarVisibility ScrollBarVisibility.Auto
            ScrollViewer.content (
                Canvas.create [
                    Canvas.clipToBounds true
                    Canvas.onKeyDown (fun args -> dispatch (OnKeyDown args))
                    Canvas.onTextInput (fun args -> dispatch (OnTextInput args))
                    Canvas.onPointerPressed (fun args -> dispatch (OnPointerPressed args))
                    Canvas.children (contentLines model dispatch)
                ]
            )
            ScrollViewer.onScrollChanged (fun args -> dispatch (ScrollBy args.OffsetDelta.Y))
        ]

    let render (model : EditorState) (dispatch : EditorMessage -> unit) =
        DockPanel.create [
            DockPanel.lastChildFill true
            DockPanel.children [ lineNumbers model dispatch; control model dispatch ]
        ]
