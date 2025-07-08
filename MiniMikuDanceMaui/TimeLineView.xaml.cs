using Microsoft.Maui.Controls;
using System;

namespace MiniMikuDanceMaui;

public partial class TimeLineView : ContentView
{
    public TimeLineView()
    {
        InitializeComponent();
    }

    public void ShowAddOverlay()
    {
        Overlay.IsVisible = true;
        AddWindow.IsVisible = true;
        EditWindow.IsVisible = false;
        DeleteWindow.IsVisible = false;
    }

    public void ShowEditOverlay()
    {
        Overlay.IsVisible = true;
        AddWindow.IsVisible = false;
        EditWindow.IsVisible = true;
        DeleteWindow.IsVisible = false;
    }

    public void ShowDeleteOverlay()
    {
        Overlay.IsVisible = true;
        AddWindow.IsVisible = false;
        EditWindow.IsVisible = false;
        DeleteWindow.IsVisible = true;
    }

    public void HideOverlay()
    {
        Overlay.IsVisible = false;
        AddWindow.IsVisible = false;
        EditWindow.IsVisible = false;
        DeleteWindow.IsVisible = false;
    }

    private void OnAddClicked(object? sender, EventArgs e) => ShowAddOverlay();
    private void OnEditClicked(object? sender, EventArgs e) => ShowEditOverlay();
    private void OnDeleteClicked(object? sender, EventArgs e) => ShowDeleteOverlay();
    private void OnCancelClicked(object? sender, EventArgs e) => HideOverlay();

    private void OnAddConfirmClicked(object? sender, EventArgs e)
    {
        // TODO: 実際の追加処理を実装する
        HideOverlay();
    }

    private void OnEditConfirmClicked(object? sender, EventArgs e)
    {
        // TODO: 編集処理を実装する
        HideOverlay();
    }

    private void OnDeleteConfirmClicked(object? sender, EventArgs e)
    {
        // TODO: 削除処理を実装する
        HideOverlay();
    }

    private void OnAddSeqChanged(object? sender, ValueChangedEventArgs e)
        => AddSeqEntry.Text = ((int)e.NewValue).ToString();

    private void OnEditSeqChanged(object? sender, ValueChangedEventArgs e)
        => EditSeqEntry.Text = ((int)e.NewValue).ToString();
}
