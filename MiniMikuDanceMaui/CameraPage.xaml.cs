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
using GL = OpenTK.Graphics.ES30.GL;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Devices;
using System.IO;
using System.Linq;
using MiniMikuDance.Import;
using OpenTK.Mathematics;
using MiniMikuDance.Util;
using MiniMikuDance.PoseEstimation;
using MiniMikuDance.Motion;

namespace MiniMikuDanceMaui;

public partial class CameraPage : ContentPage
{
    // 下部領域1 : 上部領域2 の比率となるよう初期値を設定
    private double _bottomHeightRatio = 1.0 / 3.0;
    private double _bottomWidthRatio = 1.0;
    // カメラ感度はスライダーの最小値を初期値とする
    private float _rotateSensitivity = 0.1f;
    private float _panSensitivity = 0.1f;
    private float _shadeShift = -0.1f;
    private float _shadeToony = 0.9f;
    private float _rimIntensity = 0.5f;
    private const double TopMenuHeight = 36;
    private bool _viewMenuOpen;
    private bool _settingMenuOpen;
    private bool _fileMenuOpen;
    private bool _timelinePanelVisible;

    private void UpdateOverlay() => MenuOverlay.IsVisible = _viewMenuOpen || _settingMenuOpen || _fileMenuOpen;
    private readonly Dictionary<string, View> _bottomViews = new();
    private readonly Dictionary<string, Border> _bottomTabs = new();
    private string? _currentFeature;
    private string? _selectedPath;
    private string? _selectedVideoPath;
    private string? _selectedPosePath;

    private readonly SimpleCubeRenderer _renderer = new();
    private bool _glInitialized;
    private ModelData? _pendingModel;
    private ModelData? _currentModel;
    private int _selectedBoneIndex = 0;
    private Action<MiniMikuDance.PoseEstimation.JointData>? _framePlayedHandler;
    private readonly List<int> _humanoidBoneIndices = new();
    private readonly Dictionary<long, SKPoint> _touchPoints = new();

    private class PoseState
    {
        public IList<Vector3> Rotations = new List<Vector3>();
        public IList<Vector3> Translations = new List<Vector3>();
    }

    private readonly List<PoseState> _poseHistory = new();
    private int _poseHistoryIndex = -1;

    public CameraPage()
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
        this.SizeChanged += OnSizeChanged;
        _renderer.RotateSensitivity = _rotateSensitivity;
        _renderer.PanSensitivity = _panSensitivity;
        _renderer.ShadeShift = _shadeShift;
        _renderer.ShadeToony = _shadeToony;
        _renderer.RimIntensity = _rimIntensity;

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
                _rotateSensitivity = (float)v;
                _renderer.RotateSensitivity = _rotateSensitivity;
            };
            setting.PanSensitivityChanged += v =>
            {
                _panSensitivity = (float)v;
                _renderer.PanSensitivity = _panSensitivity;
            };
            setting.CameraLockChanged += locked =>
            {
                _renderer.CameraLocked = locked;
            };
            setting.ResetCameraRequested += () =>
            {
                _renderer.ResetCamera();
                Viewer?.InvalidateSurface();
            };
        }
    }

    private void ShowViewMenu()
    {
        HideSettingMenu();
        HideFileMenu();
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
        LogService.WriteLine($"View menu {(_viewMenuOpen ? "opened" : "closed")}");
    }

    private async void OnHomeClicked(object? sender, EventArgs e)
    {
        HideViewMenu();
        HideSettingMenu();
        HideFileMenu();
        LogService.WriteLine("Home clicked");
        await Navigation.PopToRootAsync();
    }


    private void OnSettingClicked(object? sender, EventArgs e)
    {
        LogService.WriteLine("Setting clicked");
        OnSettingMenuTapped(sender, new TappedEventArgs(null));
    }

    private void ShowSettingMenu()
    {
        HideViewMenu();
        HideFileMenu();
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
        LogService.WriteLine($"Setting menu {(_settingMenuOpen ? "opened" : "closed")}");
    }

    private void ShowFileMenu()
    {
        HideViewMenu();
        HideSettingMenu();
        _fileMenuOpen = true;
        FileMenu.IsVisible = true;
        UpdateOverlay();
    }

    private void HideFileMenu()
    {
        _fileMenuOpen = false;
        FileMenu.IsVisible = false;
        UpdateOverlay();
    }

    private void OnFileMenuTapped(object? sender, TappedEventArgs e)
    {
        if (_fileMenuOpen)
        {
            HideFileMenu();
        }
        else
        {
            ShowFileMenu();
        }
        UpdateLayout();
        LogService.WriteLine($"File menu {(_fileMenuOpen ? "opened" : "closed")}");
    }


    private async void OnSelectClicked(object? sender, EventArgs e)
    {
        HideViewMenu();
        HideSettingMenu();
        LogService.WriteLine("Select model clicked");
        await ShowModelSelector();
    }

    private void OnBoneClicked(object? sender, EventArgs e)
    {
        LogService.WriteLine("BONE button clicked");
        ShowBottomFeature("BONE");
        HideViewMenu();
        HideSettingMenu();
    }


    private void OnMToonClicked(object? sender, EventArgs e)
    {
        LogService.WriteLine("MTOON button clicked");
        ShowBottomFeature("MTOON");
        HideViewMenu();
        HideSettingMenu();
    }

    private void OnCameraClicked(object? sender, EventArgs e)
    {
        LogService.WriteLine("CAMERA button clicked");
        ShowBottomFeature("CAMERA");
        HideViewMenu();
        HideSettingMenu();
    }

    private void OnAnimationClicked(object? sender, EventArgs e)
    {
        LogService.WriteLine("ANIMATION button clicked");
        ShowBottomFeature("ANIMATION");
        HideViewMenu();
        HideSettingMenu();
    }

    private void OnTimelineClicked(object? sender, EventArgs e)
    {
        _timelinePanelVisible = !_timelinePanelVisible;
        if (_bottomViews.TryGetValue("ANIMATION", out var v) && v is AnimationView av)
        {
            av.SetKeyInputPanelVisible(_timelinePanelVisible);
            if (_currentModel != null)
            {
                var list = _currentModel.HumanoidBoneList.Select(h => h.Name).ToList();
                av.SetBones(list);
            }
        }
        LogService.WriteLine($"TIMELINE panel {(_timelinePanelVisible ? "shown" : "hidden")}");
        HideViewMenu();
        HideSettingMenu();
    }

    private void OnTerminalClicked(object? sender, EventArgs e)
    {
        LogService.WriteLine("TERMINAL button clicked");
        ShowBottomFeature("TERMINAL");
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
        LogService.WriteLine("Bottom region closed");
    }

    private void OnOverlayTapped(object? sender, TappedEventArgs e)
    {
        HideViewMenu();
        HideSettingMenu();
        HideFileMenu();
        UpdateLayout();
        LogService.WriteLine("Overlay tapped");
    }

    private void OnBottomRegionTapped(object? sender, TappedEventArgs e)
    {
        HideViewMenu();
        HideSettingMenu();
        HideFileMenu();
        UpdateLayout();
        LogService.WriteLine("Bottom region tapped");
    }


    private void OnExplorerClicked(object? sender, EventArgs e)
    {
        LogService.WriteLine("Explorer clicked");
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
        var readStatus = await Permissions.RequestAsync<Permissions.StorageRead>();
        var writeStatus = await Permissions.RequestAsync<Permissions.StorageWrite>();
        if (readStatus != PermissionStatus.Granted || writeStatus != PermissionStatus.Granted)
        {
            LogService.WriteLine("[CameraPage] Storage permission denied");
        }
        if (!Android.OS.Environment.IsExternalStorageManager)
        {
            LogService.WriteLine("[CameraPage] MANAGE_EXTERNAL_STORAGE not granted");
            try
            {
                var context = Android.App.Application.Context;
                var uri = Android.Net.Uri.Parse($"package:{context.PackageName}");
                var intent = new Android.Content.Intent(Android.Provider.Settings.ActionManageAppAllFilesAccessPermission, uri);
                intent.AddFlags(Android.Content.ActivityFlags.NewTask);
                context.StartActivity(intent);
            }
            catch (Exception ex)
            {
                LogService.WriteLine($"[CameraPage] Failed to launch settings: {ex.Message}");
            }
        }
#endif
        Viewer?.InvalidateSurface();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
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
        AbsoluteLayout.SetLayoutBounds(FileMenu, new Rect(0, TopMenuHeight, 200,
            FileMenu.IsVisible ? AbsoluteLayout.AutoSize : 0));
        AbsoluteLayout.SetLayoutFlags(FileMenu, AbsoluteLayoutFlags.None);
        AbsoluteLayout.SetLayoutBounds(SettingMenu, new Rect(0, TopMenuHeight, 250,
            SettingMenu.IsVisible ? AbsoluteLayout.AutoSize : 0));
        AbsoluteLayout.SetLayoutFlags(SettingMenu, AbsoluteLayoutFlags.None);

        AbsoluteLayout.SetLayoutBounds(MenuOverlay, new Rect(0, 0, W, H));
        AbsoluteLayout.SetLayoutFlags(MenuOverlay, AbsoluteLayoutFlags.None);

        AbsoluteLayout.SetLayoutBounds(FileSelectMessage, new Rect(0.5, TopMenuHeight + 20, 0.8,
            FileSelectMessage.IsVisible ? AbsoluteLayout.AutoSize : 0));
        AbsoluteLayout.SetLayoutFlags(FileSelectMessage,
            AbsoluteLayoutFlags.XProportional | AbsoluteLayoutFlags.WidthProportional);
        AbsoluteLayout.SetLayoutBounds(PoseSelectMessage, new Rect(0.5, TopMenuHeight + 20, 0.8,
            PoseSelectMessage.IsVisible ? AbsoluteLayout.AutoSize : 0));
        AbsoluteLayout.SetLayoutFlags(PoseSelectMessage,
            AbsoluteLayoutFlags.XProportional | AbsoluteLayoutFlags.WidthProportional);
        AbsoluteLayout.SetLayoutBounds(AdaptSelectMessage, new Rect(0.5, TopMenuHeight + 20, 0.8,
            AdaptSelectMessage.IsVisible ? AbsoluteLayout.AutoSize : 0));
        AbsoluteLayout.SetLayoutFlags(AdaptSelectMessage,
            AbsoluteLayoutFlags.XProportional | AbsoluteLayoutFlags.WidthProportional);

        AbsoluteLayout.SetLayoutBounds(LoadingIndicator, new Rect(0.5, 0.5, 40, 40));
        AbsoluteLayout.SetLayoutFlags(LoadingIndicator, AbsoluteLayoutFlags.PositionProportional);

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
            if (_pendingModel != null)
            {
                _renderer.ClearBoneRotations();
                _renderer.LoadModel(_pendingModel);
                _currentModel = _pendingModel;
                _shadeShift = _pendingModel.ShadeShift;
                _shadeToony = _pendingModel.ShadeToony;
                _rimIntensity = _pendingModel.RimIntensity;
                _renderer.ShadeShift = _pendingModel.ShadeShift;
                _renderer.ShadeToony = _pendingModel.ShadeToony;
                _renderer.RimIntensity = _pendingModel.RimIntensity;
                _pendingModel = null;
                SavePoseState();
            }
            _glInitialized = true;
        }
        else if (_pendingModel != null)
        {
            _renderer.ClearBoneRotations();
            _renderer.LoadModel(_pendingModel);
            _currentModel = _pendingModel;
            _shadeShift = _pendingModel.ShadeShift;
            _shadeToony = _pendingModel.ShadeToony;
            _rimIntensity = _pendingModel.RimIntensity;
            _renderer.ShadeShift = _pendingModel.ShadeShift;
            _renderer.ShadeToony = _pendingModel.ShadeToony;
            _renderer.RimIntensity = _pendingModel.RimIntensity;
            _pendingModel = null;
            SavePoseState();
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
                PickerTitle = "Select VRM file",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    [DevicePlatform.Android] = new[] { "application/octet-stream", ".vrm" },
                    [DevicePlatform.WinUI] = new[] { ".vrm" },
                    [DevicePlatform.iOS] = new[] { ".vrm" }
                })
            });

            if (result != null)
            {
                if (Path.GetExtension(result.FileName).ToLowerInvariant() != ".vrm")
                {
                    await DisplayAlert("Invalid File", "Please select a .vrm file.", "OK");
                    return;
                }

                LogService.WriteLine($"Model selected: {result.FileName}");

                await using var stream = await result.OpenReadAsync();
                var importer = new MiniMikuDance.Import.ModelImporter();
                var data = importer.ImportModel(stream);
                _renderer.LoadModel(data);
                _currentModel = data;
                _shadeShift = data.ShadeShift;
                _shadeToony = data.ShadeToony;
                _rimIntensity = data.RimIntensity;
                _renderer.ShadeShift = data.ShadeShift;
                _renderer.ShadeToony = data.ShadeToony;
                _renderer.RimIntensity = data.RimIntensity;
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
            else if (name == "Open")
            {
                var modelsPath = MmdFileSystem.Ensure("Models");
                var ev = new ExplorerView(modelsPath, new[] { ".vrm" });
                ev.FileSelected += OnOpenExplorerFileSelected;
                ev.LoadDirectory(modelsPath);
                view = ev;
            }
            else if (name == "Analyze")
            {
                var videoPath = MmdFileSystem.Ensure("Movie");
                var ev = new ExplorerView(videoPath, new[] { ".mp4" });
                ev.FileSelected += OnAnalyzeExplorerFileSelected;
                ev.LoadDirectory(videoPath);
                view = ev;
            }
            else if (name == "Adapt")
            {
                var posePath = MmdFileSystem.Ensure("Poses");
                var ev = new ExplorerView(posePath, new[] { ".json" });
                ev.FileSelected += OnAdaptExplorerFileSelected;
                ev.LoadDirectory(posePath);
                view = ev;
            }
            else if (name == "BONE")
            {
                var bv = new BoneView();
                if (_currentModel != null)
                {
                    var list = new List<string>();
                    _humanoidBoneIndices.Clear();
                    foreach (var kv in _currentModel.HumanoidBoneList)
                    {
                        list.Add(kv.Name);
                        _humanoidBoneIndices.Add(kv.Index);
                    }
                    bv.SetBones(list);
                    if (_humanoidBoneIndices.Count > 0)
                    {
                        var idx0 = _humanoidBoneIndices[0];
                        var euler = _renderer.GetBoneRotation(idx0);
                        bv.SetRotation(euler);
                        var trans = _renderer.GetBoneTranslation(idx0);
                        bv.SetTranslation(trans);
                        _selectedBoneIndex = idx0;
                    }
                }
                bv.ResetRequested += OnBoneReset;
                bv.RangeChanged += OnBoneRangeChanged;
                bv.BoneSelected += idx =>
                {
                    if (idx >= 0 && idx < _humanoidBoneIndices.Count)
                        _selectedBoneIndex = _humanoidBoneIndices[idx];
                    if (_selectedBoneIndex >= 0)
                    {
                        var euler = _renderer.GetBoneRotation(_selectedBoneIndex);
                        bv.SetRotation(euler);
                        var trans = _renderer.GetBoneTranslation(_selectedBoneIndex);
                        bv.SetTranslation(trans);
                    }
                };
                bv.RotationXChanged += v => UpdateSelectedBoneRotation(bv);
                bv.RotationYChanged += v => UpdateSelectedBoneRotation(bv);
                bv.RotationZChanged += v => UpdateSelectedBoneRotation(bv);
                bv.TranslationXChanged += v => UpdateSelectedBoneTranslation(bv);
                bv.TranslationYChanged += v => UpdateSelectedBoneTranslation(bv);
                bv.TranslationZChanged += v => UpdateSelectedBoneTranslation(bv);
                bv.SetRotationRange(-180, 180);
                if (_poseHistory.Count == 0)
                    SavePoseState();
                view = bv;
            }
            else if (name == "CAMERA")
            {
                var cv = new CameraView();
                view = cv;
            }
            else if (name == "TERMINAL")
            {
                var tv = new TerminalView();
                view = tv;
            }
            else if (name == "ANIMATION")
            {
                var av = new AnimationView();
                av.PlayRequested += OnPlayAnimationRequested;
                av.FrameChanged += OnAnimationFrameChanged;
                av.SetKeyInputPanelVisible(_timelinePanelVisible);
                if (_currentModel != null)
                {
                    var list = _currentModel.HumanoidBoneList.Select(h => h.Name).ToList();
                    av.SetBones(list);
                }
                view = av;
            }
            else if (name == "MTOON")
            {
                var mv = new MToonView
                {
                    ShadeShift = _shadeShift,
                    ShadeToony = _shadeToony,
                    RimIntensity = _rimIntensity
                };
                mv.ShadeShiftChanged += v =>
                {
                    _shadeShift = (float)v;
                    _renderer.ShadeShift = _shadeShift;
                };
                mv.ShadeToonyChanged += v =>
                {
                    _shadeToony = (float)v;
                    _renderer.ShadeToony = _shadeToony;
                };
                mv.RimIntensityChanged += v =>
                {
                    _rimIntensity = (float)v;
                    _renderer.RimIntensity = _rimIntensity;
                };
                view = mv;
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
                    _rotateSensitivity = (float)v;
                    _renderer.RotateSensitivity = _rotateSensitivity;
                };
                sv.PanSensitivityChanged += v =>
                {
                    _panSensitivity = (float)v;
                    _renderer.PanSensitivity = _panSensitivity;
                };
                sv.CameraLockChanged += locked =>
                {
                    _renderer.CameraLocked = locked;
                };
                sv.ResetCameraRequested += () =>
                {
                    _renderer.ResetCamera();
                    Viewer?.InvalidateSurface();
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
        else if (name == "BONE" && _bottomViews[name] is BoneView bv)
        {
            if (_currentModel != null)
            {
                var list = new List<string>();
                _humanoidBoneIndices.Clear();
                foreach (var kv in _currentModel.HumanoidBoneList)
                {
                    list.Add(kv.Name);
                    _humanoidBoneIndices.Add(kv.Index);
                }
                bv.SetBones(list);
                if (_selectedBoneIndex >= 0)
                {
                    var euler = _renderer.GetBoneRotation(_selectedBoneIndex);
                    bv.SetRotation(euler);
                    var trans = _renderer.GetBoneTranslation(_selectedBoneIndex);
                    bv.SetTranslation(trans);
                }
            }
        }
        else if (name == "Analyze" && _bottomViews[name] is ExplorerView aev)
        {
            var videoPath = MmdFileSystem.Ensure("Movie");
            aev.LoadDirectory(videoPath);
        }
        else if (name == "Adapt" && _bottomViews[name] is ExplorerView aev2)
        {
            var posePath = MmdFileSystem.Ensure("Poses");
            aev2.LoadDirectory(posePath);
        }
        else if (name == "MTOON" && _bottomViews[name] is MToonView mv)
        {
            mv.ShadeShift = _shadeShift;
            mv.ShadeToony = _shadeToony;
            mv.RimIntensity = _rimIntensity;
        }
        else if (name == "ANIMATION" && _bottomViews[name] is AnimationView av)
        {
            av.SetKeyInputPanelVisible(_timelinePanelVisible);
            if (_currentModel != null)
            {
                var list = _currentModel.HumanoidBoneList.Select(h => h.Name).ToList();
                av.SetBones(list);
            }
        }
        else if (name == "CAMERA" && _bottomViews[name] is CameraView)
        {
            // nothing to update
        }
        else if (name == "TERMINAL" && _bottomViews[name] is TerminalView)
        {
            // nothing to update
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

    private async void OnAddToLibraryClicked(object? sender, EventArgs e)
    {
        LogService.WriteLine("Add to library clicked");
        HideFileMenu();
        await AddToLibraryAsync();
    }

    private async Task AddToLibraryAsync()
    {
        LogService.WriteLine("Add to library start");
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select VRM file",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    [DevicePlatform.Android] = new[] { "application/octet-stream", ".vrm" },
                    [DevicePlatform.WinUI] = new[] { ".vrm" },
                    [DevicePlatform.iOS] = new[] { ".vrm" }
                })
            });

            if (result == null) return;

            string dstDir = MmdFileSystem.Ensure("Models");
            string dstPath = Path.Combine(dstDir, Path.GetFileName(result.FullPath));
            await using Stream src = await result.OpenReadAsync();
            await using FileStream dst = File.Create(dstPath);
            await src.CopyToAsync(dst);

            LogService.WriteLine($"Added to library: {Path.GetFileName(dstPath)}");

            await DisplayAlert("Copied", $"{Path.GetFileName(dstPath)} をライブラリに追加しました", "OK");
        }
        catch (Exception ex)
        {
            LogService.WriteLine($"Add to library failed: {ex.Message}");
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private void OnOpenInViewerClicked(object? sender, EventArgs e)
    {
        LogService.WriteLine("Open in viewer clicked");
        HideFileMenu();
        HideViewMenu();
        HideSettingMenu();
        ShowOpenExplorer();
    }

    private void OnEstimatePoseClicked(object? sender, EventArgs e)
    {
        LogService.WriteLine("Estimate pose clicked");
        HideFileMenu();
        HideViewMenu();
        HideSettingMenu();
        ShowPoseExplorer();
    }

    private void OnAdaptPoseClicked(object? sender, EventArgs e)
    {
        LogService.WriteLine("Adapt pose clicked");
        HideFileMenu();
        HideViewMenu();
        HideSettingMenu();
        ShowAdaptExplorer();
    }

    private void ShowOpenExplorer()
    {
        LogService.WriteLine("Show open explorer");
        ShowBottomFeature("Open");
        FileSelectMessage.IsVisible = true;
        SelectedFilePath.Text = string.Empty;
        _selectedPath = null;
        UpdateLayout();
    }

    private void OnOpenExplorerFileSelected(object? sender, string path)
    {
        if (Path.GetExtension(path).ToLowerInvariant() != ".vrm")
        {
            return;
        }
        LogService.WriteLine($"File selected: {Path.GetFileName(path)}");
        _selectedPath = path;
        SelectedFilePath.Text = path;
    }

    private async void OnImportClicked(object? sender, EventArgs e)
    {
        LogService.WriteLine("Import clicked");
        if (string.IsNullOrEmpty(_selectedPath))
        {
            await DisplayAlert("Error", "ファイルが選択されていません", "OK");
            return;
        }

        RemoveBottomFeature("Open");
        FileSelectMessage.IsVisible = false;
        LoadingIndicator.IsVisible = true;
        Viewer.HasRenderLoop = false;
        UpdateLayout();

        try
        {
            var importer = new ModelImporter();
            var data = await Task.Run(() => importer.ImportModel(_selectedPath));
            _pendingModel = data;
            LogService.WriteLine($"Imported VRM: {Path.GetFileName(_selectedPath!)}");
            LogService.WriteLine($"Spec: {data.Info.SpecVersion}");
            LogService.WriteLine($"Title: {data.Info.Title}");
            LogService.WriteLine($"Author: {data.Info.Author}");
            LogService.WriteLine($"License: {data.Info.License}");
            LogService.WriteLine($"Nodes: {data.Info.NodeCount}");
            LogService.WriteLine($"Meshes: {data.Info.MeshCount}");
            LogService.WriteLine($"Skins: {data.Info.SkinCount}");
            LogService.WriteLine($"Vertices: {data.Info.VertexCount}");
            LogService.WriteLine($"Triangles: {data.Info.TriangleCount}");
            LogService.WriteLine($"Materials: {data.Info.MaterialCount}");
            LogService.WriteLine($"Textures: {data.Info.TextureCount}");
            LogService.WriteLine($"Humanoid bones: {data.Info.HumanoidBoneCount} / 55");
            foreach (var bone in data.Bones)
            {
                LogService.WriteLine($"Bone: {bone.Name}");
            }
            _renderer.ResetCamera();
            _glInitialized = false;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
            LogService.WriteLine($"Import failed: {ex.Message}");
        }
        finally
        {
            Viewer.HasRenderLoop = true;
            LoadingIndicator.IsVisible = false;
            UpdateLayout();
            Viewer.InvalidateSurface();
            _selectedPath = null;
        }
    }

    private void OnCancelImportClicked(object? sender, EventArgs e)
    {
        LogService.WriteLine("Import canceled");
        _selectedPath = null;
        FileSelectMessage.IsVisible = false;
        SelectedFilePath.Text = string.Empty;
        UpdateLayout();
    }

    private void ShowAdaptExplorer()
    {
        LogService.WriteLine("Show adapt explorer");
        ShowBottomFeature("Adapt");
        AdaptSelectMessage.IsVisible = true;
        SelectedPosePath.Text = string.Empty;
        _selectedPosePath = null;
        UpdateLayout();
    }

    private void OnAdaptExplorerFileSelected(object? sender, string path)
    {
        if (Path.GetExtension(path).ToLowerInvariant() != ".json")
        {
            return;
        }
        LogService.WriteLine($"Pose selected: {Path.GetFileName(path)}");
        _selectedPosePath = path;
        SelectedPosePath.Text = path;
    }

    private void ShowPoseExplorer()
    {
        LogService.WriteLine("Show pose explorer");
        ShowBottomFeature("Analyze");
        PoseSelectMessage.IsVisible = true;
        SelectedVideoPath.Text = string.Empty;
        _selectedVideoPath = null;
        UpdateLayout();
    }

    private void OnAnalyzeExplorerFileSelected(object? sender, string path)
    {
        if (Path.GetExtension(path).ToLowerInvariant() != ".mp4")
        {
            return;
        }
        LogService.WriteLine($"Video selected: {Path.GetFileName(path)}");
        _selectedVideoPath = path;
        SelectedVideoPath.Text = path;
    }

    private async void OnStartEstimateClicked(object? sender, EventArgs e)
    {
        LogService.WriteLine("Start estimate clicked");
        if (string.IsNullOrEmpty(_selectedVideoPath))
        {
            await DisplayAlert("Error", "ファイルが選択されていません", "OK");
            return;
        }

        RemoveBottomFeature("Analyze");
        PoseSelectMessage.IsVisible = false;
        LoadingIndicator.IsVisible = true;
        UpdateLayout();

        try
        {
            string? path = await App.Initializer.AnalyzeVideoAsync(_selectedVideoPath);
            if (!string.IsNullOrEmpty(path))
            {
                await DisplayAlert("Saved", $"{Path.GetFileName(path)} を保存しました", "OK");
            }
        }
        catch (Exception ex)
        {
            LogService.WriteLine($"Estimate pose failed: {ex.Message}");
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            UpdateLayout();
            _selectedVideoPath = null;
        }
    }

    private void OnCancelEstimateClicked(object? sender, EventArgs e)
    {
        LogService.WriteLine("Estimate canceled");
        _selectedVideoPath = null;
        PoseSelectMessage.IsVisible = false;
        SelectedVideoPath.Text = string.Empty;
        UpdateLayout();
    }

    private async void OnStartAdaptClicked(object? sender, EventArgs e)
    {
        LogService.WriteLine("Start adapt clicked");
        if (string.IsNullOrEmpty(_selectedPosePath))
        {
            await DisplayAlert("Error", "ファイルが選択されていません", "OK");
            return;
        }

        RemoveBottomFeature("Adapt");
        AdaptSelectMessage.IsVisible = false;
        LoadingIndicator.IsVisible = true;
        UpdateLayout();

        try
        {
            using var stream = File.OpenRead(_selectedPosePath);
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();
            var opts = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            opts.Converters.Add(new MiniMikuDance.Util.Vector3JsonConverter());
            var joints = System.Text.Json.JsonSerializer.Deserialize<MiniMikuDance.PoseEstimation.JointData[]>(json, opts);
            if (joints != null && App.Initializer.MotionGenerator != null && App.Initializer.MotionPlayer != null)
            {
                var motion = App.Initializer.MotionGenerator.Generate(joints);
                App.Initializer.Motion = motion;
                AttachFramePlayedHandler();
                if (_bottomViews.TryGetValue("ANIMATION", out var view) && view is AnimationView av2)
                {
                    av2.SetFrameCount(motion.Frames.Length);
                    av2.UpdatePlayState(true);
                }
                App.Initializer.MotionPlayer.Play(motion);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            UpdateLayout();
            _selectedPosePath = null;
        }
    }

    private void OnCancelAdaptClicked(object? sender, EventArgs e)
    {
        LogService.WriteLine("Adapt canceled");
        _selectedPosePath = null;
        AdaptSelectMessage.IsVisible = false;
        SelectedPosePath.Text = string.Empty;
        UpdateLayout();
    }

    private void UpdateSelectedBoneRotation(BoneView bv)
    {
        if (_currentModel == null) return;
        if (_selectedBoneIndex < 0 || _selectedBoneIndex >= _currentModel.Bones.Count)
            return;

        var eulerTk = new OpenTK.Mathematics.Vector3(bv.RotationX, bv.RotationY, bv.RotationZ);
        _renderer.SetBoneRotation(_selectedBoneIndex, eulerTk);
        SavePoseState();
        Viewer?.InvalidateSurface();
    }

    private void UpdateSelectedBoneTranslation(BoneView bv)
    {
        if (_currentModel == null) return;
        if (_selectedBoneIndex < 0 || _selectedBoneIndex >= _currentModel.Bones.Count)
            return;

        var t = new OpenTK.Mathematics.Vector3(bv.TranslationX, bv.TranslationY, bv.TranslationZ);
        _renderer.SetBoneTranslation(_selectedBoneIndex, t);
        SavePoseState();
        Viewer?.InvalidateSurface();
    }

    private void OnPlayAnimationRequested()
    {
        var player = App.Initializer.MotionPlayer;
        var motion = App.Initializer.Motion;
        if (player == null || motion == null)
            return;

        if (player.IsPlaying)
        {
            player.Pause();
            if (_bottomViews.TryGetValue("ANIMATION", out var v) && v is AnimationView av)
                av.UpdatePlayState(false);
        }
        else
        {
            if (player.FrameIndex >= motion.Frames.Length)
                player.Restart();
            else if (player.FrameIndex == 0)
                player.Play(motion);
            else
                player.Resume();

            if (_bottomViews.TryGetValue("ANIMATION", out var v) && v is AnimationView av)
                av.UpdatePlayState(true);
        }
    }

    private void OnAnimationFrameChanged(int frame)
    {
        var player = App.Initializer.MotionPlayer;
        if (player == null)
            return;
        player.Seek(frame);
        if (_bottomViews.TryGetValue("ANIMATION", out var v) && v is AnimationView av)
            av.SetFrameIndex(player.FrameIndex);
    }

    private void AttachFramePlayedHandler()
    {
        var player = App.Initializer.MotionPlayer;
        if (player == null)
            return;
        if (_framePlayedHandler != null)
            player.OnFramePlayed -= _framePlayedHandler;

        _framePlayedHandler = _ =>
        {
            if (_bottomViews.TryGetValue("ANIMATION", out var v) && v is AnimationView av)
            {
                av.SetFrameIndex(player.FrameIndex);
                if (!player.IsPlaying)
                    av.UpdatePlayState(false);
            }
        };

        player.OnFramePlayed += _framePlayedHandler;
    }

    private void SavePoseState()
    {
        var state = new PoseState
        {
            Rotations = _renderer.GetAllBoneRotations(),
            Translations = _renderer.GetAllBoneTranslations()
        };
        if (_poseHistoryIndex < _poseHistory.Count - 1)
            _poseHistory.RemoveRange(_poseHistoryIndex + 1, _poseHistory.Count - _poseHistoryIndex - 1);
        _poseHistory.Add(state);
        _poseHistoryIndex = _poseHistory.Count - 1;
    }

    private void UpdateBoneViewValues()
    {
        if (_bottomViews.TryGetValue("BONE", out var v) && v is BoneView bv)
        {
            if (_selectedBoneIndex >= 0)
            {
                var rot = _renderer.GetBoneRotation(_selectedBoneIndex);
                var trans = _renderer.GetBoneTranslation(_selectedBoneIndex);
                bv.SetRotation(rot);
                bv.SetTranslation(trans);
            }
        }
        Viewer?.InvalidateSurface();
    }

    private void OnBoneReset()
    {
        _renderer.ClearBoneRotations();
        SavePoseState();
        UpdateBoneViewValues();
    }

    private void OnBoneRangeChanged(int range)
    {
        if (_bottomViews.TryGetValue("BONE", out var v) && v is BoneView bv)
        {
            bv.SetRotationRange(-range, range);
        }
    }
}
