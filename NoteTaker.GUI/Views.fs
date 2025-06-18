namespace NoteTaker.Views

open System.IO
open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Media
open Avalonia.Layout
open FSharp.Formatting.Markdown

open NoteTaker.Model
open NoteTaker.Controls

module Widgets =
    module Preview =
        let private textBlock text fontSize =
            TextBlock.create [
                TextBlock.text text
                TextBlock.fontSize fontSize
                TextBlock.textWrapping TextWrapping.Wrap
                TextBlock.margin (0.0, 4.0)
            ]

        let rec private writeSpansPlain (spans : MarkdownSpans) : string =
            spans
            |> Seq.fold
                (fun acc span ->
                    match span with
                    | Literal(text, _) -> acc + text
                    | Strong(body, _) -> acc + writeSpansPlain body
                    | Emphasis(body, _) -> acc + writeSpansPlain body
                    | InlineCode(code, _) -> acc + code
                    | DirectLink(body, _, _, _) -> acc + writeSpansPlain body
                    | IndirectLink(body, _, _, _) -> acc + writeSpansPlain body
                    | DirectImage(altText, _, _, _) -> acc + altText
                    | IndirectImage(altText, _, _, _) -> acc + altText
                    | AnchorLink(link, _) -> acc + link
                    | HardLineBreak _ -> acc + "\n"
                    | LatexInlineMath(code, _) -> acc + code
                    | LatexDisplayMath(code, _) -> acc + code
                    | EmbedSpans _ -> acc + "[Embedded]")
                ""

        let rec private renderSpans (spans : MarkdownSpans) : IView list =
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
                | InlineCode(code, _) -> [
                    Border.create [
                        Border.background (SolidColorBrush(Color.Parse "#f5f5f5"))
                        Border.cornerRadius 3.0
                        Border.padding (2.0, 1.0)
                        Border.child (
                            TextBlock.create [
                                TextBlock.text code
                                TextBlock.fontFamily "Consolas, Menlo, Monaco, monospace"
                                TextBlock.fontSize 13.0
                                TextBlock.foreground (SolidColorBrush(Color.Parse "#333"))
                            ]
                        )
                    ]
                  ]
                | Emphasis(body, _) -> [
                    StackPanel.create [
                        StackPanel.orientation Orientation.Horizontal
                        StackPanel.children (renderSpans body)
                        StackPanel.fontStyle FontStyle.Italic
                    ]
                  ]
                | AnchorLink(link, _) -> [
                    TextBlock.create [
                        TextBlock.text link
                        TextBlock.foreground Brushes.DodgerBlue
                        TextBlock.cursor Avalonia.Input.Cursor.Default
                        TextBlock.textDecorations TextDecorations.Underline
                    ]
                  ]
                | DirectLink(body, link, _, _) -> [
                    TextBlock.create [
                        TextBlock.text (writeSpansPlain body)
                        TextBlock.foreground Brushes.DodgerBlue
                        TextBlock.cursor Avalonia.Input.Cursor.Default
                        TextBlock.textDecorations TextDecorations.Underline
                        ToolTip.tip link
                    ]
                  ]
                | IndirectLink(body, _, _, _) -> [
                    TextBlock.create [
                        TextBlock.text (writeSpansPlain body)
                        TextBlock.foreground Brushes.DodgerBlue
                        TextBlock.cursor Avalonia.Input.Cursor.Default
                        TextBlock.textDecorations TextDecorations.Underline
                    ]
                  ]
                | DirectImage(body, link, title, _) -> [
                    StackPanel.create [
                        StackPanel.orientation Orientation.Vertical
                        StackPanel.children [
                            TextBlock.create [
                                TextBlock.text $"[Image: {body}]"
                                TextBlock.foreground Brushes.Gray
                                TextBlock.fontStyle FontStyle.Italic
                                ToolTip.tip $"Image URL: {link}"
                            ]
                            match title with
                            | Some t ->
                                TextBlock.create [
                                    TextBlock.text t
                                    TextBlock.fontSize 12.0
                                    TextBlock.foreground Brushes.Gray
                                ]
                            | None -> ()
                        ]
                    ]
                  ]
                | IndirectImage(body, _, _, _) -> [
                    TextBlock.create [
                        TextBlock.text $"[Image: {body}]"
                        TextBlock.foreground Brushes.Gray
                        TextBlock.fontStyle FontStyle.Italic
                    ]
                  ]
                | HardLineBreak _ -> [ TextBlock.create [ TextBlock.text "\n" ] ]
                | LatexInlineMath(code, _) -> [
                    Border.create [
                        Border.background (SolidColorBrush(Color.Parse "#fff8dc"))
                        Border.cornerRadius 3.0
                        Border.padding (2.0, 1.0)
                        Border.child (
                            TextBlock.create [
                                TextBlock.text $"${code}$"
                                TextBlock.fontFamily "Consolas, Menlo, Monaco, monospace"
                                TextBlock.fontSize 13.0
                                TextBlock.foreground (SolidColorBrush(Color.Parse "#8b4513"))
                            ]
                        )
                    ]
                  ]
                | LatexDisplayMath(code, _) -> [
                    Border.create [
                        Border.background (SolidColorBrush(Color.Parse "#fff8dc"))
                        Border.cornerRadius 3.0
                        Border.padding (8.0, 4.0)
                        Border.margin (0.0, 8.0)
                        Border.child (
                            TextBlock.create [
                                TextBlock.text $"$${code}$$"
                                TextBlock.fontFamily "Consolas, Menlo, Monaco, monospace"
                                TextBlock.fontSize 14.0
                                TextBlock.foreground (SolidColorBrush(Color.Parse "#8b4513"))
                                TextBlock.textAlignment TextAlignment.Center
                            ]
                        )
                    ]
                  ]
                | EmbedSpans _ -> [
                    TextBlock.create [
                        TextBlock.text "[Embedded Content]"
                        TextBlock.foreground Brushes.Gray
                        TextBlock.fontStyle FontStyle.Italic
                    ]
                  ])

        let rec private renderParagraph (par : MarkdownParagraph) : IView list =
            match par with
            | Heading(size = 1; body = spans) -> [ textBlock (writeSpansPlain spans) 26.0 ]
            | Heading(size = 2; body = spans) -> [ textBlock (writeSpansPlain spans) 22.0 ]
            | Heading(size = 3; body = spans) -> [ textBlock (writeSpansPlain spans) 18.0 ]
            | Heading(size = size; body = spans) -> [
                textBlock (writeSpansPlain spans) (max 14.0 (20.0 - float (size - 3) * 2.0))
              ]
            | Paragraph(body, _) -> renderSpans body
            | ListBlock(Unordered, items, _) ->
                items
                |> List.collect (fun item ->
                    let txt = item |> List.collect renderParagraph

                    [
                        StackPanel.create [
                            StackPanel.orientation Orientation.Horizontal
                            StackPanel.children (textBlock "• " 14.0 :: txt)
                            StackPanel.margin (16.0, 0.0, 0.0, 0.0)
                        ]
                    ])
            | ListBlock(Ordered, items, _) ->
                items
                |> List.mapi (fun index item ->
                    let txt = item |> List.collect renderParagraph

                    StackPanel.create [
                        StackPanel.orientation Orientation.Horizontal
                        StackPanel.children (textBlock $"{index + 1}. " 14.0 :: txt)
                        StackPanel.margin (16.0, 0.0, 0.0, 0.0)
                    ])
                |> List.map (fun x -> x :> IView)
            | CodeBlock(code, _, _, _, _, _) -> [
                Border.create [
                    Border.background (SolidColorBrush(Color.Parse "#1e1e1e"))
                    Border.cornerRadius 4.0
                    Border.padding 8.0
                    Border.margin (0.0, 8.0)
                    Border.child (
                        TextBlock.create [
                            TextBlock.text code
                            TextBlock.foreground Brushes.White
                            TextBlock.fontFamily "Consolas, Menlo, Monaco, monospace"
                            TextBlock.textWrapping TextWrapping.Wrap
                        ]
                    )
                ]
              ]
            | QuotedBlock(paragraphs, _) -> [
                Border.create [
                    Border.borderBrush (SolidColorBrush(Color.Parse "#ccc"))
                    Border.borderThickness (4.0, 0.0, 0.0, 0.0)
                    Border.padding (16.0, 8.0, 8.0, 8.0)
                    Border.margin (0.0, 8.0)
                    Border.background (SolidColorBrush(Color.Parse "#f9f9f9"))
                    Border.child (
                        StackPanel.create [
                            StackPanel.children (paragraphs |> List.collect renderParagraph)
                        ]
                    )
                ]
              ]
            | HorizontalRule _ -> [
                Border.create [
                    Border.height 1.0
                    Border.background (SolidColorBrush(Color.Parse "#ccc"))
                    Border.margin (0.0, 16.0)
                ]
              ]
            | TableBlock _ -> [
                TextBlock.create [
                    TextBlock.text "[Table - not yet supported]"
                    TextBlock.foreground Brushes.Gray
                    TextBlock.fontStyle FontStyle.Italic
                ]
              ]
            | _ -> []

        let render (markdown : string) : IView =
            Markdown.Parse markdown
            |> _.Paragraphs
            |> Seq.toList
            |> List.collect renderParagraph
            |> StackPanel.children
            |> fun children -> StackPanel.create [ children; StackPanel.margin 16.0 ]
            |> fun panel -> ScrollViewer.create [ ScrollViewer.content panel ]

    module Sidebars =
        module Navigation =
            let private isSelected view state = state.CurrentView = view
            let private getTheme model = model.AppTheme

            let private renderItem (model : Model) dispatch (item : Section) : IView =
                let selected = isSelected item model

                let t = getTheme model
                let bg = if selected then t.Base02 else Colors.Transparent
                let fg = if selected then t.Base0D else t.Base05
                let borderT = if selected then Thickness(2, 0, 0, 0) else Thickness(0)
                let borderB = if selected then SolidColorBrush t.Base0D else null
                let label = item.label
                let handler _ = item |> SelectView |> dispatch

                Button.create [
                    Button.content label
                    Button.onClick handler
                    Button.margin (Thickness(8, 2, 8, 2))
                    Button.padding (Thickness(12, 8, 12, 8))
                    Button.horizontalAlignment HorizontalAlignment.Stretch
                    Button.background bg
                    Button.foreground fg
                    Button.fontWeight (if selected then FontWeight.Bold else FontWeight.Normal)
                    Button.fontSize 13.0
                    Button.borderThickness borderT
                    Button.borderBrush borderB
                    Button.cornerRadius 4.0
                ]


            let private items model dispatch : IView list =
                Section.List |> List.map (renderItem model dispatch)

            let private header model =
                TextBlock.create [
                    TextBlock.text "Navigation"
                    TextBlock.fontWeight FontWeight.Bold
                    TextBlock.fontSize 14.0
                    TextBlock.foreground model.AppTheme.Base04
                    TextBlock.margin (Thickness(12, 12, 12, 8))
                ]

            let render model dispatch =
                StackPanel.create [
                    StackPanel.background model.AppTheme.Base01
                    StackPanel.children (header model :: items model dispatch)
                    StackPanel.margin (Thickness(0, 0, 1, 0))
                ]

        module FileBrowser =
            let private header model =
                TextBlock.create [
                    TextBlock.text "Files"
                    TextBlock.fontWeight FontWeight.Bold
                    TextBlock.fontSize 14.0
                    TextBlock.foreground model.AppTheme.Base04
                    TextBlock.margin (Thickness(12, 12, 12, 8))
                ]

            let private fileButton
                (fileName : string)
                (filePath : string)
                (model : Model)
                (dispatch : Message -> unit)
                : IView =
                Button.create [
                    Button.content fileName
                    Button.onClick (fun _ -> dispatch (OpenFile filePath))
                    Button.horizontalAlignment HorizontalAlignment.Stretch
                    Button.margin (Thickness(8, 1, 8, 1))
                    Button.padding (Thickness(12, 6, 12, 6))
                    Button.background Colors.Transparent
                    Button.foreground model.AppTheme.Base05
                    Button.fontSize 12.0
                    Button.cornerRadius 3.0
                    Button.onPointerEntered (fun _ -> ())
                    ToolTip.tip filePath
                ]

            let listing
                (files : string list)
                (model : Model)
                (dispatch : Message -> unit)
                : IView list =
                files |> List.map (fun f -> fileButton (Path.GetFileName f) f model dispatch)

            let private emptyState model =
                StackPanel.create [
                    StackPanel.children [
                        TextBlock.create [
                            TextBlock.text "No files"
                            TextBlock.foreground model.AppTheme.Base03
                            TextBlock.fontSize 12.0
                            TextBlock.fontStyle FontStyle.Italic
                            TextBlock.horizontalAlignment HorizontalAlignment.Center
                            TextBlock.margin (Thickness(0, 16, 0, 0))
                        ]
                    ]
                ]

            let render (model : Model) dispatch =
                StackPanel.create [
                    StackPanel.background model.AppTheme.Base01
                    StackPanel.children (
                        match model.Files with
                        | [] -> [ header model; emptyState model ]
                        | files -> header model :: listing files model dispatch
                    )
                    StackPanel.margin (Thickness(1, 0, 0, 0))
                ]

    module Menu =

        let private renderButton
            (model : Model)
            (dispatch : Message -> unit)
            : string * Message -> IView =
            fun (label : string, msg : Message) ->
                Button.create [
                    Button.content label
                    Button.onClick (fun _ -> dispatch msg)
                    Button.margin (Thickness(4, 4, 4, 4))
                    Button.padding (Thickness(12, 6, 12, 6))
                    Button.background Colors.Transparent
                    Button.foreground model.AppTheme.Base05
                    Button.fontSize 13.0
                    Button.cornerRadius 3.0
                    Button.borderThickness (Thickness(0))
                ]

        let private actions model dispatch : IView list =
            [|
                "File", ToggleMenuButton FileButton
                "Edit", ToggleMenuButton EditButton
                "Help", ToggleMenuButton HelpButton
            |]
            |> Array.map (renderButton model dispatch)
            |> Array.toList

        let private modeDropdown model dispatch =
            Border.create [
                Border.background model.AppTheme.Base01
                Border.cornerRadius 4.0
                Border.padding (Thickness(1))
                Border.margin (Thickness(8, 4, 8, 4))
                Border.child (
                    ComboBox.create [
                        ComboBox.dataItems (EditorMode.List |> List.map box)
                        ComboBox.itemTemplate (
                            DataTemplateView.create<_, _> (fun (mode : EditorMode) ->
                                TextBlock.create [
                                    TextBlock.text (mode.ToString())
                                    TextBlock.foreground model.AppTheme.Base05
                                    TextBlock.padding (Thickness(8, 4))
                                ]
                                :> IView)
                        )
                        ComboBox.selectedItem (box model.Editor.Mode)
                        ComboBox.onSelectedItemChanged (fun args ->
                            match args with
                            | :? EditorMode as mode -> dispatch (ChangeEditorMode mode)
                            | _ -> ())
                        ComboBox.maxDropDownHeight 120.0
                        ComboBox.background model.AppTheme.Base00
                        ComboBox.foreground model.AppTheme.Base05
                        ComboBox.fontSize 13.0
                        ComboBox.minWidth 100.0
                    ]
                )
            ]

        let private settingsButton model =
            Button.create [
                Button.content "Settings"
                Button.onClick (fun _ -> ())
                Button.margin (Thickness(4, 4, 8, 4))
                Button.padding (Thickness(12, 6, 12, 6))
                Button.background Colors.Transparent
                Button.foreground model.AppTheme.Base05
                Button.fontSize 13.0
                Button.cornerRadius 3.0
                Button.borderThickness (Thickness(0))
            ]

        let render (model : Model) dispatch =
            Border.create [
                Border.dock Dock.Top
                Border.background model.AppTheme.Base01
                Border.borderThickness (Thickness(0, 0, 0, 1))
                Border.borderBrush (SolidColorBrush(model.AppTheme.Base02))
                Border.child (
                    DockPanel.create [
                        DockPanel.lastChildFill false
                        DockPanel.margin (Thickness(8, 0, 8, 0))
                        DockPanel.children (
                            actions model dispatch
                            @ [ modeDropdown model dispatch; settingsButton model ]
                        )
                    ]
                )
            ]

module Windows =
    open Widgets

    module Main =
        let render : Renderer =
            fun model dispatch ->
                DockPanel.create [
                    DockPanel.children [
                        Menu.render model dispatch
                        Border.create [
                            Border.dock Dock.Left
                            Border.minWidth 180
                            Border.maxWidth 220
                            Border.width 200
                            Border.background (SolidColorBrush(model.AppTheme.Base01))
                            Border.child (Sidebars.Navigation.render model dispatch)
                        ]
                        Border.create [
                            Border.dock Dock.Left
                            Border.minWidth 180
                            Border.maxWidth 220
                            Border.width 200
                            Border.background (SolidColorBrush(model.AppTheme.Base01))
                            Border.child (Sidebars.FileBrowser.render model dispatch)
                        ]

                        match model.Editor.Mode with
                        | Preview -> Preview.render model.Editor.Content
                        | _ -> EditorControl.render model dispatch
                    ]
                ]
