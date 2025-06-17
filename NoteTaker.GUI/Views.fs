namespace NoteTaker.Views

open System.IO
open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Media
open Avalonia.Layout
open NoteTaker
open NoteTaker.Controls


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

        let rec renderSpans (spans : MarkdownSpans) : IView list =
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
        let render (markdown : string) : IView =
            let _doc = Markdown.Parse markdown

            StackPanel.create [ StackPanel.children [] ]

    module Content =
        let render : Renderer =
            fun model _ ->
                TextBlock.create [
                    TextBlock.text $"Current View: {model.CurrentView.label}"
                    TextBlock.verticalAlignment VerticalAlignment.Center
                    TextBlock.horizontalAlignment HorizontalAlignment.Center
                ]

    module Sidebar =
        let sidebarItems _ (dispatch : Message -> unit) : List<IView> =
            Section.List
            |> List.map (fun view ->
                Button.create [
                    Button.content view.label
                    Button.onClick (fun _ -> dispatch (SelectView view))
                ])

        let render : Renderer =
            fun state dispatch ->
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
                let r (item : Section) : IView =
                    let selected = isSelected item model

                    Button.create [
                        Button.content item.label
                        Button.onClick (fun _ -> dispatch (SelectView item))
                        Button.margin (Thickness(0, 4, 0, 4))
                        Button.horizontalAlignment HorizontalAlignment.Stretch
                        Button.background (if selected then t.Base02 else t.Base01)
                        Button.foreground (if selected then t.Base0D else t.Base05)
                        Button.fontWeight (if selected then FontWeight.Bold else FontWeight.Medium)
                    ]

                r

            let private items model dispatch : IView list =
                Section.List |> List.map (renderItem model dispatch)

            let render model dispatch =
                StackPanel.create [
                    StackPanel.background model.AppTheme.Base00
                    StackPanel.children (items model dispatch)
                ]

        module FileBrowser =
            let private header =
                TextBlock.create [
                    TextBlock.text "FileSystem"
                    TextBlock.fontWeight FontWeight.Bold
                    TextBlock.margin (Thickness(8, 0, 0, 8))
                ]

            let listing (files : string list) (dispatch : Message -> unit) : IView list =
                files
                |> List.map (fun f ->
                    Button.create [
                        Button.content (Path.GetFileName f : string)
                        Button.onClick (fun _ -> dispatch (OpenFile f))
                        Button.horizontalAlignment HorizontalAlignment.Stretch
                        Button.margin (Thickness(2))
                    ])

            let render (model : Model) dispatch =
                StackPanel.create [
                    StackPanel.background model.AppTheme.Base01
                    StackPanel.children (
                        match model.Files with
                        | [] -> header :: [ TextBlock.create [ TextBlock.text "No Files" ] ]
                        | files -> header :: listing files dispatch
                    )
                ]

    module Menu =
        let private renderButton (dispatch : Message -> unit) : string * Message -> IView =
            fun (label : string, msg : Message) ->
                (Button.create [
                    Button.content label
                    Button.onClick (fun _ -> dispatch msg)
                    Button.margin (Thickness(4, 2, 4, 2))

                ])

        let private actions dispatch : IView list =
            [|
                "File", ToggleMenuButton FileButton
                "Edit", ToggleMenuButton EditButton
                "Help", ToggleMenuButton HelpButton
            |]
            |> Array.map (renderButton dispatch)
            |> Array.toList

        let render (_model : Model) dispatch =
            DockPanel.create [
                DockPanel.dock Dock.Top
                DockPanel.lastChildFill false
                DockPanel.children (actions dispatch)
            ]

open Widgets

module Windows =
    module Main =

        let render : Renderer =
            fun model dispatch ->
                DockPanel.create [
                    DockPanel.children [
                        Widgets.Menu.render model dispatch
                        Border.create [
                            Border.dock Dock.Left
                            Border.width 128
                            Border.background Brushes.DarkSlateBlue
                            Border.child (Sidebars.Navigation.render model dispatch)
                        ]
                        Border.create [
                            Border.dock Dock.Left
                            Border.width 128
                            Border.background Brushes.LightSteelBlue
                            Border.child (Sidebars.FileBrowser.render model dispatch)
                        ]

                        EditorControl.render model dispatch
                    ]
                ]
