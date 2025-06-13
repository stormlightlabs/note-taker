namespace NoteTaker.Views

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Media
open Avalonia.Layout
open NoteTaker

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
            Views.List
            |> List.map (fun view ->
                Button.create [
                    Button.content view.label
                    Button.onClick (fun _ -> dispatch (SelectView view))
                ])

        let render state dispatch =
            StackPanel.create [ StackPanel.children (sidebarItems state dispatch) ]

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
