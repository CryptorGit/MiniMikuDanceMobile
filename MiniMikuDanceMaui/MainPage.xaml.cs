using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using SkiaSharp;
using OpenTK.Graphics.ES30;
using GL = OpenTK.Graphics.ES30.GL;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Devices;
using System.IO;
using System.Linq;
using OpenTK.Mathematics;
using MiniMikuDance.Util;
using MiniMikuDance.PoseEstimation;
using MiniMikuDance.Motion;
using MiniMikuDance.Camera;
using MiniMikuDance.App;
using MiniMikuDance.Import;
using System.Runtime.Versioning;

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
    private readonly AppSettings _settings = AppSettings.Load();

    private readonly PmxRenderer _renderer = new();
    private readonly CameraController _cameraController = new();
    private float _rotateSensitivity = 0.1f;
    private float _panSensitivity = 0.1f;
    private float _cameraDistance = AppSettings.DefaultCameraDistance;
    private float _shadeShift = -0.1f;
    private float _shadeToony = 0.9f;
    private float _rimIntensity = 0.5f;
    private int _extractTotalFrames;
    private int _poseTotalFrames;
    private int _adaptTotalFrames;
    // bottomWidth is no longer used; bottom region spans full screen width
    // private double bottomWidth = 0;
    private bool _glInitialized;
    private MiniMikuDance.Import.ModelData? _pendingModel;
    private MiniMikuDance.Import.ModelData? _currentModel;
    private readonly Dictionary<long, SKPoint> _touchPoints = new();
    private bool _boneMode;
    private MotionEditor? _motionEditor;
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

    private class PoseState
    {
        public IList<Vector3> Rotations = new List<Vector3>();
        public IList<Vector3> Translations = new List<Vector3>();
    }

    private readonly List<PoseState> _poseHistory = new();
    private int _poseHistoryIndex = -1;
    private PoseState? _poseBeforeKeyInput;
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
    _renderer.ShadeShift = -0.1f;
    _renderer.ShadeToony = 0.9f;
    _renderer.RimIntensity = 0.5f;
    _renderer.StageSize = _settings.StageSize;
    _cameraDistance = _settings.CameraDistance;
    _renderer.DefaultCameraDistance = _cameraDistance;
    _renderer.Distance = _cameraDistance;
    _renderer.DefaultCameraTargetY = _settings.CameraTargetY;

    var motion = App.Initializer.Motion;
    if (motion != null)
    {
        _motionEditor = new MotionEditor(motion);
    }

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
        setting.CameraDistance = _settings.CameraDistance;
        setting.CameraDistanceChanged += v =>
        {
            _renderer.DefaultCameraDistance = (float)v;
            _renderer.Distance = (float)v;
            _settings.CameraDistance = (float)v;
            _settings.Save();
            _cameraDistance = (float)v;
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

    AddKeyPanel.Confirmed += OnAddKeyConfirmClicked;
    AddKeyPanel.Canceled += OnAddKeyCancelClicked;
    AddKeyPanel.BoneChanged += OnAddKeyBoneChanged;
    AddKeyPanel.FrameChanged += OnAddKeyFrameChanged;
    AddKeyPanel.ParameterChanged += OnKeyParameterChanged;

    // BoneAxisControl の値変更時にモデルへ即座に反映する
    AddKeyPanel.RotXControl.ValueChanged += OnBoneAxisValueChanged;
    AddKeyPanel.RotYControl.ValueChanged += OnBoneAxisValueChanged;
    AddKeyPanel.RotZControl.ValueChanged += OnBoneAxisValueChanged;
    AddKeyPanel.PosXControl.ValueChanged += OnBoneAxisValueChanged;
    AddKeyPanel.PosYControl.ValueChanged += OnBoneAxisValueChanged;
    AddKeyPanel.PosZControl.ValueChanged += OnBoneAxisValueChanged;

    EditKeyPanel.Confirmed += OnEditKeyConfirmClicked;
    EditKeyPanel.Canceled += OnEditKeyCancelClicked;
    EditKeyPanel.BoneChanged += OnEditKeyBoneChanged;
    EditKeyPanel.FrameChanged += OnEditKeyFrameChanged;
    EditKeyPanel.ParameterChanged += OnKeyParameterChanged;
    EditKeyPanel.RotXControl.ValueChanged += OnBoneAxisValueChanged;
    EditKeyPanel.RotYControl.ValueChanged += OnBoneAxisValueChanged;
    EditKeyPanel.RotZControl.ValueChanged += OnBoneAxisValueChanged;
    EditKeyPanel.PosXControl.ValueChanged += OnBoneAxisValueChanged;
    EditKeyPanel.PosYControl.ValueChanged += OnBoneAxisValueChanged;
    EditKeyPanel.PosZControl.ValueChanged += OnBoneAxisValueChanged;
    DeletePanel.Confirmed += OnKeyDeleteConfirmClicked;
    DeletePanel.Canceled += OnKeyDeleteCancelClicked;
    DeletePanel.BoneChanged += OnDeleteBoneChanged;

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
        sv.CameraDistance = _cameraDistance;
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

private void OnPoseEditorClicked(object? sender, EventArgs e)
{
    ShowBottomFeature("POSE");
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
    if (ViewMenu == null || SettingMenu == null || FileMenu == null)
        return;

    HideViewMenu();
    HideSettingMenu();
    HideFileMenu();
    UpdateLayout();
}

private void UpdateSettingViewProperties(SettingView? sv)
{
    if (sv == null || _renderer == null)
        return;

    sv!.HeightRatio = _bottomHeightRatio;
    sv.RotateSensitivity = _rotateSensitivity;
    sv.PanSensitivity = _panSensitivity;
    sv.CameraDistance = _cameraDistance;
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

#if ANDROID
[SupportedOSPlatform("android30.0")]
private static void RequestManageAllFilesAccessPermission()
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
        RequestManageAllFilesAccessPermission();
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
        AdaptSelectMessage == null || AddKeyPanel == null || EditKeyPanel == null ||
        DeletePanel == null || ProgressFrame == null || LoadingIndicator == null ||
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
        PmxImportDialog.IsVisible ? 0.8 : 0));
    AbsoluteLayout.SetLayoutFlags(PmxImportDialog,
        AbsoluteLayoutFlags.XProportional | AbsoluteLayoutFlags.WidthProportional | AbsoluteLayoutFlags.HeightProportional);
    AbsoluteLayout.SetLayoutBounds(PoseSelectMessage, new Rect(0.5, TopMenuHeight + 20, 0.8,
        PoseSelectMessage.IsVisible ? AbsoluteLayout.AutoSize : 0));
    AbsoluteLayout.SetLayoutFlags(PoseSelectMessage,
        AbsoluteLayoutFlags.XProportional | AbsoluteLayoutFlags.WidthProportional);
    AbsoluteLayout.SetLayoutBounds(AdaptSelectMessage, new Rect(0.5, TopMenuHeight + 20, 0.8,
        AdaptSelectMessage.IsVisible ? AbsoluteLayout.AutoSize : 0));
    AbsoluteLayout.SetLayoutFlags(AdaptSelectMessage,
        AbsoluteLayoutFlags.XProportional | AbsoluteLayoutFlags.WidthProportional);
    AbsoluteLayout.SetLayoutBounds(AddKeyPanel, new Rect(W - 300 - 20, TopMenuHeight + 20, 300, 500));
    AbsoluteLayout.SetLayoutFlags(AddKeyPanel, AbsoluteLayoutFlags.None);
    AbsoluteLayout.SetLayoutBounds(EditKeyPanel, new Rect(W - 300 - 20, TopMenuHeight + 20, 300, 500));
    AbsoluteLayout.SetLayoutFlags(EditKeyPanel, AbsoluteLayoutFlags.None);
    AbsoluteLayout.SetLayoutBounds(DeletePanel, new Rect(W - 300 - 20, TopMenuHeight + 20, 300, 200));
    AbsoluteLayout.SetLayoutFlags(DeletePanel, AbsoluteLayoutFlags.None);

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
    if (_renderer == null)
        return;
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
        SavePoseState();
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

        if (_boneMode)
        {
            // TODO: implement bone manipulation via touch
        }
        else if (_touchPoints.Count == 1 && prevPoints.ContainsKey(e.Id))
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
        else if (name == "POSE")
        {
            var pv = new PoseEditorView();
            pv.ModeChanged += mode => _boneMode = mode;
            view = pv;
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
            tv.AddKeyClicked += async (s, e) =>
            {
                if (s is TimelineView timelineView)
                {
                    int boneIndex = timelineView.SelectedKeyInputBoneIndex;
                    var boneName = timelineView.BoneNames.Count > boneIndex && boneIndex >= 0
                        ? timelineView.BoneNames[boneIndex]
                        : timelineView.SelectedBoneName;

                    if (timelineView.HasKeyframe(boneName, timelineView.CurrentFrame))
                    {
                        await DisplayAlert("Info", "既にキーがあります", "OK");
                        AddKeyPanel.IsVisible = false;
                        return;
                    }

                    AddKeyPanel.SetBones(timelineView.BoneNames);
                    AddKeyPanel.SelectedBoneIndex = boneIndex;
                    AddKeyPanel.SetFrame(timelineView.CurrentFrame, timelineView.GetKeyframesForBone(boneName));
                    timelineView.SelectedKeyInputBoneIndex = AddKeyPanel.SelectedBoneIndex;
                    // Ensure rotation limit is applied even when the index does not change
                    OnAddKeyBoneChanged(AddKeyPanel.SelectedBoneIndex);
                }
                _poseBeforeKeyInput = new PoseState
                {
                    Rotations = _renderer.GetAllBoneRotations(),
                    Translations = _renderer.GetAllBoneTranslations()
                };
                AddKeyPanel.IsVisible = true;
            };
            tv.EditKeyClicked += async (s, e) =>
            {
                if (s is not TimelineView timelineView || EditKeyPanel == null)
                {
                    await DisplayAlert("Error", "EditKeyPanel または TimelineView が初期化されていません", "OK");
                    return;
                }

                int boneIndex = timelineView.SelectedKeyInputBoneIndex;
                var boneName = timelineView.BoneNames.Count > boneIndex && boneIndex >= 0
                    ? timelineView.BoneNames[boneIndex]
                    : timelineView.SelectedBoneName;

                if (!timelineView.HasKeyframe(boneName, timelineView.CurrentFrame))
                {
                    await DisplayAlert("Info", "キーがありません", "OK");
                    EditKeyPanel.IsVisible = false;
                    return;
                }

                _poseBeforeKeyInput = new PoseState
                {
                    Rotations = _renderer.GetAllBoneRotations(),
                    Translations = _renderer.GetAllBoneTranslations()
                };

                EditKeyPanel.SetBones(timelineView.BoneNames);
                EditKeyPanel.SelectedBoneIndex = boneIndex;

                var frames = timelineView.GetKeyframesForBone(boneName);
                EditKeyPanel.SetFrame(timelineView.CurrentFrame, frames,
                    timelineView.GetBoneTranslationAtFrame,
                    timelineView.GetBoneRotationAtFrame);

                timelineView.SelectedKeyInputBoneIndex = EditKeyPanel.SelectedBoneIndex;

                var selectedIndex = EditKeyPanel.SelectedBoneIndex;
                if (selectedIndex >= 0)
                {
                    // Ensure rotation limit is applied even when the index does not change
                    OnEditKeyBoneChanged(selectedIndex);

                    var boneName2 = EditKeyPanel.SelectedBone;
                    if (!string.IsNullOrEmpty(boneName2))
                    {
                        OnKeyParameterChanged(
                            boneName2,
                            EditKeyPanel.FrameNumber,
                            EditKeyPanel.Translation,
                            EditKeyPanel.EulerRotation);
                    }
                }

                EditKeyPanel.IsVisible = true;
            };
           tv.DeleteKeyClicked += (s, e) =>
           {
               DeletePanel.IsVisible = true;
               if (s is TimelineView timelineView)
               {
                    int boneIndex = timelineView.SelectedKeyInputBoneIndex;
                    DeletePanel.SetBones(timelineView.BoneNames);
                    DeletePanel.SelectedBoneIndex = boneIndex;
                    var bone = timelineView.BoneNames.Count > boneIndex && boneIndex >= 0
                        ? timelineView.BoneNames[boneIndex]
                        : timelineView.SelectedBoneName;
                    var frames = timelineView.GetKeyframesForBone(bone);
                    DeletePanel.SetFrames(frames);
                    DeletePanel.SelectedFrameIndex = frames.IndexOf(timelineView.CurrentFrame);
                    timelineView.SelectedKeyInputBoneIndex = DeletePanel.SelectedBoneIndex;
               }
           };
            view = tv;
        }
        else if (name == "MTOON")
        {
            var mv = new LightingView
            {
                ShadeShift = _shadeShift,
                ShadeToony = _shadeToony,
                RimIntensity = _rimIntensity
            } ?? throw new InvalidOperationException("LightingView の生成に失敗しました");
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
            sv.CameraDistanceChanged += v =>
            {
                if (_renderer != null)
                {
                    _renderer.DefaultCameraDistance = (float)v;
                    _renderer.Distance = (float)v;
                }
                if (_settings != null)
                {
                    _settings.CameraDistance = (float)v;
                    _settings.Save();
                }
                _cameraDistance = (float)v;
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
    _selectedModelPath = null;
    UpdateLayout();
}

private void OnSelectPmxModelClicked(object? sender, EventArgs e)
{
    ShowModelExplorer();
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
        var importer = new ModelImporter { Scale = 1.0f };
        var data = await Task.Run(() => importer.ImportModel(_selectedModelPath));

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
        SelectedModelPath.Text = string.Empty;
    }
}

private void OnCancelImportClicked(object? sender, EventArgs e)
{
    _selectedModelPath = null;
    PmxImportDialog.IsVisible = false;
    SelectedModelPath.Text = string.Empty;
    SetLoadingIndicatorVisibilityAndLayout(false);
    UpdateLayout();
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
            ["leftUpperArm"]  = System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.UnitX, MathF.PI / 4f),
            ["rightUpperArm"] = System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.UnitX, MathF.PI / 4f),
            ["leftShoulder"]  = System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.UnitX, MathF.PI / 18f),
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
            SavePoseState();
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

private async void OnKeyConfirmClicked(string bone, int frame, Vector3 trans, Vector3 rot)
{
    SetLoadingIndicatorVisibilityAndLayout(true);
    await Task.Delay(50);
    _motionEditor?.AddKeyFrame(bone, frame);

    var rClamped = ClampRotation(bone, rot);

    if (rClamped != rot)
    {
        if (AddKeyPanel.IsVisible)
            AddKeyPanel.SetRotation(rClamped);
        else if (EditKeyPanel.IsVisible)
            EditKeyPanel.SetRotation(rClamped);
    }

    if (_currentFeature == "TIMELINE" && _bottomViews.TryGetValue("TIMELINE", out var timelineView) && timelineView is TimelineView tv)
    {
        tv.AddKeyframe(bone, frame, trans, rClamped);
    }

    if (_currentModel != null)
    {
        if (_currentModel.HumanoidBones.TryGetValue(bone, out int index))
        {
            _renderer.SetBoneTranslation(index, trans);
            _renderer.SetBoneRotation(index, rClamped);

            SavePoseState();
            Viewer?.InvalidateSurface();
        }
    }
    _poseBeforeKeyInput = null;
    AddKeyPanel.IsVisible = false;
    EditKeyPanel.IsVisible = false;
    SetLoadingIndicatorVisibilityAndLayout(false);
}

private void OnKeyCancelClicked()
{
    AddKeyPanel.IsVisible = false;
    EditKeyPanel.IsVisible = false;
    if (_poseBeforeKeyInput != null)
    {
        _renderer.SetAllBoneRotations(_poseBeforeKeyInput.Rotations);
        _renderer.SetAllBoneTranslations(_poseBeforeKeyInput.Translations);
        Viewer?.InvalidateSurface();
        _poseBeforeKeyInput = null;
        SavePoseState();
    }
    SetLoadingIndicatorVisibilityAndLayout(false);
}

private void OnAddKeyConfirmClicked(string bone, int frame, Vector3 trans, Vector3 rot)
    => OnKeyConfirmClicked(bone, frame, trans, rot);

private void OnEditKeyConfirmClicked(string bone, int frame, Vector3 trans, Vector3 rot)
    => OnKeyConfirmClicked(bone, frame, trans, rot);

private void OnAddKeyCancelClicked()
    => OnKeyCancelClicked();

private void OnEditKeyCancelClicked()
    => OnKeyCancelClicked();

private async void OnKeyDeleteConfirmClicked(string bone, int frame)
{
    SetLoadingIndicatorVisibilityAndLayout(true);
    await Task.Delay(50);
    _motionEditor?.RemoveKeyFrame(bone, frame);
        if (_currentFeature == "TIMELINE" && _bottomViews.TryGetValue("TIMELINE", out var timelineView) && timelineView is TimelineView tv)
        {
            tv.RemoveKeyframe(bone, frame);
            // 現在のフレームにおける姿勢を再適用してモデルを更新する
            ApplyTimelineFrame(tv, tv.CurrentFrame);
            Viewer?.InvalidateSurface();
        }
    DeletePanel.IsVisible = false;
    SetLoadingIndicatorVisibilityAndLayout(false);
}

private void OnKeyDeleteCancelClicked()
{
    DeletePanel.IsVisible = false;
    SetLoadingIndicatorVisibilityAndLayout(false);
}

private void OnAddKeyBoneChanged(int index)
{
    int frame = AddKeyPanel.FrameNumber;

    if (_bottomViews.TryGetValue("TIMELINE", out var timelineView) && timelineView is TimelineView tv)
    {
        tv.SelectedKeyInputBoneIndex = index;

        if (index >= 0 && index < tv.BoneNames.Count)
        {
            var boneName = tv.BoneNames[index];
            if (_bonesConfig != null && _bonesConfig.TryGetLimit(boneName, out var lim))
                AddKeyPanel.SetRotationLimit(lim);
            else
                AddKeyPanel.SetRotationLimit(null);

            if (tv.HasAnyKeyframe(boneName))
            {
                AddKeyPanel.SetTranslation(tv.GetNearestTranslation(boneName, frame));
                AddKeyPanel.SetRotation(ClampRotation(boneName, tv.GetNearestRotation(boneName, frame)));
            }
            else if (_motionEditor != null)
            {
                var t = GetBoneTranslationAtFrame(boneName, frame);
                var r = ClampRotation(boneName, GetBoneRotationAtFrame(boneName, frame));
                AddKeyPanel.SetTranslation(t);
                AddKeyPanel.SetRotation(r);
            }
            else
            {
                AddKeyPanel.SetTranslation(Vector3.Zero);
                AddKeyPanel.SetRotation(Vector3.Zero);
            }
        }

        return;
    }

    if (_currentModel == null) return;
    if (index >= 0 && index < _currentModel.HumanoidBoneList.Count)
    {
        var boneName = _currentModel.HumanoidBoneList[index].Name;
        if (_bonesConfig != null && _bonesConfig.TryGetLimit(boneName, out var lim))
            AddKeyPanel.SetRotationLimit(lim);
        else
            AddKeyPanel.SetRotationLimit(null);

        var t = GetBoneTranslationAtFrame(boneName, frame);
        var r = ClampRotation(boneName, GetBoneRotationAtFrame(boneName, frame));
        AddKeyPanel.SetTranslation(t);
        AddKeyPanel.SetRotation(r);
    }
}

private void OnEditKeyBoneChanged(int index)
{
    int frame = EditKeyPanel.FrameNumber;

    if (_bottomViews.TryGetValue("TIMELINE", out var timelineView) && timelineView is TimelineView tv)
    {
        tv.SelectedKeyInputBoneIndex = index;

        if (index >= 0 && index < tv.BoneNames.Count)
        {
            var boneName = tv.BoneNames[index];
            if (_bonesConfig != null && _bonesConfig.TryGetLimit(boneName, out var lim))
                EditKeyPanel.SetRotationLimit(lim);
            else
                EditKeyPanel.SetRotationLimit(null);
            if (tv.HasKeyframe(boneName, frame))
            {
                EditKeyPanel.SetTranslation(tv.GetBoneTranslationAtFrame(boneName, frame));
                EditKeyPanel.SetRotation(ClampRotation(boneName, tv.GetBoneRotationAtFrame(boneName, frame)));
            }
            else if (tv.HasAnyKeyframe(boneName))
            {
                EditKeyPanel.SetTranslation(tv.GetNearestTranslation(boneName, frame));
                EditKeyPanel.SetRotation(ClampRotation(boneName, tv.GetNearestRotation(boneName, frame)));
            }
            else if (_motionEditor != null)
            {
                var t = GetBoneTranslationAtFrame(boneName, frame);
                var r = ClampRotation(boneName, GetBoneRotationAtFrame(boneName, frame));
                EditKeyPanel.SetTranslation(t);
                EditKeyPanel.SetRotation(r);
            }
            else
            {
                EditKeyPanel.SetTranslation(Vector3.Zero);
                EditKeyPanel.SetRotation(Vector3.Zero);
            }

            EditKeyPanel.SetFrameOptions(tv.GetKeyframesForBone(boneName));
            OnKeyParameterChanged(
                EditKeyPanel.SelectedBone,
                EditKeyPanel.FrameNumber,
                EditKeyPanel.Translation,
                EditKeyPanel.EulerRotation);
        }

        return;
    }

    if (_currentModel == null) return;
    if (index >= 0 && index < _currentModel.HumanoidBoneList.Count)
    {
        var boneName = _currentModel.HumanoidBoneList[index].Name;
        if (_bonesConfig != null && _bonesConfig.TryGetLimit(boneName, out var lim))
            EditKeyPanel.SetRotationLimit(lim);
        else
            EditKeyPanel.SetRotationLimit(null);

        var t = GetBoneTranslationAtFrame(boneName, frame);
        var r = ClampRotation(boneName, GetBoneRotationAtFrame(boneName, frame));
        EditKeyPanel.SetTranslation(t);
        EditKeyPanel.SetRotation(r);
        OnKeyParameterChanged(
            EditKeyPanel.SelectedBone,
            EditKeyPanel.FrameNumber,
            EditKeyPanel.Translation,
            EditKeyPanel.EulerRotation);
    }
}

private void OnDeleteBoneChanged(int index)
{
    if (_currentModel == null) return;
    if (_bottomViews.TryGetValue("TIMELINE", out var timelineView) && timelineView is TimelineView tv)
    {
        tv.SelectedKeyInputBoneIndex = index;
        if (index >= 0 && index < tv.BoneNames.Count)
        {
            var bone = tv.BoneNames[index];
            DeletePanel.SetFrames(tv.GetKeyframesForBone(bone));
        }
        else
        {
            DeletePanel.SetFrames(Array.Empty<int>());
        }
    }
}

private void OnAddKeyFrameChanged(int frame)
{
    if (_bottomViews.TryGetValue("TIMELINE", out var timelineView) && timelineView is TimelineView tv)
    {
        int boneIndex = AddKeyPanel.SelectedBoneIndex;
        if (boneIndex >= 0 && boneIndex < tv.BoneNames.Count)
        {
            var bone = tv.BoneNames[boneIndex];
            if (tv.HasAnyKeyframe(bone))
            {
                var t = tv.GetBoneTranslationAtFrame(bone, frame);
                var r = ClampRotation(bone, tv.GetBoneRotationAtFrame(bone, frame));
                AddKeyPanel.SetTranslation(t);
                AddKeyPanel.SetRotation(r);
            }
            else if (_motionEditor != null)
            {
                var t = GetBoneTranslationAtFrame(bone, frame);
                var r = ClampRotation(bone, GetBoneRotationAtFrame(bone, frame));
                AddKeyPanel.SetTranslation(t);
                AddKeyPanel.SetRotation(r);
            }
            else
            {
                AddKeyPanel.SetTranslation(Vector3.Zero);
                AddKeyPanel.SetRotation(Vector3.Zero);
            }
        }
        return;
    }
}

private void OnEditKeyFrameChanged(int frame)
{
    if (_bottomViews.TryGetValue("TIMELINE", out var timelineView) && timelineView is TimelineView tv)
    {
        int boneIndex = EditKeyPanel.SelectedBoneIndex;
        if (boneIndex >= 0 && boneIndex < tv.BoneNames.Count)
        {
            var bone = tv.BoneNames[boneIndex];
            if (tv.HasAnyKeyframe(bone))
            {
                var t = tv.GetBoneTranslationAtFrame(bone, frame);
                var r = ClampRotation(bone, tv.GetBoneRotationAtFrame(bone, frame));
                EditKeyPanel.SetTranslation(t);
                EditKeyPanel.SetRotation(r);
                OnKeyParameterChanged(
                    EditKeyPanel.SelectedBone,
                    EditKeyPanel.FrameNumber,
                    EditKeyPanel.Translation,
                    EditKeyPanel.EulerRotation);
            }
            else if (_motionEditor != null)
            {
                var t = GetBoneTranslationAtFrame(bone, frame);
                var r = ClampRotation(bone, GetBoneRotationAtFrame(bone, frame));
                EditKeyPanel.SetTranslation(t);
                EditKeyPanel.SetRotation(r);
                OnKeyParameterChanged(
                    EditKeyPanel.SelectedBone,
                    EditKeyPanel.FrameNumber,
                    EditKeyPanel.Translation,
                    EditKeyPanel.EulerRotation);
            }
            else
            {
                EditKeyPanel.SetTranslation(Vector3.Zero);
                EditKeyPanel.SetRotation(Vector3.Zero);
                OnKeyParameterChanged(
                    EditKeyPanel.SelectedBone,
                    EditKeyPanel.FrameNumber,
                    EditKeyPanel.Translation,
                    EditKeyPanel.EulerRotation);
            }
        }
        return;
    }
}

private void OnKeyParameterChanged(string bone, int frame, Vector3 trans, Vector3 rot)
{
    if (_currentModel == null)
        return;

    if (_currentModel.HumanoidBones.TryGetValue(bone, out int index))
    {
        _renderer.SetBoneTranslation(index, trans);
        _renderer.SetBoneRotation(index, ClampRotation(bone, rot));
        Viewer?.InvalidateSurface();
    }
}

/// <summary>
/// BoneAxisControl の値が変更された際に呼び出されるハンドラ。
/// 現在選択中のボーンに対してモデルを更新する。
/// </summary>
/// <param name="v">未使用</param>
private void OnBoneAxisValueChanged(double v)
{
    if (_currentModel == null)
        return;

    string boneName;
    Vector3 translation;
    Vector3 rotation;

    if (AddKeyPanel.IsVisible)
    {
        boneName = AddKeyPanel.SelectedBone;
        translation = AddKeyPanel.Translation;
        rotation = AddKeyPanel.EulerRotation;
    }
    else if (EditKeyPanel.IsVisible)
    {
        boneName = EditKeyPanel.SelectedBone;
        translation = EditKeyPanel.Translation;
        rotation = EditKeyPanel.EulerRotation;
    }
    else
    {
        return;
    }

    if (string.IsNullOrEmpty(boneName) ||
        !_currentModel.HumanoidBones.TryGetValue(boneName, out int index))
        return;

    var rClamped = ClampRotation(boneName, rotation);

    if (rClamped != rotation)
    {
        if (AddKeyPanel.IsVisible)
            AddKeyPanel.SetRotation(rClamped);
        else if (EditKeyPanel.IsVisible)
            EditKeyPanel.SetRotation(rClamped);
        rotation = rClamped;
    }

    _renderer.SetBoneTranslation(index, translation);
    _renderer.SetBoneRotation(index, rClamped);
    SavePoseState();
    Viewer?.InvalidateSurface();
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

private Vector3 ClampRotation(string bone, Vector3 rot)
{
    if (_bonesConfig == null)
        return rot;

    var clamped = _bonesConfig.Clamp(bone, rot.ToNumerics());
    return clamped.ToOpenTK();
}

private Vector3 GetBoneTranslationAtFrame(string bone, int frame)
{
    if (_bottomViews.TryGetValue("TIMELINE", out var view) && view is TimelineView tv)
    {
        if (tv.HasKeyframe(bone, frame))
            return tv.GetBoneTranslationAtFrame(bone, frame);
    }

    if (_motionEditor == null || App.Initializer.Applier == null || _currentModel == null)
        return Vector3.Zero;

    var frames = _motionEditor.Motion.Frames;
    if (frame < 0 || frame >= frames.Length)
        return Vector3.Zero;

    var (_, transform) = App.Initializer.Applier.Apply(frames[frame]);

    if (!_currentModel.HumanoidBones.TryGetValue("hips", out int hipsIndex))
        return Vector3.Zero;
    if (!_currentModel.HumanoidBones.TryGetValue(bone, out int index) || index != hipsIndex)
        return Vector3.Zero;

    return new Vector3(transform.M41, transform.M42, transform.M43);
}

private Vector3 GetBoneRotationAtFrame(string bone, int frame)
{
    if (_motionEditor == null || App.Initializer.Applier == null || _currentModel == null)
        return Vector3.Zero;
    var frames = _motionEditor.Motion.Frames;
    if (frame < 0 || frame >= frames.Length)
        return Vector3.Zero;

    // JointData からモデル座標系での回転を取得する
    var (rotations, _) = App.Initializer.Applier.Apply(frames[frame]);
    if (!_currentModel.HumanoidBones.TryGetValue(bone, out int index))
        return Vector3.Zero;
    if (!rotations.TryGetValue(index, out var q))
        return Vector3.Zero;
    var euler = ToEulerAngles(q);
    return new Vector3(euler.X, euler.Y, euler.Z);
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
