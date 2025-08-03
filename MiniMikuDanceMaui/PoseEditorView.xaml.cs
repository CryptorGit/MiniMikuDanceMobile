using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace MiniMikuDanceMaui;

public partial class PoseEditorView : ContentView
{
    public event Action<bool>? ModeChanged;
    private bool _boneMode;

    public PoseEditorView()
    {
        InitializeComponent();
        UpdateButtons();
    }

    private void OnCameraClicked(object? sender, EventArgs e)
    {
        if (_boneMode)
        {
            _boneMode = false;
            UpdateButtons();
            ModeChanged?.Invoke(_boneMode);
        }
    }

    private void OnBoneClicked(object? sender, EventArgs e)
    {
        if (!_boneMode)
        {
            _boneMode = true;
            UpdateButtons();
            ModeChanged?.Invoke(_boneMode);
        }
    }

    private void UpdateButtons()
    {
        var active = (Color)Application.Current.Resources["TabActiveColor"];
        var inactive = (Color)Application.Current.Resources["TabInactiveColor"];
        CameraModeButton.BackgroundColor = _boneMode ? inactive : active;
        BoneModeButton.BackgroundColor = _boneMode ? active : inactive;
    }
}
