using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using Microsoft.Maui.Dispatching;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using SkiaSharp;
using OpenTK.Graphics.ES30;
using GL = OpenTK.Graphics.ES30.GL;
using System.IO;
using System.Linq;
using MiniMikuDance.Import;
using OpenTK.Mathematics;
using MiniMikuDance.App;
using MiniMikuDance.IK;
using MiniMikuDance.Util;
using MiniMikuDanceMaui.Rendering;
using MiniMikuDanceMaui.Services;
using MiniMikuDanceMaui.Views.Panels;

namespace MiniMikuDanceMaui.Views.Pages;

public partial class MainPage : ContentPage
{
    private double _bottomHeightRatio = 1.0 / 3.0;
    private const double TopMenuHeight = 36;
    private bool _viewMenuOpen;
    private bool _settingMenuOpen;
    private bool _fileMenuOpen;

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
    private bool _poseMode;
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

    protected override async void OnAppearing()
    {
        base.OnAppearing();
#if ANDROID
        var readStatus = await Permissions.RequestAsync<Permissions.StorageRead>();
        var writeStatus = await Permissions.RequestAsync<Permissions.StorageWrite>();
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

        AbsoluteLayout.SetLayoutBounds(BottomRegion,
            new Rect(0,
                H - bottomHeight - safe.Bottom,
                W,
                bottomHeight));
        AbsoluteLayout.SetLayoutFlags(BottomRegion, AbsoluteLayoutFlags.None);
    }
}

