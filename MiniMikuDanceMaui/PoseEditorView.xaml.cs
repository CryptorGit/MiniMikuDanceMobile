using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using SkiaSharp;
using Vector2 = OpenTK.Mathematics.Vector2;
using Vector3 = OpenTK.Mathematics.Vector3;

namespace MiniMikuDanceMaui;

public partial class PoseEditorView : ContentView
{
    public event Action<bool>? ModeChanged;
    public event Action<IList<Vector3>, IList<Vector3>>? PoseChanged;
    private bool _boneMode;
    public PmxRenderer? Renderer { get; set; }
    private int _selectedIkBone = -1;
    private Vector3 _ikPlanePoint;

    public PoseEditorView()
    {
        InitializeComponent();
        UpdateButtons();
    }

    /// <summary>
    /// Bone モードの状態を外部から切り替える。
    /// </summary>
    /// <param name="enable">true で Bone モード、false で Camera モード。</param>
    public void SetBoneMode(bool enable)
    {
        if (_boneMode == enable)
            return;

        _boneMode = enable;
        UpdateButtons();
        ModeChanged?.Invoke(_boneMode);
    }

    private void OnCameraClicked(object? sender, EventArgs e)
    {
        if (_boneMode)
            SetBoneMode(false);
    }

    private void OnBoneClicked(object? sender, EventArgs e)
    {
        if (!_boneMode)
            SetBoneMode(true);
    }

    private void UpdateButtons()
    {
        var active = (Color)Application.Current.Resources["TabActiveColor"];
        var inactive = (Color)Application.Current.Resources["TabInactiveColor"];
        CameraModeButton.BackgroundColor = _boneMode ? inactive : active;
        BoneModeButton.BackgroundColor = _boneMode ? active : inactive;
    }

    private void OnIkBoneRadiusEntryCompleted(object? sender, EventArgs e)
    {
        if (Renderer == null)
            return;

        if (float.TryParse(IkBoneRadiusEntry.Text, out var v) && v > 0f)
        {
            Renderer.IkBoneRadius = v;
            Renderer.RebuildIkBoneMesh();
        }
        else
        {
            IkBoneRadiusEntry.Text = Renderer.IkBoneRadius.ToString();
        }
    }

    /// <summary>
    /// Viewer のタッチイベントを処理する。
    /// Bone モード時のみ IK ボーンのヒットテストとドラッグ処理を行う。
    /// </summary>
    /// <param name="e">タッチイベント。</param>
    public void HandleViewerTouch(SKTouchEventArgs e)
    {
        if (!_boneMode || Renderer == null)
            return;

        if (e.ActionType == SKTouchAction.Pressed)
        {
            var touch = new Vector2((float)e.Location.X, (float)e.Location.Y);
            _selectedIkBone = -1;
            float min = float.MaxValue;
            foreach (var (idx, world, screen) in Renderer.GetIkBonePositions())
            {
                float d = Vector2.Distance(touch, screen);
                if (d < min)
                {
                    min = d;
                    _selectedIkBone = idx;
                    _ikPlanePoint = world;
                }
            }
        }
        else if (e.ActionType == SKTouchAction.Moved && _selectedIkBone >= 0)
        {
            var pos = Renderer.ProjectScreenPointToViewPlane((float)e.Location.X, (float)e.Location.Y, _ikPlanePoint);
            Renderer.SetIkTargetPosition(_selectedIkBone, pos);
            Renderer.Render();
            EmitPose();
        }
        else if (e.ActionType == SKTouchAction.Released || e.ActionType == SKTouchAction.Cancelled)
        {
            _selectedIkBone = -1;
            Renderer.Render();
            EmitPose();
        }

        e.Handled = true;
    }

    private void EmitPose()
    {
        if (Renderer == null)
            return;
        var rotations = Renderer.GetAllBoneRotations();
        var translations = Renderer.GetAllBoneTranslations();
        PoseChanged?.Invoke(rotations, translations);
    }

    public void RefreshIkGoalList()
    {
        if (Renderer == null)
            return;
        IkGoalList.Children.Clear();
        foreach (var (idx, name, enabled) in Renderer.GetIkGoals())
        {
            var sw = new Switch { IsToggled = enabled };
            sw.Toggled += (s, e) =>
            {
                Renderer.SetIkGoalEnabled(idx, e.Value);
                Renderer.Render();
            };
            var lbl = new Label
            {
                Text = name,
                TextColor = (Color)Application.Current.Resources["TextColor"],
                VerticalOptions = LayoutOptions.Center
            };
            var hs = new HorizontalStackLayout { Spacing = 6 };
            hs.Children.Add(sw);
            hs.Children.Add(lbl);
            IkGoalList.Children.Add(hs);
        }
    }
}
