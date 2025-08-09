using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using Microsoft.Maui.Dispatching;
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
using MiniMikuDance.App;
using SixLabors.ImageSharp.PixelFormats;
using MiniMikuDance.IK;

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
    private string? _modelDir;
    private float _modelScale = 1f;
    private readonly AppSettings _settings = AppSettings.Load();

    private readonly PmxRenderer _renderer = new();
    private float _rotateSensitivity = 0.1f;
    private float _panSensitivity = 1f;
    private float _shadeShift = -0.1f;
    private float _shadeToony = 0.9f;
    private float _rimIntensity = 0.5f;
    private int _extractTotalFrames;
    private int _poseTotalFrames;
    private bool _poseMode;
    // bottomWidth is no longer used; bottom region spans full screen width
    // private double bottomWidth = 0;
    private bool _glInitialized;
    private ModelData? _pendingModel;
    private ModelData? _currentModel;
    private readonly Dictionary<long, SKPoint> _touchPoints = new();
    private readonly long[] _touchIds = new long[2];
    private readonly BonesConfig? _bonesConfig = App.Initializer.BonesConfig;
    private bool _needsRender;
    private readonly IDispatcherTimer _renderTimer;
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

    private void OnPoseModeToggled(object? sender, ToggledEventArgs e)
    {
        _poseMode = e.Value;
        _touchPoints.Clear();
        if (_poseMode && _currentModel != null)
        {
            IkManager.GenerateVrChatSkeleton(_currentModel.Bones);
            _renderer.SetIkBones(IkManager.Bones.Values);
            IkManager.PickFunc = _renderer.PickBone;
            IkManager.GetBonePositionFunc = _renderer.GetBoneWorldPosition;
            IkManager.GetCameraPositionFunc = _renderer.GetCameraPosition;
            IkManager.SetBoneRotation = _renderer.SetBoneRotation;
            IkManager.SetBoneTranslation = _renderer.SetBoneTranslation;
            IkManager.ToModelSpaceFunc = _renderer.WorldToModel;
        }
        else
        {
            IkManager.Clear();
            _renderer.ClearIkBones();
            IkManager.PickFunc = null;
            IkManager.GetBonePositionFunc = null;
            IkManager.GetCameraPositionFunc = null;
            IkManager.SetBoneRotation = null;
            IkManager.SetBoneTranslation = null;
            IkManager.ToModelSpaceFunc = null;
        }
        _renderer.ShowIkBones = _poseMode && _settings.ShowIkBones;
        Viewer?.InvalidateSurface();
    }


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
        _renderer.PanSensitivity = 1f;
        _renderer.ShadeShift = -0.1f;
        _renderer.ShadeToony = 0.9f;
        _renderer.RimIntensity = 0.5f;
        _renderer.StageSize = _settings.StageSize;
        _renderer.DefaultCameraDistance = _settings.CameraDistance;
        _renderer.DefaultCameraTargetY = _settings.CameraTargetY;
        _renderer.ShowIkBones = _poseMode && _settings.ShowIkBones;

        if (Viewer is SKGLView glView)
        {
            glView.PaintSurface += OnPaintSurface;
            glView.Touch += OnViewTouch;
            _renderer.Viewer = glView;
        }

        _renderTimer = Dispatcher.CreateTimer();
        _renderTimer.Interval = TimeSpan.FromMilliseconds(16);
        _renderTimer.Tick += (s, e) =>
        {
            if (_needsRender)
            {
                Viewer?.InvalidateSurface();
            }
        };
        _renderTimer.Start();
        _needsRender = true;

        if (SettingContent is SettingView setting)
        {
            setting.StageSize = _settings.StageSize;
            setting.StageSizeChanged += v =>
            {
                _renderer.StageSize = (float)v;
                _settings.StageSize = (float)v;
                _settings.Save();
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
            setting.ZoomSensitivityChanged += v =>
            {
                _renderer.ZoomSensitivity = (float)v;
            };
            setting.ShowBoneOutline = _renderer.ShowBoneOutline;
            setting.BoneOutlineChanged += show =>
            {
                _renderer.ShowBoneOutline = show;
                Viewer?.InvalidateSurface();
            };
            setting.ShowIkBones = _renderer.ShowIkBones;
            setting.IkBonesChanged += show =>
            {
                _settings.ShowIkBones = show;
                _renderer.ShowIkBones = _poseMode && show;
                Viewer?.InvalidateSurface();
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

    private void OnMorphClicked(object? sender, EventArgs e)
    {
        ShowBottomFeature("MORPH");
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
        sv.ZoomSensitivity = _renderer.ZoomSensitivity;
        sv.ShowBoneOutline = _renderer.ShowBoneOutline;
        sv.ShowIkBones = _renderer.ShowIkBones;
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

        AbsoluteLayout.SetLayoutBounds(
            PmxImportDialog,
            new Rect(
                0.5,
                TopMenuHeight + 20,
                AbsoluteLayout.AutoSize,
                PmxImportDialog.IsVisible ? AbsoluteLayout.AutoSize : 0));
        AbsoluteLayout.SetLayoutFlags(PmxImportDialog, AbsoluteLayoutFlags.XProportional);

        AbsoluteLayout.SetLayoutBounds(
            PoseSelectMessage,
            new Rect(
                0.5,
                TopMenuHeight + 20,
                AbsoluteLayout.AutoSize,
                PoseSelectMessage.IsVisible ? AbsoluteLayout.AutoSize : 0));
        AbsoluteLayout.SetLayoutFlags(PoseSelectMessage, AbsoluteLayoutFlags.XProportional);

        AbsoluteLayout.SetLayoutBounds(
            ProgressFrame,
            new Rect(
                0.5,
                TopMenuHeight + 20,
                AbsoluteLayout.AutoSize,
                ProgressFrame.IsVisible ? AbsoluteLayout.AutoSize : 0));
        AbsoluteLayout.SetLayoutFlags(ProgressFrame, AbsoluteLayoutFlags.XProportional);

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
                IkManager.GenerateVrChatSkeleton(_currentModel.Bones);
                _renderer.SetIkBones(IkManager.Bones.Values);
                IkManager.PickFunc = _renderer.PickBone;
                IkManager.GetBonePositionFunc = _renderer.GetBoneWorldPosition;
                IkManager.GetCameraPositionFunc = _renderer.GetCameraPosition;
                IkManager.SetBoneRotation = _renderer.SetBoneRotation;
                IkManager.SetBoneTranslation = _renderer.SetBoneTranslation;
                IkManager.ToModelSpaceFunc = _renderer.WorldToModel;
            }
        }
    }

    private void OnViewTouch(object? sender, SKTouchEventArgs e)
    {
        if (_poseMode)
        {
            if (e.ActionType == SKTouchAction.Pressed)
            {
                IkManager.PickBone(e.Location.X, e.Location.Y);
            }
            else if (e.ActionType == SKTouchAction.Moved)
            {
                var ray = _renderer.ScreenPointToRay(e.Location.X, e.Location.Y);
                var pos = IkManager.IntersectDragPlane(ray);
                if (pos.HasValue && IkManager.SelectedBoneIndex >= 0)
                {
                    IkManager.UpdateTarget(IkManager.SelectedBoneIndex, pos.Value);
                }
            }
            else if (e.ActionType == SKTouchAction.Released || e.ActionType == SKTouchAction.Cancelled)
            {
                IkManager.ReleaseSelection();
            }
            e.Handled = true;
            Viewer?.InvalidateSurface();
            _needsRender = true;
            return;
        }

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



                MiniMikuDance.Import.ModelImporter.CacheCapacity = _settings.TextureCacheSize;
                using var importer = new MiniMikuDance.Import.ModelImporter();
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
                var imagesPath = MmdFileSystem.Ensure("Movie");
                var ev = new ExplorerView(imagesPath, new[] { ".png", ".jpg", ".jpeg", ".bmp", ".webp" });
                ev.FileSelected += OnAnalyzeExplorerFileSelected;
                ev.LoadDirectory(imagesPath);
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
                var textColor = (Color)(Application.Current?.Resources?.TryGetValue("TextColor", out var textColorValue) == true ? textColorValue : Colors.Black);
                view = new Label { Text = $"{name} view", TextColor = textColor, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center };
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
                TextColor = (Color)(Application.Current?.Resources?.TryGetValue("TextColor", out var textColorValue2) == true ? textColorValue2 : Colors.Black),
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
            var imagesPath = MmdFileSystem.Ensure("Movie");
            aev.LoadDirectory(imagesPath);
        }
        else if (name == "MTOON" && _bottomViews[name] is LightingView mv)
        {
            mv.ShadeShift = _renderer.ShadeShift;
            mv.ShadeToony = _renderer.ShadeToony;
            mv.RimIntensity = _renderer.RimIntensity;
        }
        else if (name == "MORPH" && _bottomViews[name] is MorphView morphView)
        {
            if (_currentModel?.Morphs != null)
            {
                morphView.SetMorphs(_currentModel.Morphs);
            }
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
        SelectedModelPath.Text = string.Empty;
        _selectedModelPath = null;
        _modelDir = null;
        _modelScale = 1f;
        // Use the same mechanism as Estimate Pose: show bottom explorer and dialog overlay together
        ShowExplorer("Open", PmxImportDialog, SelectedModelPath, ref _selectedModelPath);
    }

    private void OnEstimatePoseClicked(object? sender, EventArgs e)
    {
        HideAllMenusAndLayout();
        ShowPoseExplorer();
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
        SelectedModelPath.Text = Path.GetFileName(path);
    }

    private async void OnImportPmxClicked(object? sender, EventArgs e)
    {

        if (string.IsNullOrEmpty(_selectedModelPath))
        {
            await DisplayAlert("Error", "ファイルが選択されていません", "OK");
            return;
        }

        RemoveBottomFeature("Open");
        PmxImportDialog.IsVisible = false;
        SetLoadingIndicatorVisibilityAndLayout(true);
        Viewer.HasRenderLoop = false;

        try
        {
            _modelScale = 1f;
            ModelImporter.CacheCapacity = _settings.TextureCacheSize;
            using var importer = new ModelImporter { Scale = _modelScale };
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
                foreach (var kvp in textureMap)
                {
                    var rel = kvp.Key;
                    var indices = kvp.Value;
                    var path = Path.Combine(_modelDir, rel.Replace('/', Path.DirectorySeparatorChar));
                    if (!File.Exists(path))
                    {
                        var fileName = Path.GetFileName(rel);
                        var found = Directory.GetFiles(_modelDir, fileName, SearchOption.AllDirectories).FirstOrDefault();
                        if (found == null)
                            continue;
                        rel = Path.GetRelativePath(_modelDir, found).Replace(Path.DirectorySeparatorChar, '/');
                        path = found;
                    }

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
            if (_bottomViews.TryGetValue("MORPH", out var view) && view is MorphView mv)
            {
                mv.SetMorphs(data.Morphs);
            }
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
            _modelDir = null;
            SelectedModelPath.Text = string.Empty;
        }
    }

    private void OnCancelImportClicked(object? sender, EventArgs e)
    {
        _selectedModelPath = null;
        PmxImportDialog.IsVisible = false;
        SelectedModelPath.Text = string.Empty;
        _modelScale = 1f;
        _modelDir = null;
        SetLoadingIndicatorVisibilityAndLayout(false);
        UpdateLayout();
    }

    private void ShowPoseExplorer()
    {
        ShowExplorer("Analyze", PoseSelectMessage, SelectedVideoPath, ref _selectedVideoPath);
    }

    private void OnAnalyzeExplorerFileSelected(object? sender, string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        var ok = ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".webp";
        if (!ok) return;
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
        // Photo mode: only show pose progress
        _extractTotalFrames = 0;
        _poseTotalFrames = 1;
        SetProgressVisibilityAndLayout(true, false, true);

        try
        {
            UpdatePoseProgress(0);
            string? path = await App.Initializer.AnalyzePhotoAsync(
                _selectedVideoPath,
                new Progress<float>(p => MainThread.BeginInvokeOnMainThread(() => UpdatePoseProgress(p))));
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
            SetProgressVisibilityAndLayout(false, false, true);
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

    private void ShowExplorer(string featureName, Border messageFrame, Label pathLabel, ref string? selectedPath)
    {
        ShowBottomFeature(featureName);
        messageFrame.IsVisible = true;
        pathLabel.Text = string.Empty;
        selectedPath = null;
        UpdateLayout();
    }

}
