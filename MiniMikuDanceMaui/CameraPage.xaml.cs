using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using SkiaSharp;
using OpenTK.Graphics.ES30;
using Microsoft.Maui.Storage;
using System.IO;
using System.Linq;
using MiniMikuDance.Import;

namespace MiniMikuDanceMaui;

public partial class CameraPage : ContentPage
{
    private double _bottomHeightRatio = 0.5;
    private double _bottomWidthRatio = 1.0;
    private double _rotateSensitivity = 1.0;
    private double _panSensitivity = 1.0;
    private const double TopMenuHeight = 36;
    private bool _viewMenuOpen;
    private bool _settingMenuOpen;
    
    private void UpdateOverlay() => MenuOverlay.IsVisible = _viewMenuOpen || _settingMenuOpen;
    private readonly Dictionary<string, View> _bottomViews = new();
    private readonly Dictionary<string, Border> _bottomTabs = new();
    private string? _currentFeature;

    private readonly SimpleCubeRenderer _renderer = new();
    private bool _glInitialized;
    private readonly Dictionary<long, SKPoint> _touchPoints = new();

    public CameraPage()
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
        this.SizeChanged += OnSizeChanged;
        _renderer.RotateSensitivity = (float)_rotateSensitivity;
        _renderer.PanSensitivity = (float)_panSensitivity;

        if (Viewer is SKGLView glView)
        {
            glView.PaintSurface += OnPaintSurface;
            glView.Touch += OnViewTouch;
        }

        if (SettingContent is SettingView setting)
        {
            setting.HeightRatioChanged += ratio =>
            {
                _bottomHeightRatio = ratio;
                UpdateLayout();
            };
            setting.RotateSensitivityChanged += v =>
            {
                _rotateSensitivity = v;
                _renderer.RotateSensitivity = (float)_rotateSensitivity;
            };
            setting.PanSensitivityChanged += v =>
            {
                _panSensitivity = v;
                _renderer.PanSensitivity = (float)_panSensitivity;
            };
            setting.CameraLockChanged += locked =>
            {
                _renderer.CameraLocked = locked;
            };
        }
    }

    private void ShowViewMenu()
    {
        HideSettingMenu();
        _viewMenuOpen = true;
        ViewMenu.IsVisible = true;
        UpdateOverlay();
    }

    private void OnViewMenuTapped(object? sender, TappedEventArgs e)
    {
        if (_viewMenuOpen)
        {
            HideViewMenu();
        }
        else
        {
            ShowViewMenu();
        }
        UpdateLayout();
    }

    private async void OnHomeClicked(object? sender, EventArgs e)
    {
        HideViewMenu();
        await Navigation.PopToRootAsync();
    }

    private void OnSettingClicked(object? sender, EventArgs e)
    {
        OnSettingMenuTapped(sender, new TappedEventArgs(null));
    }

    private void ShowSettingMenu()
    {
        HideViewMenu();
        _settingMenuOpen = true;
        SettingMenu.IsVisible = true;
        if (SettingContent is SettingView sv)
        {
            sv.HeightRatio = _bottomHeightRatio;
            sv.RotateSensitivity = _rotateSensitivity;
            sv.PanSensitivity = _panSensitivity;
            sv.CameraLocked = _renderer.CameraLocked;
        }
        UpdateOverlay();
    }

    private void OnSettingMenuTapped(object? sender, TappedEventArgs e)
    {
        if (_settingMenuOpen)
        {
            HideSettingMenu();
        }
        else
        {
            ShowSettingMenu();
        }
        UpdateLayout();
    }

    private async void OnSelectClicked(object? sender, EventArgs e)
    {
        HideViewMenu();
        HideSettingMenu();
        await ShowModelSelector();
    }

    private void OnPoseClicked(object? sender, EventArgs e)
    {
        LogService.WriteLine("POSE button clicked");
        ShowBottomFeature("POSE");
        HideViewMenu();
        HideSettingMenu();
    }

    private void OnMotionClicked(object? sender, EventArgs e)
    {
        LogService.WriteLine("MOTION button clicked");
        ShowBottomFeature("MOTION");
        HideViewMenu();
        HideSettingMenu();
    }
    
    private void OnArClicked(object? sender, EventArgs e)
    {
        LogService.WriteLine("AR button clicked");
        ShowBottomFeature("AR");
        HideViewMenu();
        HideSettingMenu();
    }

    private void OnRecordClicked(object? sender, EventArgs e)
    {
        LogService.WriteLine("RECORD button clicked");
        ShowBottomFeature("RECORD");
        HideViewMenu();
        HideSettingMenu();
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
        UpdateLayout();
    }

    private void OnOverlayTapped(object? sender, TappedEventArgs e)
    {
        HideViewMenu();
        HideSettingMenu();
        UpdateLayout();
    }

    private void OnBottomRegionTapped(object? sender, TappedEventArgs e)
    {
        HideViewMenu();
        HideSettingMenu();
        UpdateLayout();
    }

    private void OnResetCamClicked(object? sender, EventArgs e)
    {
        _renderer.ResetCamera();
        Viewer?.InvalidateSurface();
        LogService.WriteLine("Camera reset");
        HideViewMenu();
        HideSettingMenu();
    }

    private void OnExplorerClicked(object? sender, EventArgs e)
    {
        ShowBottomFeature("Explorer");
        HideViewMenu();
        HideSettingMenu();
    }

    private void HideViewMenu()
    {
        _viewMenuOpen = false;
        ViewMenu.IsVisible = false;
        UpdateOverlay();
    }

    private void HideBottomRegion()
    {
        BottomRegion.IsVisible = false;
        _currentFeature = null;
        UpdateTabColors();
    }

    private void HideSettingMenu()
    {
        _settingMenuOpen = false;
        SettingMenu.IsVisible = false;
        UpdateOverlay();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
#if ANDROID
        var status = await Permissions.RequestAsync<Permissions.StorageWrite>();
        if (status != PermissionStatus.Granted)
        {
            LogService.WriteLine("[CameraPage] Storage permission denied");
        }
#endif
        try
        {
            var modelPath = await EnsureSampleModel();
            var importer = new ModelImporter();
            ModelData? data = null;
            if (!string.IsNullOrEmpty(modelPath))
            {
                LogService.WriteLine($"[CameraPage] Using model: {modelPath}");
                data = importer.ImportModel(modelPath);
            }
            else
            {
                try
                {
                    using var stream = await FileSystem.OpenAppPackageFileAsync("AliciaSolid.vrm");
                    LogService.WriteLine("[CameraPage] Loading bundled model: AliciaSolid.vrm");
                    data = importer.ImportModel(stream);
                }
                catch (FileNotFoundException fnf)
                {
                    LogService.WriteLine($"[CameraPage] Bundled model not found: {fnf.FileName}");
                }
            }

            if (data != null)
            {
                _renderer.LoadModel(data);
                LogService.WriteLine("[CameraPage] Model initialized successfully");
            }
            else
            {
                LogService.WriteLine("[CameraPage] Model data was null. Initialization aborted");
            }
        }
        catch (Exception ex)
        {
            LogService.WriteLine($"Failed to initialize model: {ex.Message}");
            LogService.WriteLine(ex.ToString());
        }
        _glInitialized = false;
        Viewer?.InvalidateSurface();
    }

    private Task<string?> EnsureSampleModel()
    {
        var modelDir = MmdFileSystem.Ensure("Models");
        MmdFileSystem.AppendAccessLog(modelDir);
        LogService.WriteLine($"[CameraPage] Searching models in {modelDir}");
        var vrm = Directory.EnumerateFiles(modelDir, "*.vrm").FirstOrDefault();
        if (!string.IsNullOrEmpty(vrm))
        {
            LogService.WriteLine($"[CameraPage] Found existing model at {vrm}");
            return Task.FromResult<string?>(vrm);
        }

        LogService.WriteLine("[CameraPage] No models found in storage");
        return Task.FromResult<string?>(null);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _glInitialized = false;
    }

    private void OnSizeChanged(object? sender, EventArgs e) => UpdateLayout();

    private void UpdateLayout()
    {
        double W = this.Width;
        double H = this.Height;
        Thickness safe = this.Padding;
        AbsoluteLayout.SetLayoutBounds(TopMenu, new Rect(0, 0, W, TopMenuHeight));
        AbsoluteLayout.SetLayoutFlags(TopMenu, AbsoluteLayoutFlags.None);

        double bottomHeight = BottomRegion.IsVisible ? H * _bottomHeightRatio : 0;
        AbsoluteLayout.SetLayoutBounds(ViewMenu, new Rect(0, TopMenuHeight, 200,
            ViewMenu.IsVisible ? AbsoluteLayout.AutoSize : 0));
        AbsoluteLayout.SetLayoutFlags(ViewMenu, AbsoluteLayoutFlags.None);
        AbsoluteLayout.SetLayoutBounds(SettingMenu, new Rect(0, TopMenuHeight, 250,
            SettingMenu.IsVisible ? AbsoluteLayout.AutoSize : 0));
        AbsoluteLayout.SetLayoutFlags(SettingMenu, AbsoluteLayoutFlags.None);

        AbsoluteLayout.SetLayoutBounds(MenuOverlay, new Rect(0, 0, W, H));
        AbsoluteLayout.SetLayoutFlags(MenuOverlay, AbsoluteLayoutFlags.None);

        AbsoluteLayout.SetLayoutBounds(Viewer, new Rect(0, 0, W, H));
        AbsoluteLayout.SetLayoutFlags(Viewer, AbsoluteLayoutFlags.None);
        double bottomWidth = W * _bottomWidthRatio;
        AbsoluteLayout.SetLayoutBounds(BottomRegion, new Rect((W - bottomWidth) / 2, H - bottomHeight, bottomWidth, bottomHeight));
        AbsoluteLayout.SetLayoutFlags(BottomRegion, AbsoluteLayoutFlags.None);
    }






    private void OnPaintSurface(object? sender, SKPaintGLSurfaceEventArgs e)
    {
        if (!_glInitialized)
        {
            GL.LoadBindings(new SKGLViewBindingsContext());
            _renderer.Initialize();
            _glInitialized = true;
        }
        _renderer.Resize(e.BackendRenderTarget.Width, e.BackendRenderTarget.Height);
        _renderer.Render();
        GL.Flush();
    }

    private void OnViewTouch(object? sender, SKTouchEventArgs e)
    {
        if (_viewMenuOpen || _settingMenuOpen)
        {
            HideViewMenu();
            HideSettingMenu();
            UpdateLayout();
        }

        if (e.ActionType == SKTouchAction.Pressed)
        {
            _touchPoints[e.Id] = e.Location;
        }
        else if (e.ActionType == SKTouchAction.Moved)
        {
            var prevPoints = new Dictionary<long, SKPoint>(_touchPoints);
            _touchPoints[e.Id] = e.Location;

            if (_touchPoints.Count == 1 && prevPoints.ContainsKey(e.Id))
            {
                var prev = prevPoints[e.Id];
                var dx = e.Location.X - prev.X;
                var dy = e.Location.Y - prev.Y;
                _renderer.Orbit(dx, dy);
            }
            else if (_touchPoints.Count == 2 && prevPoints.Count == 2)
            {
                var ids = new List<long>(_touchPoints.Keys);
                var p0Old = prevPoints[ids[0]];
                var p1Old = prevPoints[ids[1]];
                var p0New = _touchPoints[ids[0]];
                var p1New = _touchPoints[ids[1]];
                var oldMid = new SKPoint((p0Old.X + p1Old.X) / 2, (p0Old.Y + p1Old.Y) / 2);
                var newMid = new SKPoint((p0New.X + p1New.X) / 2, (p0New.Y + p1New.Y) / 2);
                _renderer.Pan(newMid.X - oldMid.X, newMid.Y - oldMid.Y);
                float oldDist = (p0Old - p1Old).Length;
                float newDist = (p0New - p1New).Length;
                _renderer.Dolly(oldDist - newDist);
            }
        }
        else if (e.ActionType == SKTouchAction.Released || e.ActionType == SKTouchAction.Cancelled)
        {
            _touchPoints.Remove(e.Id);
        }
        e.Handled = true;
        Viewer?.InvalidateSurface();
    }

    private async Task ShowModelSelector()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select VRM file"
            });

            if (result != null)
            {
                if (Path.GetExtension(result.FileName).ToLowerInvariant() != ".vrm")
                {
                    await DisplayAlert("Invalid File", "Please select a .vrm file.", "OK");
                    return;
                }

                LogService.WriteLine($"Model selected: {result.FileName}");

                var importer = new MiniMikuDance.Import.ModelImporter();
                var data = importer.ImportModel(result.FullPath);
                _renderer.LoadModel(data);
                Viewer?.InvalidateSurface();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
            LogService.WriteLine($"Error selecting model: {ex.Message}");
        }
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
            else if (name == "SETTING")
            {
                var sv = new SettingView { HeightRatio = _bottomHeightRatio, RotateSensitivity = _rotateSensitivity, PanSensitivity = _panSensitivity, CameraLocked = _renderer.CameraLocked };
                sv.HeightRatioChanged += ratio =>
                {
                    _bottomHeightRatio = ratio;
                    UpdateLayout();
                };
                sv.RotateSensitivityChanged += v =>
                {
                    _rotateSensitivity = v;
                    _renderer.RotateSensitivity = (float)_rotateSensitivity;
                };
                sv.PanSensitivityChanged += v =>
                {
                    _panSensitivity = v;
                    _renderer.PanSensitivity = (float)_panSensitivity;
                };
                sv.CameraLockChanged += locked =>
                {
                    _renderer.CameraLocked = locked;
                };
                view = sv;
            }
            else
            {
                view = new Label { Text = $"{name} view", TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center };
            }
            _bottomViews[name] = view;

            var border = new Border
            {
                BackgroundColor = Color.FromArgb("#444444"),
                Padding = new Thickness(8, 2),
                MinimumWidthRequest = 60
            };
            var label = new Label
            {
                Text = name,
                TextColor = Colors.White,
                FontSize = 14,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };
            border.Content = label;
            string captured = name;
            var tap = new TapGestureRecognizer();
            tap.Tapped += (s, e) =>
            {
                SwitchBottomFeature(captured);
                HideViewMenu();
                HideSettingMenu();
                UpdateLayout();
            };
            border.GestureRecognizers.Add(tap);
            BottomTabBar.Add(border);
            _bottomTabs[name] = border;
        }
        else if (name == "SETTING" && _bottomViews[name] is SettingView sv)
        {
            sv.HeightRatio = _bottomHeightRatio;
            sv.RotateSensitivity = _rotateSensitivity;
            sv.PanSensitivity = _panSensitivity;
            sv.CameraLocked = _renderer.CameraLocked;
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
        foreach (var kv in _bottomTabs)
        {
            kv.Value.BackgroundColor = kv.Key == _currentFeature
                ? Color.FromArgb("#333333")
                : Color.FromArgb("#666666");
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
