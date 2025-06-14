namespace NoteTaker.Views

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Media
open Avalonia.Layout
open NoteTaker

/// Represents base16 theme
type Theme = {
    Name : string
    Author : string
    IsDark : bool
    /// Default Background
    Base00 : string
    /// Lighter Background (Status bars, line numbers)
    Base01 : string
    /// Selection Background
    Base02 : string
    /// Comments, Invisibles, Line Highlighting
    Base03 : string
    /// Dark Foreground (Status bars)
    Base04 : string
    /// Default Foreground, Caret, Delimiters, Operators
    Base05 : string
    /// Light Foreground (Not often used)
    Base06 : string
    /// Light Background (Not often used)
    Base07 : string
    /// Variables, XML Tags, Markup Link Text, Markup Lists, Diff Deleted
    Base08 : string
    /// Integers, Boolean, Constants, XML Attributes, Markup Link Url
    Base09 : string
    /// Classes, Markup Bold, Search Text Background
    Base0A : string
    /// Strings, Inherited Class, Markup Code, Diff Inserted
    Base0B : string
    /// Support, Regular Expressions, Escape Characters, Markup Quotes
    Base0C : string
    /// Functions, Methods, Attribute IDs, Headings
    Base0D : string
    /// Keywords, Storage, Selector, Markup Italic, Diff Changed
    Base0E : string
    /// Deprecated, Opening/Closing Embedded Language Tags
    Base0F : string
}

module Theme =
    module Presets =
        let solarizedDark : Theme = {
            Name = "Solarized Dark"
            Author = "Ethan Schoonover"
            IsDark = true
            Base00 = "#002b36"
            Base01 = "#073642"
            Base02 = "#586e75"
            Base03 = "#657b83"
            Base04 = "#839496"
            Base05 = "#93a1a1"
            Base06 = "#eee8d5"
            Base07 = "#fdf6e3"
            Base08 = "#dc322f"
            Base09 = "#cb4b16"
            Base0A = "#b58900"
            Base0B = "#859900"
            Base0C = "#2aa198"
            Base0D = "#268bd2"
            Base0E = "#6c71c4"
            Base0F = "#d33682"
        }

        let solarizedLight : Theme = {
            Name = "Solarized Light"
            Author = "Ethan Schoonover"
            IsDark = false
            Base00 = "#fdf6e3"
            Base01 = "#eee8d5"
            Base02 = "#93a1a1"
            Base03 = "#839496"
            Base04 = "#657b83"
            Base05 = "#586e75"
            Base06 = "#073642"
            Base07 = "#002b36"
            Base08 = "#dc322f"
            Base09 = "#cb4b16"
            Base0A = "#b58900"
            Base0B = "#859900"
            Base0C = "#2aa198"
            Base0D = "#268bd2"
            Base0E = "#6c71c4"
            Base0F = "#d33682"
        }


open Theme

module Widgets =
    module Preview =
        open FSharp.Formatting.Markdown

        let inline textBlock text fontSize =
            TextBlock.create [
                TextBlock.text text
                TextBlock.fontSize fontSize
                TextBlock.textWrapping TextWrapping.Wrap
                TextBlock.margin (0.0, 4.0)
            ]

        let rec renderSpans (spans : MarkdownSpans) : Types.IView list =
            spans
            |> Seq.toList
            |> List.collect (fun span ->
                match span with
                | Literal(text, _) -> [ textBlock text 14.0 ]
                | Strong(body, _) -> [
                    StackPanel.create [
                        StackPanel.orientation Orientation.Horizontal
                        StackPanel.children (renderSpans body)
                        StackPanel.fontWeight FontWeight.Bold
                    ]
                  ]
                | InlineCode(_code, _range) -> failwith "todo"
                | Emphasis(body, _) -> [
                    StackPanel.create [
                        StackPanel.orientation Orientation.Horizontal
                        StackPanel.children (renderSpans body)
                        StackPanel.fontStyle FontStyle.Italic
                    ]
                  ]
                | AnchorLink(_link, _range) -> failwith "todo"
                | DirectLink(_body, _link, _title, _range) -> [
                    TextBlock.create [
                        TextBlock.text ""
                        TextBlock.foreground Brushes.DodgerBlue
                        TextBlock.cursor Avalonia.Input.Cursor.Default
                    ]
                  ]
                | IndirectLink(_body, _original, _key, _range) -> failwith "todo"
                | DirectImage(_body, _link, _title, _range) -> failwith "todo"
                | IndirectImage(_body, _link, _key, _range) -> failwith "todo"
                | HardLineBreak _range -> failwith "todo"
                | LatexInlineMath(_code, _range) -> failwith "todo"
                | LatexDisplayMath(_code, _range) -> failwith "todo"
                | EmbedSpans(_customSpans, _range) -> failwith "todo")


        // | DirectLink(body = b; link = url) -> [
        //     TextBlock.create [
        //         TextBlock.text (
        //             String.concat
        //                 ""
        //                 (renderSpans b |> List.choose (fun x -> x.TryGetText()))
        //         )
        //         TextBlock.foreground Brushes.CornflowerBlue
        //         TextBlock.cursor Input.Cursors.Hand
        //         TextBlock.onTapped (fun _ _ -> BrowserUtil.openUrl url)
        //     ]
        //   ]
        // | _ -> []) // ignore other inline cases for now

        // let rec renderParagraph (par : MarkdownParagraph) : IView list =
        //     match par with
        //     | Heading(size = 1; body = spans) -> [
        //         textBlock (Markdown.WriteSpansPlain spans) 26.0
        //       ]
        //     | Heading(size = 2; body = spans) -> [
        //         textBlock (Markdown.WriteSpansPlain spans) 22.0
        //       ]
        //     | Heading(size = 3; body = spans) -> [
        //         textBlock (Markdown.WriteSpansPlain spans) 18.0
        //       ]
        //     | Paragraph(spans = spans) -> renderSpans spans
        //     | ListBlock(isOrdered = false; items = items) ->
        //         items
        //         |> List.collect (fun item ->
        //             let txt = item |> List.collect renderParagraph
        //
        //             [
        //                 StackPanel.create [
        //                     StackPanel.orientation Orientation.Horizontal
        //                     StackPanel.children (textBlock "• " 14.0 :: txt)
        //                     StackPanel.margin 16.0 0.0 0.0 0.0
        //                 ]
        //             ])
        //     | CodeBlock(code = code; lang = _) -> [
        //         Border.create [
        //             Border.background (SolidColorBrush(Color.Parse "#1e1e1e"))
        //             Border.cornerRadius 4.0
        //             Border.padding 8.0
        //             Border.child (
        //                 TextBlock.create [
        //                     TextBlock.text code
        //                     TextBlock.foreground Brushes.White
        //                     TextBlock.fontFamily "Consolas, Menlo, monospace"
        //                     TextBlock.textWrapping TextWrapping.Wrap
        //                 ]
        //             )
        //         ]
        //       ]
        //     | _ -> [] // unhandled paragraph kinds

        /// TODO: "doc.Paragraphs |> Seq.toList |> List.collect renderParagraph"
        let render (markdown : string) : Types.IView =
            let _doc = Markdown.Parse markdown

            StackPanel.create [ StackPanel.children [] ]

    module Content =
        let render (model : Model) _ =
            TextBlock.create [
                TextBlock.text $"Current View: {model.CurrentView.label}"
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]

    module Sidebar =
        let sidebarItems _ (dispatch : Message -> unit) : List<Types.IView> =
            Section.List
            |> List.map (fun view ->
                Button.create [
                    Button.content view.label
                    Button.onClick (fun _ -> dispatch (SelectView view))
                ])

        let render state dispatch =
            StackPanel.create [ StackPanel.children (sidebarItems state dispatch) ]

    module Sidebars =
        // <Style Selector=".NavSidebar">
        //     <Setter Property="Background" Value="#1e293b"/>
        //     <Setter Property="Padding" Value="8"/>
        //     <Setter Property="FontFamily" Value="Inter"/>
        //     <Setter Property="FontSize" Value="14"/>
        // </Style>
        // <Style Selector=".NavSelected">
        //     <Setter Property="Background" Value="#334155"/>
        //     <Setter Property="Foreground" Value="#38bdf8"/>
        //     <Setter Property="FontWeight" Value="Bold"/>
        // </Style>
        // <Style Selector=".FileSidebar">
        //     <Setter Property="Background" Value="#e2e8f0"/>
        //     <Setter Property="FontSize" Value="13"/>
        // </Style>
        // <Style Selector=".EditorPane">
        //     <Setter Property="Background" Value="White"/>
        //     <Setter Property="BorderThickness" Value="1"/>
        //     <Setter Property="BorderBrush" Value="#cbd5e1"/>
        //     <Setter Property="CornerRadius" Value="8"/>
        // </Style>
        module Navigation =
            let private t = Presets.solarizedDark
            let private isSelected view state = state.CurrentView = view

            let private renderItem (model : Model) dispatch =
                let r (item : Section) : Types.IView =
                    let selected = isSelected item model

                    Button.create [
                        Button.content item.label
                        Button.onClick (fun _ -> dispatch (SelectView item))
                        Button.margin (Thickness(0, 4, 0, 4))
                        Button.horizontalAlignment HorizontalAlignment.Stretch
                        Button.background (if selected then t.Base02 else t.Base01)
                        Button.foreground (if selected then t.Base0D else t.Base05)
                        Button.fontWeight (
                            if selected then FontWeight.Bold else FontWeight.Medium
                        )
                    ]

                r

            let private items model dispatch : Types.IView list =
                Section.List |> List.map (renderItem model dispatch)

            let render model dispatch =
                StackPanel.create [ StackPanel.children (items model dispatch) ]

        module FileBrowser =
            let private header =
                TextBlock.create [
                    TextBlock.text "FileSystem"
                    TextBlock.fontWeight FontWeight.Bold
                    TextBlock.margin (Thickness(8, 0, 0, 8))
                ]

            let listing (files : string list) =
                files |> List.map (fun f -> Button.create [ Button.content f ])

            let render (model : Model) dispatch =
                StackPanel.create [ StackPanel.children [] ]

    module Editor =
        module private LineNumbers =
            let render col =
                fun (model : Model) _ ->
                    TextBlock.create [
                        Grid.column col
                        TextBlock.text (model.lineNums |> String.concat "\n")
                        TextBlock.margin 4.0
                        TextBlock.verticalAlignment VerticalAlignment.Top
                    ]

        module EditorBody =
            let private onChange dispatch =
                fun contents -> dispatch (TextChanged contents)

            let private onCaret dispatch =
                fun (args : AvaloniaPropertyChangedEventArgs) ->
                    args.NewValue :?> int |> fun c -> dispatch (CaretMoved(c))

            let render col =
                fun (model : Model) dispatch ->
                    ScrollViewer.create [
                        Grid.column col
                        ScrollViewer.content [
                            TextBox.create [
                                TextBox.text model.rawContents
                                TextBox.caretIndex model.caretI
                                TextBox.acceptsReturn true
                                TextBox.onTextChanged (onChange dispatch)
                                TextBox.onPropertyChanged (onCaret dispatch)
                            ]
                        ]
                    ]

        module private StatusBar =
            let private displayPos state =
                state.Editor.Position |> fun (ln, col) -> $"Ln %d{ln}, Col %d{col}"

            let render model _ =
                TextBlock.create [
                    DockPanel.dock Dock.Bottom
                    TextBlock.text (displayPos model)
                    TextBlock.margin 4.0
                ]

        let render (model : Model) (dispatch : Message -> unit) =
            DockPanel.create [
                DockPanel.children [
                    Grid.create [
                        Grid.columnDefinitions "Auto, *"
                        Grid.children [
                            LineNumbers.render 0 model dispatch
                            EditorBody.render 1 model dispatch
                        ]
                    ]
                    StatusBar.render model dispatch
                ]
            ]

module Windows =
    open Widgets

    module Main =
        let render model dispatch =
            DockPanel.create [
                DockPanel.children [
                    Sidebar.render model dispatch
                    Content.render model dispatch
                ]
            ]
