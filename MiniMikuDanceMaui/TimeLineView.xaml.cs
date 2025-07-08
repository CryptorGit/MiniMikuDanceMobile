using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using MiniMikuDance.Motion;

namespace MiniMikuDanceMaui;

public partial class TimeLineView : ContentView
{
    public event Action? AddBoneClicked;
    public event Action? AddKeyClicked;
    public event Action? EditKeyClicked;
    public event Action? DeleteKeyClicked;
    public event Action? CloseClicked;
    public event Action? PlayClicked;
    public event Action? PauseClicked;
    public event Action? StopClicked;

    private bool _syncingScroll;

    public TimeLineView()
    {
        LogService.WriteLine("TimeLineView constructor");
        InitializeComponent();
        AddBoneButton.Clicked += (s, e) => AddBoneClicked?.Invoke();
        AddKeyButton.Clicked += (s, e) => AddKeyClicked?.Invoke();
        EditKeyButton.Clicked += (s, e) => EditKeyClicked?.Invoke();
        DeleteKeyButton.Clicked += (s, e) => DeleteKeyClicked?.Invoke();
        CloseButton.Clicked += OnCloseClicked;
        PlayButton.Clicked += (s, e) => PlayClicked?.Invoke();
        PauseButton.Clicked += (s, e) => PauseClicked?.Invoke();
        StopButton.Clicked += (s, e) => StopClicked?.Invoke();
        GridScroll.Scrolled += OnGridScrolled;
        BoneList.Scrolled += OnBoneListScrolled;
    }

    public void SetBones(IEnumerable<string> bones)
    {
        LogService.WriteLine($"SetBones count={bones.Count()}");
        var list = bones.ToList();
        GridView.SetBones(list);
        BoneList.ItemsSource = list;
    }

    public int AddBone(string bone)
    {
        LogService.WriteLine($"AddBone {bone}");
        return GridView.AddBone(bone);
    }

    public Task ScrollToRowAsync(int index)
        => GridScroll.ScrollToAsync(0, index * GridView.RowHeight, true);

    public void SetMotion(MotionEditor? editor, MotionPlayer? player)
    {
        LogService.WriteLine("SetMotion called");
        GridView.MotionEditor = editor;
        GridView.MotionPlayer = player;
    }

    private async void OnGridScrolled(object? sender, ScrolledEventArgs e)
    {
        LogService.WriteLine($"GridScrolled {e.ScrollX},{e.ScrollY}");
        if (_syncingScroll) return;
        _syncingScroll = true;
        int index = (int)(e.ScrollY / GridView.RowHeight);
        await BoneList.ScrollToAsync(index, -1, ScrollToPosition.Start, false);
        _syncingScroll = false;
    }

    private async void OnBoneListScrolled(object? sender, ItemsViewScrolledEventArgs e)
    {
        LogService.WriteLine($"BoneListScrolled {e.HorizontalOffset},{e.VerticalOffset}");
        if (_syncingScroll) return;
        _syncingScroll = true;
        await GridScroll.ScrollToAsync(GridScroll.ScrollX, e.VerticalOffset, false);
        _syncingScroll = false;
    }

    private void OnCloseClicked(object? sender, EventArgs e)
    {
        CloseClicked?.Invoke();
    }
}
