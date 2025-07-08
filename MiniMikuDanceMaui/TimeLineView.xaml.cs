using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using MiniMikuDance.Motion;

namespace MiniMikuDanceMaui;

public partial class TimeLineView : ContentView
{
    public event Action? AddBoneClicked;
    public event Action? AddKeyClicked;
    public event Action? EditKeyClicked;
    public event Action? DeleteKeyClicked;

    public TimeLineView()
    {
        InitializeComponent();
        AddBoneButton.Clicked += (s, e) => AddBoneClicked?.Invoke();
        AddKeyButton.Clicked += (s, e) => AddKeyClicked?.Invoke();
        EditKeyButton.Clicked += (s, e) => EditKeyClicked?.Invoke();
        DeleteKeyButton.Clicked += (s, e) => DeleteKeyClicked?.Invoke();
        FrameScaleEntry.Text = "10";
    }

    public int FrameScale
    {
        get => int.TryParse(FrameScaleEntry.Text, out var v) ? v : 10;
        set => FrameScaleEntry.Text = value.ToString();
    }

    public void SetBones(IEnumerable<string> bones)
        => GridView.SetBones(bones);

    public void SetMotion(MotionEditor? editor, MotionPlayer? player)
    {
        GridView.MotionEditor = editor;
        GridView.MotionPlayer = player;
    }
}
