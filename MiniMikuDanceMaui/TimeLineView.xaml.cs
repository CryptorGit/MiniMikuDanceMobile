using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using MiniMikuDance.Motion;

namespace MiniMikuDanceMaui;

public partial class TimeLineView : ContentView
{
    public event Action? AddBoneClicked;
    public event Action? AddKeyClicked;
    public event Action? EditKeyClicked;
    public event Action? DeleteKeyClicked;

    private bool _syncingScroll;

    public TimeLineView()
    {
        InitializeComponent();
        AddBoneButton.Clicked += (s, e) => AddBoneClicked?.Invoke();
        AddKeyButton.Clicked += (s, e) => AddKeyClicked?.Invoke();
        EditKeyButton.Clicked += (s, e) => EditKeyClicked?.Invoke();
        DeleteKeyButton.Clicked += (s, e) => DeleteKeyClicked?.Invoke();
        FrameScaleEntry.Text = "10";
        GridScroll.Scrolled += OnGridScrolled;
        BoneList.Scrolled += OnBoneListScrolled;
    }

    public int FrameScale
    {
        get => int.TryParse(FrameScaleEntry.Text, out var v) ? v : 10;
        set => FrameScaleEntry.Text = value.ToString();
    }

    public void SetBones(IEnumerable<string> bones)
    {
        var list = bones.ToList();
        GridView.SetBones(list);
        BoneList.ItemsSource = list;
    }

    public void SetMotion(MotionEditor? editor, MotionPlayer? player)
    {
        GridView.MotionEditor = editor;
        GridView.MotionPlayer = player;
    }

    private async void OnGridScrolled(object? sender, ScrolledEventArgs e)
    {
        if (_syncingScroll) return;
        _syncingScroll = true;
        int index = (int)(e.ScrollY / GridView.RowHeight);
        await BoneList.ScrollToAsync(index, -1, ScrollToPosition.Start, false);
        _syncingScroll = false;
    }

    private async void OnBoneListScrolled(object? sender, ItemsViewScrolledEventArgs e)
    {
        if (_syncingScroll) return;
        _syncingScroll = true;
        await GridScroll.ScrollToAsync(GridScroll.ScrollX, e.VerticalOffset, false);
        _syncingScroll = false;
    }
}
