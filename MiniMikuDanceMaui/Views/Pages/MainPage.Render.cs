using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using OpenTK.Graphics.ES30;
using GL = OpenTK.Graphics.ES30.GL;
using System;
using System.Diagnostics;
using System.Linq;
using MiniMikuDance.IK;
using MiniMikuDanceMaui.Rendering;

namespace MiniMikuDanceMaui.Views.Pages;

public partial class MainPage
{
    private void UpdateRendererLightingProperties()
    {
        _renderer.ShadeShift = _shadeShift;
        _renderer.ShadeToony = _shadeToony;
        _renderer.RimIntensity = _rimIntensity;
    }

    private void OnPaintSurface(object? sender, SKPaintGLSurfaceEventArgs e)
    {
        if (!_glInitialized)
        {
            GL.LoadBindings(new SKGLViewBindingsContext());
            _renderer.Initialize();
            _renderer.BonesConfig = _bonesConfig;
            _glInitialized = true;
        }

        LoadPendingModel();
        _renderer.Render(e.Surface.Canvas, e.BackendRenderTarget, e.Info);
        GL.Flush();
        _needsRender = false;
    }

    private void LoadPendingModel()
    {
        if (_pendingModel != null)
        {
            IkManager.Clear();
            _renderer.ClearIkBones();
            _renderer.ClearBoneRotations();
            _renderer.LoadModel(_pendingModel);
            _currentModel = _pendingModel;
            _shadeShift = _pendingModel.ShadeShift;
            _shadeToony = _pendingModel.ShadeToony;
            _rimIntensity = _pendingModel.RimIntensity;
            UpdateRendererLightingProperties();
            _pendingModel = null;

            if (_poseMode && _currentModel != null)
            {
                IkManager.LoadPmxIkBones(_currentModel.Bones);
                try
                {
                    var ikBones = IkManager.Bones.Values;
                    if (ikBones.Any())
                    {
                        _renderer.SetIkBones(ikBones);
                    }
                    else
                    {
                        _renderer.ClearIkBones();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
                IkManager.PickFunc = _renderer.PickBone;
                IkManager.GetBonePositionFunc = _renderer.GetBoneWorldPosition;
                IkManager.GetCameraPositionFunc = _renderer.GetCameraPosition;
                IkManager.SetBoneTranslation = _renderer.SetBoneTranslation;
                IkManager.ToModelSpaceFunc = _renderer.WorldToModel;
                IkManager.ToWorldSpaceFunc = _renderer.ModelToWorld;
            }
        }
    }

    private void OnViewTouch(object? sender, SKTouchEventArgs e)
    {
        if (_poseMode)
        {
            try
            {
                switch (e.ActionType)
                {
                    case SKTouchAction.Pressed:
                        IkManager.PickBone(e.Location.X, e.Location.Y);
                        if (!_poseMode)
                        {
                            e.Handled = true;
                            return;
                        }
                        break;
                    case SKTouchAction.Moved:
                        if (!_poseMode)
                        {
                            e.Handled = true;
                            return;
                        }
                        var ray = _renderer.ScreenPointToRay(e.Location.X, e.Location.Y);
                        var pos = IkManager.IntersectDragPlane(ray);
                        if (!_poseMode)
                        {
                            e.Handled = true;
                            return;
                        }
                        if (pos.HasValue && IkManager.SelectedBoneIndex >= 0)
                        {
                            IkManager.UpdateTarget(IkManager.SelectedBoneIndex, pos.Value);
                        }
                        break;
                    case SKTouchAction.Released:
                    case SKTouchAction.Cancelled:
                        IkManager.ReleaseSelection();
                        if (!_poseMode)
                        {
                            e.Handled = true;
                            return;
                        }
                        break;
                }
                if (!_poseMode)
                {
                    e.Handled = true;
                    return;
                }
                IkManager.InvalidateViewer?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                IkManager.ReleaseSelection();
                MainThread.BeginInvokeOnMainThread(async () =>
                    await DisplayAlert("Error", ex.Message, "OK"));
            }
            e.Handled = true;
            return;
        }

        if (_viewMenuOpen || _settingMenuOpen)
        {
            SetMenuVisibility(ref _viewMenuOpen, ViewMenu, false);
            SetMenuVisibility(ref _settingMenuOpen, SettingMenu, false);
            UpdateLayout();
        }

        if (e.ActionType == SKTouchAction.Pressed)
        {
            _touchPoints[e.Id] = e.Location;
        }
        else if (e.ActionType == SKTouchAction.Moved)
        {
            if (_touchPoints.TryGetValue(e.Id, out var prev))
            {
                _touchPoints[e.Id] = e.Location;

                if (_touchPoints.Count == 1)
                {
                    var dx = e.Location.X - prev.X;
                    var dy = e.Location.Y - prev.Y;
                    _renderer.Orbit(dx, dy);
                }
                else if (_touchPoints.Count == 2)
                {
                    var index = 0;
                    foreach (var id in _touchPoints.Keys)
                    {
                        _touchIds[index++] = id;
                    }

                    var id0 = _touchIds[0];
                    var id1 = _touchIds[1];

                    var p0Old = id0 == e.Id ? prev : _touchPoints[id0];
                    var p1Old = id1 == e.Id ? prev : _touchPoints[id1];
                    var p0New = _touchPoints[id0];
                    var p1New = _touchPoints[id1];

                    var oldMid = new SKPoint((p0Old.X + p1Old.X) / 2, (p0Old.Y + p1Old.Y) / 2);
                    var newMid = new SKPoint((p0New.X + p1New.X) / 2, (p0New.Y + p1New.Y) / 2);
                    _renderer.Pan(newMid.X - oldMid.X, newMid.Y - oldMid.Y);
                    float oldDist = (p0Old - p1Old).Length;
                    float newDist = (p0New - p1New).Length;
                    _renderer.Dolly(newDist - oldDist);
                }
            }
            else
            {
                _touchPoints[e.Id] = e.Location;
            }
        }
        else if (e.ActionType == SKTouchAction.Released || e.ActionType == SKTouchAction.Cancelled)
        {
            _touchPoints.Remove(e.Id);
        }
        e.Handled = true;
        _needsRender = true;
    }
}

