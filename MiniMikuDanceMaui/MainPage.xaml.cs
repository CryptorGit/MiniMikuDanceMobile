using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using Microsoft.Maui.Dispatching;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
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
using MiniMikuDance.App;
using SixLabors.ImageSharp.PixelFormats;
using MiniMikuDance.IK;
using MiniMikuDance.Util;

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
    private static readonly HashSet<string> ModelExtensions = new() { ".pmx", ".pmd" };
    private string? _modelDir;
    private float _modelScale = 1f;
    private readonly AppSettings _settings = AppSettings.Load();

    private readonly PmxRenderer _renderer = new();
    private float _rotateSensitivity = 0.1f;
    private float _panSensitivity = 1f;
    private float _shadeShift = -0.1f;
    private float _shadeToony = 0.9f;
    private float _rimIntensity = 0.5f;
    private float _sphereStrength = 1f;
    private float _toonStrength = 1f;
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
    private int _renderTimerErrorCount;
    private void OnPoseModeToggled(object? sender, ToggledEventArgs e)
    {
        _poseMode = e.Value;
        _touchPoints.Clear();
        if (_poseMode && _currentModel != null)
        {
            EnablePoseMode();
        }
        else
        {
            DisablePoseMode();
        }
        _renderer.ShowIkBones = _poseMode;
        Viewer?.InvalidateSurface();
    }

    private void EnablePoseMode()
    {
        _renderer.SetExternalRotation(Quaternion.Identity);
        _renderer.ModelTransform = Matrix4.Identity;
        IkManager.LoadPmxIkBones(_currentModel!.Bones);
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
        IkManager.InvalidateViewer = () =>
        {
            _needsRender = true;
            Viewer?.InvalidateSurface();
        };
    }

    private void DisablePoseMode()
    {
        IkManager.ReleaseSelection();
        _renderer.ClearIkBones();
        IkManager.Clear();
        IkManager.PickFunc = null;
        IkManager.GetBonePositionFunc = null;
        IkManager.GetCameraPositionFunc = null;
        IkManager.SetBoneTranslation = null;
        IkManager.ToModelSpaceFunc = null;
        IkManager.ToWorldSpaceFunc = null;
        IkManager.InvalidateViewer = null;
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

    private static bool HasAllowedExtension(string path, HashSet<string> allowed)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return allowed.Contains(ext);
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
        _sphereStrength = _settings.SphereStrength;
        _toonStrength = _settings.ToonStrength;
        _renderer.SphereStrength = _sphereStrength;
        _renderer.ToonStrength = _toonStrength;
        _renderer.StageSize = _settings.StageSize;
        _renderer.DefaultCameraDistance = _settings.CameraDistance;
        _renderer.DefaultCameraTargetY = _settings.CameraTargetY;
        _renderer.BonePickPixels = _settings.BonePickPixels;
        _renderer.ShowIkBones = _poseMode;
        _renderer.IkBoneScale = _settings.IkBoneScale;

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
            try
            {
                if (_needsRender)
                {
                    Viewer?.InvalidateSurface();
                }
                _renderTimerErrorCount = 0;
            }
            catch (Exception ex)
            {
                _renderTimerErrorCount++;
                Console.Error.WriteLine(ex);
                if (_renderTimerErrorCount >= 3)
                {
                    _renderTimer.Stop();
                    MainThread.BeginInvokeOnMainThread(async () =>
                        await DisplayAlert("Error", "Rendering stopped due to repeated errors.", "OK"));
                }
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
            setting.IkBoneSize = _settings.IkBoneScale;
            setting.IkBoneSizeChanged += v =>
            {
                _renderer.IkBoneScale = (float)v;
                _settings.IkBoneScale = (float)v;
                _settings.Save();
            };
            setting.BonePickPixels = _settings.BonePickPixels;
            setting.BonePickPixelsChanged += v =>
            {
                _renderer.BonePickPixels = (float)v;
                _settings.BonePickPixels = (float)v;
                _settings.Save();
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

    }

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

    private void HideAllMenusAndLayout()
    {
        SetMenuVisibility(ref _viewMenuOpen, ViewMenu, false);
        SetMenuVisibility(ref _settingMenuOpen, SettingMenu, false);
        SetMenuVisibility(ref _fileMenuOpen, FileMenu, false);
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
                        var intent = new Android.Content.Intent(Android.Provider.Settings.ActionManageAppAllFilesAccessPermission, uri);
                        intent.AddFlags(Android.Content.ActivityFlags.NewTask);
                        context.StartActivity(intent);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    await DisplayAlert("Error", ex.Message, "OK");
                }
            }
        }

        if (MmdFileSystem.FallbackToInternalStorage)
        {
            await DisplayAlert("ストレージ", "外部ストレージへのアクセスに失敗したため、内部ストレージを使用します。", "OK");
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
            MenuOverlay == null || PmxImportDialog == null ||
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
        _renderer.SphereStrength = _sphereStrength;
        _renderer.ToonStrength = _toonStrength;
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
                if (IkManager.InvalidateViewer != null)
                    IkManager.InvalidateViewer();
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


                PmxImporter.CacheCapacity = _settings.TextureCacheSize;
                using IModelImporter importer = new PmxImporter();
                ModelData data;
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
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                        dir = null;
                    }
                    data = importer.ImportModel(stream, dir);
                }
                _renderer.LoadModel(data);
                _currentModel = data;
                UpdateRendererLightingProperties();
                Viewer?.InvalidateSurface();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
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
            else if (name == "MTOON")
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
                    UpdateRendererLightingProperties();
                    _settings.SphereStrength = _sphereStrength;
                    _settings.Save();
                };
                mv.ToonStrengthChanged += v =>
                {
                    _toonStrength = (float)v;
                    UpdateRendererLightingProperties();
                    _settings.ToonStrength = _toonStrength;
                    _settings.Save();
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
        else if (name == "MTOON" && _bottomViews[name] is LightingView mv)
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

        SetMenuVisibility(ref _fileMenuOpen, FileMenu, false);
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
            Debug.WriteLine(ex);
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
        // Show bottom explorer and dialog overlay together
        ShowExplorer("Open", PmxImportDialog, SelectedModelPath, ref _selectedModelPath);
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
        if (!HasAllowedExtension(path, ModelExtensions))
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

        bool success = false;

        try
        {
            _modelScale = 1f;
            PmxImporter.CacheCapacity = _settings.TextureCacheSize;
            using IModelImporter importer = new PmxImporter { Scale = _modelScale };
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
                using var semaphore = new SemaphoreSlim(Environment.ProcessorCount);

                async Task ProcessTextureAsync(string rel, List<int> indices)
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var localRel = rel;
                        var path = Path.Combine(_modelDir, localRel.Replace('/', Path.DirectorySeparatorChar));
                        if (!File.Exists(path))
                        {
                            var fileName = Path.GetFileName(localRel);
                            var found = Directory.GetFiles(_modelDir, fileName, SearchOption.AllDirectories).FirstOrDefault();
                            if (found == null)
                                return;
                            localRel = Path.GetRelativePath(_modelDir, found).Replace(Path.DirectorySeparatorChar, '/');
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
                            sm.TextureFilePath = localRel;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }

                var tasks = textureMap.Select(kvp => ProcessTextureAsync(kvp.Key, kvp.Value));
                await Task.WhenAll(tasks);
            }

            _pendingModel = data;
            Viewer.InvalidateSurface();
            if (_bottomViews.TryGetValue("MORPH", out var view) && view is MorphView mv)
            {
                mv.SetMorphs(data.Morphs);
            }
            success = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            SelectedModelPath.Text = "モデルの読み込みに失敗しました";
            await DisplayAlert("Error", "モデルの読み込みに失敗しました", "OK");
        }
        finally
        {
            Viewer.HasRenderLoop = true;
            SetLoadingIndicatorVisibilityAndLayout(false);
            _selectedModelPath = null;
            _modelDir = null;
            if (success)
            {
                SelectedModelPath.Text = string.Empty;
            }
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
