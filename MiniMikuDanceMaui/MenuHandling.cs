using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using MiniMikuDance.Import;

namespace MiniMikuDanceMaui;

public partial class MainPage
{
    // 下部領域1 : 上部領域2 の比率となるよう初期値を設定
    private double _bottomHeightRatio = 1.0 / 3.0;
    private bool _viewMenuOpen;
    private bool _settingMenuOpen;
    private bool _fileMenuOpen;

    private void UpdateOverlay() => MenuOverlay.IsVisible = _viewMenuOpen || _settingMenuOpen || _fileMenuOpen;
    private readonly Dictionary<string, View> _bottomViews = new();
    private readonly Dictionary<string, Border> _bottomTabs = new();
    private string? _currentFeature;

    private void SetMenuVisibility(ref bool flag, View menu, bool visible)
    {
        flag = visible;
        menu.IsVisible = visible;
        UpdateOverlay();
    }

    private void ShowViewMenu()
    {
        SetMenuVisibility(ref _settingMenuOpen, SettingMenu, false);
        SetMenuVisibility(ref _fileMenuOpen, FileMenu, false);
        SetMenuVisibility(ref _viewMenuOpen, ViewMenu, true);
    }

    private void OnViewMenuTapped(object? sender, TappedEventArgs e)
    {
        var visible = !_viewMenuOpen;
        HideAllMenus();
        SetMenuVisibility(ref _viewMenuOpen, ViewMenu, visible);
        UpdateLayout();
    }

    private void ShowSettingMenu()
    {
        SetMenuVisibility(ref _viewMenuOpen, ViewMenu, false);
        SetMenuVisibility(ref _fileMenuOpen, FileMenu, false);
        SetMenuVisibility(ref _settingMenuOpen, SettingMenu, true);
        if (SettingContent is SettingView sv)
        {
            sv.HeightRatio = _bottomHeightRatio;
            sv.RotateSensitivity = _renderer.RotateSensitivity;
            sv.PanSensitivity = _renderer.PanSensitivity;
            sv.IkBoneSize = _renderer.IkBoneScale;
            sv.BonePickPixels = _renderer.BonePickPixels;
            sv.LockTranslation = _settings.Physics.LockTranslation;
        }
    }

    private void OnSettingMenuTapped(object? sender, EventArgs e)
    {
        var visible = !_settingMenuOpen;
        HideAllMenus();
        SetMenuVisibility(ref _settingMenuOpen, SettingMenu, visible);
        UpdateLayout();
    }

    private void ShowFileMenu()
    {
        SetMenuVisibility(ref _viewMenuOpen, ViewMenu, false);
        SetMenuVisibility(ref _settingMenuOpen, SettingMenu, false);
        SetMenuVisibility(ref _fileMenuOpen, FileMenu, true);
    }

    private void OnFileMenuTapped(object? sender, TappedEventArgs e)
    {
        var visible = !_fileMenuOpen;
        HideAllMenus();
        SetMenuVisibility(ref _fileMenuOpen, FileMenu, visible);
        UpdateLayout();
    }

    private void OnBoneClicked(object? sender, EventArgs e)
    {
        ShowBottomFeature("BONE");
        HideAllMenus();
    }

    private void OnLightingClicked(object? sender, EventArgs e)
    {
        ShowBottomFeature("Lighting");
        HideAllMenus();
    }

    private void OnMorphClicked(object? sender, EventArgs e)
    {
        ShowBottomFeature("MORPH");
        HideAllMenus();
    }

    private void OnPhysicsClicked(object? sender, EventArgs e)
    {
        ShowBottomFeature("PHYSICS");
        HideAllMenus();
    }

    private void OnCloseBottomTapped(object? sender, TappedEventArgs e)
    {
        if (_currentFeature != null)
        {
            RemoveBottomFeature(_currentFeature);
        }
        else
        {
            HideBottomRegion();
        }
        HideAllMenus();
    }

    private void OnOverlayTapped(object? sender, TappedEventArgs e)
    {
        HideAllMenus();
    }

    private void OnBottomRegionTapped(object? sender, TappedEventArgs e)
    {
        HideAllMenus();
    }

    private void HideAllMenus()
    {
        SetMenuVisibility(ref _viewMenuOpen, ViewMenu, false);
        SetMenuVisibility(ref _settingMenuOpen, SettingMenu, false);
        SetMenuVisibility(ref _fileMenuOpen, FileMenu, false);
        UpdateLayout();
    }

    private void HideBottomRegion()
    {
        BottomRegion.IsVisible = false;
        _currentFeature = null;
        UpdateTabColors();
    }

    private void UpdateSettingViewProperties(SettingView? sv)
    {
        if (sv == null || _renderer == null)
            return;

        sv.HeightRatio = _bottomHeightRatio;
        sv.RotateSensitivity = _rotateSensitivity;
        sv.PanSensitivity = _panSensitivity;
        sv.ZoomSensitivity = _renderer.ZoomSensitivity;
        sv.ShowBoneOutline = _renderer.ShowBoneOutline;
        sv.LockTranslation = _settings.Physics.LockTranslation;
    }

    private void UpdateBoneViewProperties(BoneView? bv)
    {
        if (bv == null || _currentModel?.Bones == null)
            return;

        var list = _currentModel.Bones.Select(b => b.Name).ToList();
        bv.SetBones(list);
    }

    private void SetupBoneView(BoneView bv)
    {
        UpdateBoneViewProperties(bv);
        bv.BoneTapped += idx => _renderer.SelectedBoneIndex = idx;
        _renderer.BoneSelectionChanged += bv.HighlightBone;
        bv.HighlightBone(_renderer.SelectedBoneIndex);
    }

    private void ShowBottomFeature(string name)
    {
        if (!_bottomViews.ContainsKey(name))
        {
            View view;
            if (name == "Explorer")
            {
                var ev = new ExplorerView(MmdFileSystem.BaseDir);
                ev.LoadDirectory(MmdFileSystem.BaseDir);
                view = ev;
            }
            else if (name == "Open")
            {
                var modelsPath = MmdFileSystem.Ensure("Models");
                var ev = new ExplorerView(modelsPath, ModelExtensions);
                ev.FileSelected += OnOpenExplorerFileSelected;
                ev.LoadDirectory(modelsPath);
                view = ev;
            }
            else if (name == "BONE")
            {
                var bv = new BoneView();
                SetupBoneView(bv);
                view = bv;
            }
            else if (name == "PMX")
            {
                var pv = new PmxView();
                pv.SetModel(_currentModel);
                view = pv;
            }
            else if (name == "Lighting")
            {
                var mv = new LightingView
                {
                    ShadeShift = _shadeShift,
                    ShadeToony = _shadeToony,
                    RimIntensity = _rimIntensity,
                    SphereStrength = _sphereStrength,
                    ToonStrength = _toonStrength
                };
                mv.ShadeShiftChanged += v =>
                {
                    _shadeShift = (float)v;
                    UpdateRendererLightingProperties();
                };
                mv.ShadeToonyChanged += v =>
                {
                    _shadeToony = (float)v;
                    UpdateRendererLightingProperties();
                };
                mv.RimIntensityChanged += v =>
                {
                    _rimIntensity = (float)v;
                    UpdateRendererLightingProperties();
                };
                mv.SphereStrengthChanged += v =>
                {
                    _sphereStrength = (float)v;
                    _settings.SphereStrength = _sphereStrength;
                    _settings.Save();
                    UpdateRendererLightingProperties();
                };
                mv.ToonStrengthChanged += v =>
                {
                    _toonStrength = (float)v;
                    _settings.ToonStrength = _toonStrength;
                    _settings.Save();
                    UpdateRendererLightingProperties();
                };
                view = mv;
            }
            else if (name == "MORPH")
            {
                var mv = new MorphView();
                if (_currentModel?.Morphs != null)
                {
                    mv.SetMorphs(_currentModel.Morphs);
                }
                mv.MorphValueChanged += (morphName, value) =>
                {
                    _renderer.SetMorph(morphName, (float)value);
                };
                view = mv;
            }
            // TODO: リリース時にはこの "PHYSICS" ケースを削除する
            else if (name == "PHYSICS")
            {
                var rb = _currentModel?.RigidBodies ?? Enumerable.Empty<RigidBodyData>();
                var bones = _currentModel?.Bones ?? new List<BoneData>();
                var pv = new PhysicsView(_settings.Physics, _settings.UseScaledGravity, rb, bones);
                view = pv;
            }
            else if (name == "SETTING")
            {
                var sv = new SettingView();
                UpdateSettingViewProperties(sv);
                sv.HeightRatioChanged += ratio =>
                {
                    _bottomHeightRatio = ratio;
                    UpdateLayout();
                };
                sv.RotateSensitivityChanged += v =>
                {
                    if (_renderer != null)
                        _renderer.RotateSensitivity = (float)v;
                };
                sv.PanSensitivityChanged += v =>
                {
                    if (_renderer != null)
                        _renderer.PanSensitivity = (float)v;
                };
                sv.ResetCameraRequested += () =>
                {
                    if (_renderer != null)
                        _renderer.ResetCamera();
                    Viewer?.InvalidateSurface();
                };
                view = sv;
            }
            else
            {
                view = new Label
                {
                    Text = $"{name} view",
                    TextColor = ResourceHelper.GetColor("TextColor"),
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center
                };
            }
            _bottomViews[name] = view;

            var tabBgColor = (Color)(Application.Current?.Resources?.TryGetValue("TabBackgroundColor", out var tabBgColorValue) == true ? tabBgColorValue : Colors.LightGray);
            var border = new Border
            {
                BackgroundColor = tabBgColor,
                Padding = new Thickness(8, 2),
                MinimumWidthRequest = 60
            };
            var label = new Label
            {
                Text = name,
                TextColor = ResourceHelper.GetColor("TextColor"),
                FontSize = 16,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };
            border.Content = label;
            string captured = name;
            var tap = new TapGestureRecognizer();
            tap.Tapped += (s, e) =>
            {
                SwitchBottomFeature(captured);
                HideAllMenus();
            };
            border.GestureRecognizers.Add(tap);
            BottomTabBar.Add(border);
            _bottomTabs[name] = border;
        }
        else if (name == "SETTING" && _bottomViews[name] is SettingView sv)
        {
            UpdateSettingViewProperties(sv);
        }
        else if (name == "BONE" && _bottomViews[name] is BoneView bv)
        {
            UpdateBoneViewProperties(bv);
        }
        else if (name == "PMX" && _bottomViews[name] is PmxView pv)
        {
            pv.SetModel(_currentModel);
        }
        else if (name == "Open" && _bottomViews[name] is ExplorerView oev)
        {
            var modelsPath = MmdFileSystem.Ensure("Models");
            oev.LoadDirectory(modelsPath);
        }
        else if (name == "Lighting" && _bottomViews[name] is LightingView mv)
        {
            mv.ShadeShift = _renderer.ShadeShift;
            mv.ShadeToony = _renderer.ShadeToony;
            mv.RimIntensity = _renderer.RimIntensity;
            mv.SphereStrength = _renderer.SphereStrength;
            mv.ToonStrength = _renderer.ToonStrength;
        }
        else if (name == "MORPH" && _bottomViews[name] is MorphView morphView)
        {
            if (_currentModel?.Morphs != null)
            {
                morphView.SetMorphs(_currentModel.Morphs);
            }
        }
        // TODO: リリース時にはこの "PHYSICS" ケースを削除する
        else if (name == "PHYSICS" && _bottomViews[name] is PhysicsView pv2)
        {
            pv2.SetConfig(_settings.Physics, _settings.UseScaledGravity);
            var rb = _currentModel?.RigidBodies ?? Enumerable.Empty<RigidBodyData>();
            var bones = _currentModel?.Bones ?? new List<BoneData>();
            pv2.SetRigidBodies(rb, bones);
        }

        SwitchBottomFeature(name);
        BottomRegion.IsVisible = true;
        UpdateLayout();
    }

    private void SwitchBottomFeature(string name)
    {
        if (_bottomViews.TryGetValue(name, out var view))
        {
            BottomContent.Content = view;
            _currentFeature = name;
            UpdateTabColors();
        }
    }

    private void UpdateTabColors()
    {
        var active = (Color)(Application.Current?.Resources?.TryGetValue("TabActiveColor", out var activeColor) == true ? activeColor : Colors.Blue);
        var inactive = (Color)(Application.Current?.Resources?.TryGetValue("TabInactiveColor", out var inactiveColor) == true ? inactiveColor : Colors.Gray);
        foreach (var kv in _bottomTabs)
        {
            kv.Value.BackgroundColor = kv.Key == _currentFeature ? active : inactive;
        }
    }

    private void RemoveBottomFeature(string name)
    {
        if (_bottomViews.Remove(name))
        {
            if (_bottomTabs.TryGetValue(name, out var tab))
            {
                BottomTabBar.Remove(tab);
                _bottomTabs.Remove(name);
            }

            if (_currentFeature == name)
            {
                _currentFeature = null;
                if (_bottomViews.Count > 0)
                {
                    var next = _bottomViews.Keys.First();
                    SwitchBottomFeature(next);
                }
                else
                {
                    BottomRegion.IsVisible = false;
                }
            }

            UpdateLayout();
        }
    }
}
