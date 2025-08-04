using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using SkiaSharp;

namespace MiniMikuDanceMaui;

public partial class PoseEditorView : ContentView
{
    public event Action<bool>? ModeChanged;
    private bool _boneMode;
    public PmxRenderer? Renderer { get; set; }
    private int _selectedIkBone = -1;

    public PoseEditorView()
    {
        InitializeComponent();
        ModeChanged += mode =>
        {
            if (Renderer != null)
                Renderer.ShowIkBones = mode;
        };
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
            // TODO: Renderer 内の IK ボーン位置を利用したヒットテストを実装する
            _selectedIkBone = -1; // 仮: 未実装
        }
        else if (e.ActionType == SKTouchAction.Moved && _selectedIkBone >= 0)
        {
            // TODO: カメラ位置と IK ボーンを結ぶ平面へポインタを投影して新しい IK 目標座標を求める
        }
        else if (e.ActionType == SKTouchAction.Released || e.ActionType == SKTouchAction.Cancelled)
        {
            _selectedIkBone = -1;
        }

        e.Handled = true;
    }
}
