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
using System.Reflection;
using MiniMikuDance.Import;
using OpenTK.Mathematics;
using MiniMikuDance.Util;
using MiniMikuDance.PoseEstimation;
using MiniMikuDance.Motion;
using MiniMikuDance.Camera;
using MiniMikuDance.App;
using SixLabors.ImageSharp.PixelFormats;

#if ANDROID
using Android.OS;
using Android.Provider;
#endif

namespace MiniMikuDanceMaui;

public partial class MainPage : ContentPage
{

    // 下部領域1 : 上部領域2 の比率となるよう初期値を設定
    private double _bottomHeightRatio = 1.0 / 3.0;
    private const double TopMenuHeight = 36;
    private bool _viewMenuOpen;
    private bool _settingMenuOpen;
    private bool _fileMenuOpen;

    private void UpdateOverlay() => MenuOverlay.IsVisible = _viewMenuOpen || _settingMenuOpen || _fileMenuOpen;
    private readonly Dictionary<string, View> _bottomViews = new();
    private readonly Dictionary<string, Border> _bottomTabs = new();
    private string? _currentFeature;
    private string? _selectedModelPath;
    private string? _selectedVideoPath;
    private string? _selectedPosePath;
    private readonly List<string> _selectedTexturePaths = new();
    private readonly List<Label> _texturePathLabels = new();
    private int _currentTextureIndex = -1;
    private string? _modelDir;
    private float _modelScale = 1f;
    private readonly AppSettings _settings = AppSettings.Load();

    private readonly PmxRenderer _renderer = new();
    private readonly CameraController _cameraController = new();
    private float _rotateSensitivity = 0.1f;
    private float _panSensitivity = 0.1f;
    private float _zoomSensitivity = 0.1f;
    private float _shadeShift = -0.1f;
    private float _shadeToony = 0.9f;
    private float _rimIntensity = 0.5f;
    private int _extractTotalFrames;
    private int _poseTotalFrames;
    private int _adaptTotalFrames;
    // bottomWidth is no longer used; bottom region spans full screen width
    // private double bottomWidth = 0;
    private bool _glInitialized;
    private ModelData? _pendingModel;
    private ModelData? _currentModel;
    private readonly Dictionary<long, SKPoint> _touchPoints = new();
    private readonly BonesConfig? _bonesConfig = App.Initializer.BonesConfig;
    private void SetProgressVisibilityAndLayout(bool isVisible,
        bool showExtract,
        bool showPose)
    {
        ProgressFrame.IsVisible = isVisible;
        ExtractProgressBar.IsVisible = showExtract;
        ExtractProgressLabel.IsVisible = showExtract;
        PoseProgressBar.IsVisible = showPose;
        PoseProgressLabel.IsVisible = showPose;

        if (!isVisible)
        {
            ExtractProgressBar.Progress = 0;
            PoseProgressBar.Progress = 0;
            ExtractProgressLabel.Text = string.Empty;
            PoseProgressLabel.Text = string.Empty;
        }
        UpdateLayout();
    }

    private void UpdateExtractProgress(double p)
    {
        int current = (int)(_extractTotalFrames * p);
        ExtractProgressBar.Progress = p;
        ExtractProgressLabel.Text = $"動画抽出: {current}/{_extractTotalFrames} ({p * 100:0}%)";
    }

    private void UpdatePoseProgress(double p)
    {
        int current = (int)(_poseTotalFrames * p);
        PoseProgressBar.Progress = p;
        PoseProgressLabel.Text = $"姿勢推定: {current}/{_poseTotalFrames} ({p * 100:0}%)";
    }

    private void UpdateAdaptProgress(double p)
    {
        int current = (int)(_adaptTotalFrames * p);
        PoseProgressBar.Progress = p;
        PoseProgressLabel.Text = $"ポーズ適用: {current}/{_adaptTotalFrames} ({p * 100:0}%)";
    }

    public MotionPlayer? MotionPlayer => App.Initializer.MotionPlayer;

    private static string GetAppPackageDirectory()
    {
        // AppPackageDirectory が利用可能な環境ではそれを優先的に使用する
        var dirProp = typeof(FileSystem).GetProperty("AppPackageDirectory", BindingFlags.Public | BindingFlags.Static);
        if (dirProp?.GetValue(null) is string dir && !string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            return dir;

        if (!string.IsNullOrEmpty(FileSystem.AppDataDirectory) && Directory.Exists(FileSystem.AppDataDirectory))
            return FileSystem.AppDataDirectory;

        var baseDir = AppContext.BaseDirectory;
        if (!string.IsNullOrEmpty(baseDir) && Directory.Exists(baseDir))
            return baseDir;

        return System.Environment.CurrentDirectory;
    }


    public MainPage()
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
        this.SizeChanged += OnSizeChanged;
        _renderer.RotateSensitivity = 0.1f;
        _renderer.PanSensitivity = 0.1f;
        _renderer.ZoomSensitivity = _settings.ZoomSensitivity;
        _zoomSensitivity = _settings.ZoomSensitivity;
        _renderer.ShadeShift = -0.1f;
        _renderer.ShadeToony = 0.9f;
        _renderer.RimIntensity = 0.5f;
        _renderer.StageSize = _settings.StageSize;
        _renderer.DefaultCameraDistance = _settings.CameraDistance;
        _renderer.DefaultCameraTargetY = _settings.CameraTargetY;

        if (Viewer is SKGLView glView)
        {
            glView.PaintSurface += OnPaintSurface;
            glView.Touch += OnViewTouch;
        }

        if (SettingContent is SettingView setting)
        {
            setting.StageSize = _settings.StageSize;
            setting.StageSizeChanged += v =>
            {
                _renderer.StageSize = (float)v;
                _settings.StageSize = (float)v;
                _settings.Save();
            };
            setting.ZoomSensitivity = _settings.ZoomSensitivity;
            setting.ZoomSensitivityChanged += v =>
            {
                _renderer.ZoomSensitivity = (float)v;
                _settings.ZoomSensitivity = (float)v;
                _settings.Save();
                _zoomSensitivity = (float)v;
            };
            setting.HeightRatioChanged += ratio =>
            {
                _bottomHeightRatio = ratio;
                UpdateLayout();
            };
            setting.RotateSensitivityChanged += v =>
            {
                _renderer.RotateSensitivity = (float)v;
            };
            setting.PanSensitivityChanged += v =>
            {
                _renderer.PanSensitivity = (float)v;
            };
            setting.CameraLockChanged += locked =>
            {
                _renderer.CameraLocked = locked;
            };
            setting.ShowBoneOutline = _renderer.ShowBoneOutline;
            setting.BoneOutlineChanged += show =>
            {
                _renderer.ShowBoneOutline = show;
                Viewer?.InvalidateSurface();
            };
            setting.ResetCameraRequested += () =>
            {
                _renderer.ResetCamera();
                Viewer?.InvalidateSurface();
            };
        }

        App.Initializer.OnMotionApplied += OnMotionApplied;
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
        HideAllMenus();
        _viewMenuOpen = !_viewMenuOpen;
        ViewMenu.IsVisible = _viewMenuOpen;
        UpdateOverlay();
        UpdateLayout();
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
            sv.RotateSensitivity = _renderer.RotateSensitivity;
            sv.PanSensitivity = _renderer.PanSensitivity;
            sv.ZoomSensitivity = _renderer.ZoomSensitivity;
            sv.CameraLocked = _renderer.CameraLocked;
        }
        UpdateOverlay();
    }

    private void OnSettingMenuTapped(object? sender, EventArgs e)
    {
        HideAllMenus();
        _settingMenuOpen = !_settingMenuOpen;
        SettingMenu.IsVisible = _settingMenuOpen;
        UpdateOverlay();
        UpdateLayout();
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
        HideAllMenus();
        _fileMenuOpen = !_fileMenuOpen;
        FileMenu.IsVisible = _fileMenuOpen;
        UpdateOverlay();
        UpdateLayout();
    }


    private async void OnSelectClicked(object? sender, EventArgs e)
    {
        HideAllMenusAndLayout();
        await ShowModelSelector();
    }

    private void OnBoneClicked(object? sender, EventArgs e)
    {
        ShowBottomFeature("BONE");
        HideAllMenusAndLayout();
    }


    private void OnLightingClicked(object? sender, EventArgs e)
    {
        ShowBottomFeature("MTOON");
        HideAllMenusAndLayout();
    }

    private void OnGyroMenuClicked(object? sender, EventArgs e)
    {
        ShowBottomFeature("GYRO");
        HideAllMenusAndLayout();
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
        HideAllMenusAndLayout();
    }

    private void OnOverlayTapped(object? sender, TappedEventArgs e)
    {
        HideAllMenusAndLayout();
    }

    private void OnBottomRegionTapped(object? sender, TappedEventArgs e)
    {
        HideAllMenusAndLayout();
    }

    private void HideAllMenus()
    {
        _viewMenuOpen = false;
        ViewMenu.IsVisible = false;
        _settingMenuOpen = false;
        SettingMenu.IsVisible = false;
        _fileMenuOpen = false;
        FileMenu.IsVisible = false;
        UpdateOverlay();
        UpdateLayout();
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

    private void HideAllMenusAndLayout()
    {
        HideViewMenu();
        HideSettingMenu();
        HideFileMenu();
        UpdateLayout();
    }

    private void UpdateSettingViewProperties(SettingView? sv)
    {
        if (sv == null || _renderer == null)
            return;

        sv.HeightRatio = _bottomHeightRatio;
        sv.RotateSensitivity = _rotateSensitivity;
        sv.PanSensitivity = _panSensitivity;
        sv.ZoomSensitivity = _zoomSensitivity;
        sv.CameraLocked = _renderer.CameraLocked;
        sv.ShowBoneOutline = _renderer.ShowBoneOutline;
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
    }

    private void UpdateBoneViewValues()
    {
        Viewer?.InvalidateSurface();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
#if ANDROID
        var readStatus = await Permissions.RequestAsync<Permissions.StorageRead>();
        var writeStatus = await Permissions.RequestAsync<Permissions.StorageWrite>();
        if (readStatus != PermissionStatus.Granted || writeStatus != PermissionStatus.Granted)
        {
            // Permissions not granted, consider showing a message to the user
        }
        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            if (!Android.OS.Environment.IsExternalStorageManager)
            {
                try
                {
                    var context = Android.App.Application.Context;
                    if (context != null)
                    {
                        var uri = Android.Net.Uri.Parse($"package:{context.PackageName}");
                        var intent = new Android.Content.Intent(Settings.ActionManageAppAllFilesAccessPermission, uri);
                        intent.AddFlags(Android.Content.ActivityFlags.NewTask);
                        context.StartActivity(intent);
                    }
                }
                catch (Exception)
                {
                    // Handle exception if launching settings fails
                }
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
        if (TopMenu == null || ViewMenu == null || FileMenu == null || SettingMenu == null ||
            MenuOverlay == null || PmxImportDialog == null || PoseSelectMessage == null ||
            AdaptSelectMessage == null || ProgressFrame == null || LoadingIndicator == null ||
            Viewer == null || BottomRegion == null)
            return;

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

        AbsoluteLayout.SetLayoutBounds(PmxImportDialog, new Rect(0.5, TopMenuHeight + 20, 0.8,
            PmxImportDialog.IsVisible ? AbsoluteLayout.AutoSize : 0));
        AbsoluteLayout.SetLayoutFlags(PmxImportDialog,
            AbsoluteLayoutFlags.XProportional | AbsoluteLayoutFlags.WidthProportional);
        AbsoluteLayout.SetLayoutBounds(PoseSelectMessage, new Rect(0.5, TopMenuHeight + 20, 0.8,
            PoseSelectMessage.IsVisible ? AbsoluteLayout.AutoSize : 0));
        AbsoluteLayout.SetLayoutFlags(PoseSelectMessage,
            AbsoluteLayoutFlags.XProportional | AbsoluteLayoutFlags.WidthProportional);
        AbsoluteLayout.SetLayoutBounds(AdaptSelectMessage, new Rect(0.5, TopMenuHeight + 20, 0.8,
            AdaptSelectMessage.IsVisible ? AbsoluteLayout.AutoSize : 0));
        AbsoluteLayout.SetLayoutFlags(AdaptSelectMessage,
            AbsoluteLayoutFlags.XProportional | AbsoluteLayoutFlags.WidthProportional);
        AbsoluteLayout.SetLayoutBounds(ProgressFrame, new Rect(0.5, TopMenuHeight + 20, 0.8,
            ProgressFrame.IsVisible ? AbsoluteLayout.AutoSize : 0));
        AbsoluteLayout.SetLayoutFlags(ProgressFrame,
            AbsoluteLayoutFlags.XProportional | AbsoluteLayoutFlags.WidthProportional);

        AbsoluteLayout.SetLayoutBounds(LoadingIndicator, new Rect(0.5, 0.5, 40, 40));
        AbsoluteLayout.SetLayoutFlags(LoadingIndicator, AbsoluteLayoutFlags.PositionProportional);

        double viewerWidth = Math.Min(W, 2048);
        double viewerHeight = Math.Min(H, 2048);
        AbsoluteLayout.SetLayoutBounds(Viewer, new Rect(0, 0, viewerWidth, viewerHeight));
        AbsoluteLayout.SetLayoutFlags(Viewer, AbsoluteLayoutFlags.None);

        // Position bottom region to span full width of the page
        AbsoluteLayout.SetLayoutBounds(BottomRegion,
            new Rect(0,
                H - bottomHeight - safe.Bottom,
                W,
                bottomHeight));
        AbsoluteLayout.SetLayoutFlags(BottomRegion, AbsoluteLayoutFlags.None);

        if (_bottomViews.TryGetValue("TIMELINE", out var timelineView) && timelineView is TimelineView tv)
        {
            tv.RefreshScrollViews();
        }
    }






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

        _renderer.Resize(e.BackendRenderTarget.Width, e.BackendRenderTarget.Height);
        _renderer.Render();
        GL.Flush();
    }

    private void LoadPendingModel()
    {
        if (_pendingModel != null)
        {
            _renderer.ClearBoneRotations();
            _renderer.LoadModel(_pendingModel);
            _currentModel = _pendingModel;
            App.Initializer.UpdateApplier(_currentModel);
            _shadeShift = _pendingModel.ShadeShift;
            _shadeToony = _pendingModel.ShadeToony;
            _rimIntensity = _pendingModel.RimIntensity;
            UpdateRendererLightingProperties();
            _pendingModel = null;
        }
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
                PickerTitle = "Select PMX file",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    [DevicePlatform.Android] = new[] { "application/octet-stream", ".pmx", ".pmd" },
                    [DevicePlatform.WinUI] = new[] { ".pmx", ".pmd" },
                    [DevicePlatform.iOS] = new[] { ".pmx", ".pmd" }
                })
            });

            if (result != null)
            {
                var ext = Path.GetExtension(result.FileName).ToLowerInvariant();
                if (ext != ".pmx" && ext != ".pmd")
                {
                    await DisplayAlert("Invalid File", "Please select a .pmx or .pmd file.", "OK");
                    return;
                }



                var importer = new MiniMikuDance.Import.ModelImporter();
                MiniMikuDance.Import.ModelData data;
                if (!string.IsNullOrEmpty(result.FullPath))
                {
                    data = importer.ImportModel(result.FullPath);
                }
                else
                {
                    await using var stream = await result.OpenReadAsync();
                    string? dir = null;
                    try
                    {
                        var pkgDir = GetAppPackageDirectory();
                        var assetsDir = Path.Combine(pkgDir, "StreamingAssets");
                        if (Directory.Exists(assetsDir))
                        {
                            dir = Directory.EnumerateFiles(assetsDir, result.FileName, SearchOption.AllDirectories)
                                .Select(Path.GetDirectoryName)
                                .FirstOrDefault(d => d != null);
                        }
                    }
                    catch
                    {
                        dir = null;
                    }
                    data = importer.ImportModel(stream, dir);
                }
                _renderer.LoadModel(data);
                _currentModel = data;
                _shadeShift = data.ShadeShift;
                _shadeToony = data.ShadeToony;
                _rimIntensity = data.RimIntensity;
                UpdateRendererLightingProperties();
                Viewer?.InvalidateSurface();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");

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
                var ev = new ExplorerView(modelsPath, new[] { ".pmx", ".pmd" });
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
                var ev = new ExplorerView(posePath, new[] { ".csv" });
                ev.FileSelected += OnAdaptExplorerFileSelected;
                ev.LoadDirectory(posePath);
                view = ev;
            }
            else if (name == "Texture")
            {
                var texPath = MmdFileSystem.Ensure("Models");
                var ev = new ExplorerView(texPath, new[] { ".png", ".jpg", ".jpeg", ".tga" });
                ev.FileSelected += OnTexExplorerFileSelected;
                ev.LoadDirectory(texPath);
                view = ev;
            }
            else if (name == "BONE")
            {
                var bv = new BoneView();
                SetupBoneView(bv);
                view = bv;
            }
            else if (name == "CAMERA")
            {
                var cv = new CameraView(_renderer);
                view = cv;
            }
            else if (name == "GYRO")
            {
                var gv = new GyroView(_cameraController, _renderer);
                view = gv;
            }
            else if (name == "PMX")
            {
                var pv = new PmxView();
                pv.SetModel(_currentModel);
                view = pv;
            }

            else if (name == "TIMELINE")
            {
                var tv = new TimelineView();
                tv.Model = _currentModel;
                tv.CurrentFrameChanged += OnTimelineFrameChanged;
                if (_currentModel != null)
                {
                    ApplyTimelineFrame(tv, tv.CurrentFrame);
                    Viewer?.InvalidateSurface();
                }
                view = tv;
            }
            else if (name == "MTOON")
            {
                var mv = new LightingView
                {
                    ShadeShift = _shadeShift,
                    ShadeToony = _shadeToony,
                    RimIntensity = _rimIntensity
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
                view = mv;
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
                sv.ZoomSensitivityChanged += v =>
                {
                    if (_renderer != null)
                        _renderer.ZoomSensitivity = (float)v;
                    if (_settings != null)
                    {
                        _settings.ZoomSensitivity = (float)v;
                        _settings.Save();
                    }
                    _zoomSensitivity = (float)v;
                };
                sv.CameraLockChanged += locked =>
                {
                    if (_renderer != null)
                        _renderer.CameraLocked = locked;
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
                var textColor = (Color)Application.Current.Resources["TextColor"];
                view = new Label { Text = $"{name} view", TextColor = textColor, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center };
            }
            _bottomViews[name] = view;

            var tabBgColor = (Color)Application.Current.Resources["TabBackgroundColor"];
            var border = new Border
            {
                BackgroundColor = tabBgColor,
                Padding = new Thickness(8, 2),
                MinimumWidthRequest = 60
            };
            var label = new Label
            {
                Text = name,
                TextColor = (Color)Application.Current.Resources["TextColor"],
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
                HideAllMenusAndLayout();
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
        else if (name == "Texture" && _bottomViews[name] is ExplorerView tev)
        {
            var texPath = MmdFileSystem.Ensure("Models");
            tev.LoadDirectory(texPath);
        }
        else if (name == "MTOON" && _bottomViews[name] is LightingView mv)
        {
            mv.ShadeShift = _renderer.ShadeShift;
            mv.ShadeToony = _renderer.ShadeToony;
            mv.RimIntensity = _renderer.RimIntensity;
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
        var active = (Color)Application.Current.Resources["TabActiveColor"];
        var inactive = (Color)Application.Current.Resources["TabInactiveColor"];
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

    private async void OnAddToLibraryClicked(object? sender, EventArgs e)
    {

        HideFileMenu();
        await AddToLibraryAsync();
    }

    private async Task AddToLibraryAsync()
    {

        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select PMX file",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    [DevicePlatform.Android] = new[] { "application/octet-stream", ".pmx", ".pmd" },
                    [DevicePlatform.WinUI] = new[] { ".pmx", ".pmd" },
                    [DevicePlatform.iOS] = new[] { ".pmx", ".pmd" }
                })
            });

            if (result == null) return;

            string dstDir = MmdFileSystem.Ensure("Models");
            string dstPath = Path.Combine(dstDir, Path.GetFileName(result.FullPath));
            await using Stream src = await result.OpenReadAsync();
            await using FileStream dst = File.Create(dstPath);
            await src.CopyToAsync(dst);



            await DisplayAlert("Copied", $"{Path.GetFileName(dstPath)} をライブラリに追加しました", "OK");
        }
        catch (Exception ex)
        {

            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private void OnOpenInViewerClicked(object? sender, EventArgs e)
    {
        HideAllMenusAndLayout();
        PmxImportDialog.IsVisible = true;
        SelectedModelPath.Text = string.Empty;
        ScaleEntry.Text = "1.0";
        _selectedModelPath = null;
        _selectedTexturePaths.Clear();
        _texturePathLabels.Clear();
        TextureList.Children.Clear();
        _currentTextureIndex = -1;
        AddTextureRow();
        _modelScale = 1f;
        UpdateLayout();
    }

    private void OnSelectPmxModelClicked(object? sender, EventArgs e)
    {
        ShowModelExplorer();
    }

    private void OnSelectPmxTextureClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is int idx)
        {
            _currentTextureIndex = idx;
            string? tmp = null;
            ShowExplorer("Texture", PmxImportDialog, _texturePathLabels[idx], ref tmp);
        }
    }

    private void OnAddTextureRowClicked(object? sender, EventArgs e)
    {
        AddTextureRow();
    }

    private void AddTextureRow()
    {
        var textColor = (Color)Application.Current.Resources["TextColor"];
        int index = _selectedTexturePaths.Count;
        var row = new Grid
        {
            ColumnSpacing = 6,
            ColumnDefinitions =
        {
            new ColumnDefinition { Width = GridLength.Auto },
            new ColumnDefinition { Width = GridLength.Star },
            new ColumnDefinition { Width = GridLength.Auto }
        }
        };
        var nameLabel = new Label
        {
            Text = index == 0 ? "Texture" : string.Empty,
            TextColor = textColor,
            WidthRequest = 60
        };
        var pathLabel = new Label
        {
            TextColor = textColor,
            FontSize = 14,
            HorizontalOptions = LayoutOptions.Fill,
            LineBreakMode = LineBreakMode.CharacterWrap,
            MaxLines = 2,
            WidthRequest = 200
        };
        var button = new Button
        {
            Text = "参照",
            CommandParameter = index
        };
        button.Clicked += OnSelectPmxTextureClicked;

        row.Children.Add(nameLabel);
        row.Children.Add(pathLabel);
        row.Children.Add(button);
        Grid.SetColumn(nameLabel, 0);
        Grid.SetColumn(pathLabel, 1);
        Grid.SetColumn(button, 2);

        TextureList.Children.Add(row);
        _selectedTexturePaths.Add(string.Empty);
        _texturePathLabels.Add(pathLabel);
    }

    private void OnEstimatePoseClicked(object? sender, EventArgs e)
    {
        HideAllMenusAndLayout();
        ShowPoseExplorer();
    }

    private void OnAdaptPoseClicked(object? sender, EventArgs e)
    {
        HideAllMenusAndLayout();
        ShowAdaptExplorer();
    }

    private async void ShowModelExplorer()
    {
        var modelsPath = MmdFileSystem.Ensure("Models");

#if ANDROID
        var readStatus = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
        if (readStatus != PermissionStatus.Granted)
        {
            await DisplayAlert("Error", "ストレージ読み取り権限がありません", "OK");
            return;
        }
#endif

#if IOS
    // iOS では追加の権限は不要だがパスの存在を確認する
#endif
        if (!Directory.Exists(modelsPath))
        {
            await DisplayAlert("Error", $"モデルディレクトリが見つかりません: {modelsPath}", "OK");
            return;
        }

        ShowExplorer("Open", PmxImportDialog, SelectedModelPath, ref _selectedModelPath);
    }

    private void OnOpenExplorerFileSelected(object? sender, string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext != ".pmx" && ext != ".pmd")
        {
            return;
        }

        _selectedModelPath = path;
        _modelDir = Path.GetDirectoryName(path);
        SelectedModelPath.Text = path;
    }

    private async void OnImportPmxClicked(object? sender, EventArgs e)
    {

        if (string.IsNullOrEmpty(_selectedModelPath))
        {
            await DisplayAlert("Error", "ファイルが選択されていません", "OK");
            return;
        }

        RemoveBottomFeature("Open");
        RemoveBottomFeature("Texture");
        PmxImportDialog.IsVisible = false;
        SetLoadingIndicatorVisibilityAndLayout(true);
        Viewer.HasRenderLoop = false;

        try
        {
            if (!float.TryParse(ScaleEntry.Text, out _modelScale))
            {
                _modelScale = 1f;
            }

            var importer = new ModelImporter { Scale = _modelScale };
            var data = await Task.Run(() => importer.ImportModel(_selectedModelPath));

            // PMX内のテクスチャ相対パスとサブメッシュインデックスの対応表を作成
            var textureMap = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < data.SubMeshes.Count; i++)
            {
                var texPath = data.SubMeshes[i].TextureFilePath;
                if (string.IsNullOrEmpty(texPath))
                    continue;

                if (!textureMap.TryGetValue(texPath, out var list))
                {
                    list = new List<int>();
                    textureMap[texPath] = list;
                }
                list.Add(i);
            }

            if (!string.IsNullOrEmpty(_modelDir))
            {
                foreach (var rel in _selectedTexturePaths)
                {
                    if (string.IsNullOrEmpty(rel))
                        continue;

                    if (!textureMap.TryGetValue(rel, out var indices))
                        continue;

                    var path = Path.Combine(_modelDir, rel.Replace('/', Path.DirectorySeparatorChar));
                    await using var stream = File.OpenRead(path);
                    using var image = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(stream);

                    foreach (var idx in indices)
                    {
                        var sm = data.SubMeshes[idx];
                        sm.TextureBytes = new byte[image.Width * image.Height * 4];
                        image.CopyPixelDataTo(sm.TextureBytes);
                        sm.TextureWidth = image.Width;
                        sm.TextureHeight = image.Height;
                        sm.TextureFilePath = rel;
                    }
                    LogService.WriteLine($"Texture {rel} mapped to indices: {string.Join(",", indices)}");
                }
            }

            _pendingModel = data;
            Viewer.InvalidateSurface();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
            LogService.WriteLine($"Import failed: {ex.Message}");
        }
        finally
        {
            Viewer.HasRenderLoop = true;
            SetLoadingIndicatorVisibilityAndLayout(false);
            _selectedModelPath = null;
            _selectedTexturePaths.Clear();
            _texturePathLabels.Clear();
            TextureList.Children.Clear();
            SelectedModelPath.Text = string.Empty;
            _currentTextureIndex = -1;
        }
    }

    private void OnCancelImportClicked(object? sender, EventArgs e)
    {
        _selectedModelPath = null;
        _selectedTexturePaths.Clear();
        _texturePathLabels.Clear();
        TextureList.Children.Clear();
        PmxImportDialog.IsVisible = false;
        SelectedModelPath.Text = string.Empty;
        ScaleEntry.Text = "1.0";
        _modelScale = 1f;
        _currentTextureIndex = -1;
        SetLoadingIndicatorVisibilityAndLayout(false);
        UpdateLayout();
    }

    private void OnTexExplorerFileSelected(object? sender, string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext != ".png" && ext != ".jpg" && ext != ".jpeg" && ext != ".tga")
        {
            return;
        }
        if (PmxImportDialog.IsVisible && _modelDir != null)
        {
            if (_currentTextureIndex < 0)
                _currentTextureIndex = 0;

            while (_currentTextureIndex >= _texturePathLabels.Count)
                AddTextureRow();

            var rel = Path.GetRelativePath(_modelDir, path)
                .Replace(Path.DirectorySeparatorChar, '/');
            _selectedTexturePaths[_currentTextureIndex] = rel;
            _texturePathLabels[_currentTextureIndex].Text = path;
            _currentTextureIndex++;
        }
    }

    private void ShowAdaptExplorer()
    {
        ShowExplorer("Adapt", AdaptSelectMessage, SelectedPosePath, ref _selectedPosePath);
    }

    private void OnAdaptExplorerFileSelected(object? sender, string path)
    {
        if (Path.GetExtension(path).ToLowerInvariant() != ".csv")
        {
            return;
        }

        _selectedPosePath = path;
        SelectedPosePath.Text = path;
    }

    private void ShowPoseExplorer()
    {
        ShowExplorer("Analyze", PoseSelectMessage, SelectedVideoPath, ref _selectedVideoPath);
    }

    private void OnAnalyzeExplorerFileSelected(object? sender, string path)
    {
        if (Path.GetExtension(path).ToLowerInvariant() != ".mp4")
        {
            return;
        }

        _selectedVideoPath = path;
        SelectedVideoPath.Text = path;
    }

    private async void OnStartEstimateClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedVideoPath))
        {
            await DisplayAlert("Error", "ファイルが選択されていません", "OK");
            return;
        }

        RemoveBottomFeature("Analyze");
        PoseSelectMessage.IsVisible = false;
        SetProgressVisibilityAndLayout(true, true, true);

        try
        {
            _extractTotalFrames = await App.Initializer.FrameExtractor.GetFrameCountAsync(_selectedVideoPath, 30);
            _poseTotalFrames = _extractTotalFrames;
            UpdateExtractProgress(0);
            UpdatePoseProgress(0);

            string? path = await App.Initializer.AnalyzeVideoAsync(
                _selectedVideoPath,
                p => MainThread.BeginInvokeOnMainThread(() => UpdateExtractProgress(p)),
                p => MainThread.BeginInvokeOnMainThread(() => UpdatePoseProgress(p)));
            if (!string.IsNullOrEmpty(path))
            {
                await DisplayAlert("Saved", $"{Path.GetFileName(path)} を保存しました", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            SetProgressVisibilityAndLayout(false, true, true);
            _selectedVideoPath = null;
        }
    }

    private void OnCancelEstimateClicked(object? sender, EventArgs e)
    {
        _selectedVideoPath = null;
        PoseSelectMessage.IsVisible = false;
        SelectedVideoPath.Text = string.Empty;
        SetProgressVisibilityAndLayout(false, true, true);
    }

    private async void OnStartAdaptClicked(object? sender, EventArgs e)
    {
        if (_currentModel == null)
        {
            await DisplayAlert("Error", "PMXモデルが読み込まれていません。先にモデルをインポートしてください。", "OK");
            return;
        }
        if (string.IsNullOrEmpty(_selectedPosePath))
        {
            await DisplayAlert("Error", "ファイルが選択されていません", "OK");
            return;
        }

        RemoveBottomFeature("Adapt");
        AdaptSelectMessage.IsVisible = false;
        SetProgressVisibilityAndLayout(true, false, true);

        if (_pendingModel != null)
        {
            await DisplayAlert("Info", "モデルの読み込みが完了してから実行してください", "OK");
            SetProgressVisibilityAndLayout(false, false, true);
            return;
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(_selectedPosePath);
            if (lines.Length <= 1)
                throw new InvalidOperationException("CSVが空です");

            var header = lines[0].Split(',');
            var posColumns = new Dictionary<string, (int X, int Y, int Z)>(StringComparer.OrdinalIgnoreCase);
            var rotColumns = new Dictionary<string, (int X, int Y, int Z)>(StringComparer.OrdinalIgnoreCase);
            for (int i = 3; i + 2 < header.Length; i += 3)
            {
                var parts = header[i].Split('.');
                if (parts.Length < 2)
                    continue;
                var name = parts[0];
                if (parts[1].StartsWith("deg", StringComparison.OrdinalIgnoreCase))
                    rotColumns[name] = (i, i + 1, i + 2);
                else
                    posColumns[name] = (i, i + 1, i + 2);
            }

            var offsets = new Dictionary<string, System.Numerics.Quaternion>(StringComparer.OrdinalIgnoreCase)
            {
                ["leftUpperArm"] = System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.UnitX, MathF.PI / 4f),
                ["rightUpperArm"] = System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.UnitX, MathF.PI / 4f),
                ["leftShoulder"] = System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.UnitX, MathF.PI / 18f),
                ["rightShoulder"] = System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.UnitX, MathF.PI / 18f),
            };

            int total = lines.Length - 1;
            _adaptTotalFrames = total;
            UpdateAdaptProgress(0);

            if (!_bottomViews.ContainsKey("TIMELINE"))
                ShowBottomFeature("TIMELINE");

            if (_bottomViews.TryGetValue("TIMELINE", out var view) && view is TimelineView tv)
            {
                tv.ClearKeyframes();
                tv.UpdateMaxFrame(_adaptTotalFrames);

                var bones = HumanoidBones.StandardOrder;
                for (int f = 0; f < total; f++)
                {
                    var parts = lines[1 + f].Split(',');

                    var hipsTrans = Vector3.Zero;
                    if (posColumns.TryGetValue("hips", out var pc))
                    {
                        float.TryParse(parts[pc.X], out float tx);
                        float.TryParse(parts[pc.Y], out float ty);
                        float.TryParse(parts[pc.Z], out float tz);
                        hipsTrans = new Vector3(tx, -ty, -tz);
                    }

                    foreach (var bone in bones)
                    {
                        System.Numerics.Quaternion q = System.Numerics.Quaternion.Identity;
                        if (rotColumns.TryGetValue(bone, out var c))
                        {
                            float.TryParse(parts[c.X], out float ax);
                            float.TryParse(parts[c.Y], out float ay);
                            float.TryParse(parts[c.Z], out float az);

                            // 入力値は度数法で渡される
                            const float Deg2Rad = MathF.PI / 180f;
                            q = AxisAngleToQuaternion(ax * Deg2Rad, ay * Deg2Rad, az * Deg2Rad);
                            q = new System.Numerics.Quaternion(q.X, -q.Y, -q.Z, q.W);

                            if (offsets.TryGetValue(bone, out var off))
                                q = System.Numerics.Quaternion.Concatenate(System.Numerics.Quaternion.Concatenate(System.Numerics.Quaternion.Inverse(off), q), off);
                        }

                        var euler = q.ToEulerDegrees().ToOpenTK();
                        if (_currentModel != null && _currentModel.HumanoidBones.TryGetValue(bone, out _))
                        {
                            var trans = bone.Equals("hips", StringComparison.OrdinalIgnoreCase) ? hipsTrans : Vector3.Zero;
                            tv.AddKeyframe(bone, f, trans, euler);
                        }
                    }

                    UpdateAdaptProgress((f + 1) / (double)_adaptTotalFrames);
                    await Task.Delay(1);
                }

                ApplyTimelineFrame(tv, 0);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            SetProgressVisibilityAndLayout(false, false, true);
            _selectedPosePath = null;
        }
    }

    private void OnCancelAdaptClicked(object? sender, EventArgs e)
    {
        _selectedPosePath = null;
        AdaptSelectMessage.IsVisible = false;
        SelectedPosePath.Text = string.Empty;
        SetProgressVisibilityAndLayout(false, false, true);
    }

    private void OnPlayAnimationRequested()
    {
        try
        {
            var player = App.Initializer.MotionPlayer;
            var motion = App.Initializer.Motion;
            if (player == null || motion == null)
                return;

            if (player.IsPlaying)
            {
                player.Pause();
            }
            else
            {
                if (player.FrameIndex >= motion.Frames.Length)
                    player.Restart();
                else if (player.FrameIndex == 0)
                    player.Play(motion);
                else
                    player.Resume();
            }
        }
        catch (Exception)
        {
            // Handle exception
        }
    }

    private void OnAnimationFrameChanged(int frame)
    {
        var player = App.Initializer.MotionPlayer;
        if (player == null)
            return;
        player.Seek(frame);
        Viewer?.InvalidateSurface();
    }

    private void OnTimelineFrameChanged(int frame)
    {
        if (_currentModel == null)
            return;
        if (!_bottomViews.TryGetValue("TIMELINE", out var view) || view is not TimelineView tv)
            return;

        ApplyTimelineFrame(tv, frame);

        Viewer?.InvalidateSurface();
    }

    private void ApplyTimelineFrame(TimelineView tv, int frame)
    {
        if (_currentModel == null)
            return;

        foreach (var bone in tv.BoneNames)
        {
            if (!_currentModel.HumanoidBones.TryGetValue(bone, out int index))
                continue;
            // 自動補間済みの値を取得
            var t = tv.GetBoneTranslationAtFrame(bone, frame);
            var r = tv.GetBoneRotationAtFrame(bone, frame);
            _renderer.SetBoneTranslation(index, t);
            _renderer.SetBoneRotation(index, ClampRotation(bone, r));
        }
    }

    private Vector3 ClampRotation(string bone, Vector3 rot)
    {
        if (_bonesConfig == null)
            return rot;

        var clamped = _bonesConfig.Clamp(bone, rot.ToNumerics());
        return clamped.ToOpenTK();
    }

    private static System.Numerics.Quaternion AxisAngleToQuaternion(float x, float y, float z)
    {
        var axis = new System.Numerics.Vector3(x, y, z);
        float angle = axis.Length();
        if (angle < 1e-6f)
            return System.Numerics.Quaternion.Identity;
        axis /= angle;
        return System.Numerics.Quaternion.CreateFromAxisAngle(axis, angle);
    }



    private void SetLoadingIndicatorVisibilityAndLayout(bool isVisible)
    {
        LoadingIndicator.IsVisible = isVisible;
        UpdateLayout();
    }

    private void ShowExplorer(string featureName, Frame messageFrame, Label pathLabel, ref string? selectedPath)
    {
        ShowBottomFeature(featureName);
        messageFrame.IsVisible = true;
        pathLabel.Text = string.Empty;
        selectedPath = null;
        UpdateLayout();
    }

    private void OnMotionApplied((Dictionary<int, System.Numerics.Quaternion> rotations, System.Numerics.Matrix4x4 transform) data)
    {
        foreach (var kv in data.rotations)
        {
            var euler = ToEulerAngles(kv.Value);
            _renderer.SetBoneRotation(kv.Key, new Vector3(euler.X, euler.Y, euler.Z));
        }

        _renderer.ModelTransform = data.transform.ToMatrix4();

        // hips の平行移動は別途管理するためゼロにリセットする
        if (_currentModel != null && _currentModel.HumanoidBones.TryGetValue("hips", out var hipsIdx))
        {
            _renderer.SetBoneTranslation(hipsIdx, Vector3.Zero);
        }

        Viewer?.InvalidateSurface();
    }

    private static Vector3 ToEulerAngles(System.Numerics.Quaternion q)
    {
        // Z→X→Y 順を使用する
        const float rad2deg = 180f / MathF.PI;
        var m = System.Numerics.Matrix4x4.CreateFromQuaternion(q);
        float sx = -m.M23;
        float cx = MathF.Sqrt(1 - sx * sx);
        float x, y, z;
        if (cx > 1e-6f)
        {
            x = MathF.Asin(sx);
            y = MathF.Atan2(m.M13, m.M33);
            z = MathF.Atan2(m.M21, m.M22);
        }
        else
        {
            x = MathF.Asin(sx);
            y = MathF.Atan2(-m.M31, m.M11);
            z = 0f;
        }
        return new Vector3(x * rad2deg, y * rad2deg, z * rad2deg);
    }
}
