namespace NoteTaker.Views

open Avalonia.Controls
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open NoteTaker


module Widgets =
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
