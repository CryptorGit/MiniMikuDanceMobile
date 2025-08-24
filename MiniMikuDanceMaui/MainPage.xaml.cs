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
using MiniMikuDance.Physics;
using MauiIcons.Material;

namespace MiniMikuDanceMaui;

public partial class MainPage : ContentPage
{

    private const double TopMenuHeight = 36;
    private readonly AppSettings _settings = AppSettings.Load();

    private readonly PmxRenderer _renderer = new();
    private float _rotateSensitivity = 0.1f;
    private float _panSensitivity = 1f;
    private float _shadeShift = -0.1f;
    private float _shadeToony = 0.9f;
    private float _rimIntensity = 0.5f;
    private float _sphereStrength = 1f;
    private float _toonStrength = 0f;
    private bool _poseMode;
    private bool _physicsEnabled;
    // bottomWidth is no longer used; bottom region spans full screen width
    // private double bottomWidth = 0;
    private bool _glInitialized;
    private readonly Scene _scene = new();
    private IPhysicsWorld _physics = new NullPhysicsWorld();
    private readonly object _physicsLock = new();
    private bool _pendingPhysicsReload;
    private PhysicsState? _nextPhysics;
    private readonly CancellationTokenSource _physicsCts = new();
    private DateTime _lastPhysicsTime = DateTime.UtcNow;
    private readonly Dictionary<long, SKPoint> _touchPoints = new();
    private readonly long[] _touchIds = new long[2];
    private bool _needsRender;
    private readonly IDispatcherTimer _renderTimer;
    private int _renderTimerErrorCount;
    private void OnPoseModeButtonClicked(object? sender, TappedEventArgs e)
    {
        _poseMode = !_poseMode;
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
        PoseModeIcon.SetIcon(_poseMode ? MaterialIcons.AccessibilityNew : MaterialIcons.PhotoCamera);
        PoseModeIcon.SetIconColor(_poseMode ? Colors.Green : Colors.Gray);
        Viewer?.InvalidateSurface();
    }

    private void OnPhysicsButtonClicked(object? sender, TappedEventArgs e)
    {
        var desired = !_physicsEnabled;
        var state = BuildPhysicsState(desired);
        lock (_physicsLock)
        {
            if (_nextPhysics.HasValue)
            {
                (_nextPhysics.Value.World as IDisposable)?.Dispose();
            }
            _nextPhysics = state;
            _pendingPhysicsReload = true;
            _physicsEnabled = state.Enabled;
        }
        PhysicsIcon.SetIconColor(_physicsEnabled ? Colors.Green : Colors.Gray);
        _settings.EnablePhysics = _physicsEnabled;
        _settings.Save();
        _needsRender = true;
        Viewer?.InvalidateSurface();
    }

    private PhysicsState BuildPhysicsState(bool enabled)
    {
        IPhysicsWorld physics;
        if (enabled)
        {
            physics = new BepuPhysicsWorld();
            try
            {
                physics.Initialize(_settings.Physics, _settings.ModelScale);
                if (_currentModel != null && physics is BepuPhysicsWorld bepu)
                {
                    bepu.LoadRigidBodies(_currentModel);
                    bepu.LoadSoftBodies(_currentModel);
                    bepu.LoadJoints(_currentModel);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                physics = new NullPhysicsWorld();
                enabled = false;
            }
        }
        else
        {
            physics = new NullPhysicsWorld();
        }

        return new PhysicsState(physics, enabled);
    }

    private record struct PhysicsState(IPhysicsWorld World, bool Enabled);

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
            Debug.WriteLine(ex.ToString());
        }
        IkManager.PickFunc = _renderer.PickBone;
        IkManager.GetBonePositionFunc = _renderer.GetBoneWorldPosition;
        IkManager.GetCameraPositionFunc = _renderer.GetCameraPosition;
        IkManager.SetBoneWorldPosition = _renderer.SetBoneWorldPosition;
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
        IkManager.SetBoneWorldPosition = null;
        IkManager.ToModelSpaceFunc = null;
        IkManager.ToWorldSpaceFunc = null;
        IkManager.InvalidateViewer = null;
    }




    public MainPage()
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
        this.SizeChanged += OnSizeChanged;
        _sphereStrength = _settings.SphereStrength;
        _toonStrength = _settings.ToonStrength;
        _renderer.RotateSensitivity = 0.1f;
        _renderer.PanSensitivity = 1f;
        _renderer.ShadeShift = -0.1f;
        _renderer.ShadeToony = 0.9f;
        _renderer.RimIntensity = 0.5f;
        _renderer.SphereStrength = _sphereStrength;
        _renderer.ToonStrength = _toonStrength;
        _renderer.StageSize = _settings.StageSize;
        _renderer.DefaultCameraDistance = _settings.CameraDistance;
        _renderer.DefaultCameraTargetY = _settings.CameraTargetY;
        _renderer.BonePickPixels = _settings.BonePickPixels;
        _renderer.ShowIkBones = _poseMode;
        _renderer.IkBoneScale = _settings.IkBoneScale;
        _physicsEnabled = _settings.EnablePhysics;
        var initState = BuildPhysicsState(_physicsEnabled);
        _physics = initState.World;
        _physicsEnabled = initState.Enabled;
        PhysicsIcon.SetIconColor(_physicsEnabled ? Colors.Green : Colors.Gray);

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
        StartPhysicsLoop();

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
            setting.DistinguishBoneTypes = _settings.DistinguishBoneTypes;
            _renderer.DistinguishBoneTypes = _settings.DistinguishBoneTypes;
            setting.BoneTypeChanged += flag =>
            {
                _renderer.DistinguishBoneTypes = flag;
                _settings.DistinguishBoneTypes = flag;
                _settings.Save();
                Viewer?.InvalidateSurface();
            };
            setting.LockTranslation = _settings.Physics.LockTranslation;
            setting.LockTranslationChanged += flag =>
            {
                if (_physics is BepuPhysicsWorld bepu)
                    bepu.LockTranslation = flag;
                var phys = _settings.Physics;
                phys.LockTranslation = flag;
                _settings.Physics = phys;
                _settings.Save();
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
                    Debug.WriteLine(ex.ToString());
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
        _physicsCts.Cancel();
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
            _glInitialized = true;
        }

        if (_modelLoadCompleted && _loadedModel != null)
        {
            var model = _loadedModel;
            _loadedModel = null;
            _modelLoadCompleted = false;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    IkManager.Clear();
                    _renderer.ClearIkBones();
                    _renderer.ClearBoneRotations();
                    _renderer.LoadModel(model);
                    _currentModel = model;
                    WritePhysicsLog(_currentModel);
                    UpdateRendererLightingProperties();
                    _scene.Bones.Clear();
                    _scene.Bones.AddRange(_currentModel.Bones);
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
                            Debug.WriteLine(ex.ToString());
                        }
                        IkManager.PickFunc = _renderer.PickBone;
                        IkManager.GetBonePositionFunc = _renderer.GetBoneWorldPosition;
                        IkManager.GetCameraPositionFunc = _renderer.GetCameraPosition;
                        IkManager.SetBoneWorldPosition = _renderer.SetBoneWorldPosition;
                        IkManager.ToModelSpaceFunc = _renderer.WorldToModel;
                        IkManager.ToWorldSpaceFunc = _renderer.ModelToWorld;
                    }
                    UpdatePhysicsViewRigidBodies();
                    _needsRender = true;
                    Viewer?.InvalidateSurface();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    AppendCrashLog("LoadPendingModel failed", ex);
                    _renderTimer.Start();
                    if (Viewer is SKGLView gl)
                    {
                        gl.Touch -= OnViewTouch;
                        gl.Touch += OnViewTouch;
                    }
                }
            });
        }
        else if (_modelLoadCompleted)
        {
            _modelLoadCompleted = false;
        }

        lock (_physicsLock)
        {
            _renderer.Resize(e.BackendRenderTarget.Width, e.BackendRenderTarget.Height);
            _renderer.Render();
        }
        GL.Flush();
        _needsRender = false;

        if (_pendingPhysicsReload && _nextPhysics.HasValue)
        {
            lock (_physicsLock)
            {
                (_physics as IDisposable)?.Dispose();
                _physics = _nextPhysics.Value.World;
            }
            _nextPhysics = null;
            _pendingPhysicsReload = false;
        }
    }

    private void StartPhysicsLoop()
    {
        Task.Run(async () =>
        {
            while (!_physicsCts.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                float dt = (float)(now - _lastPhysicsTime).TotalSeconds;
                _lastPhysicsTime = now;
                lock (_physicsLock)
                {
                    _physics.SyncFromBones(_scene);
                    _physics.Step(dt);
                    _physics.SyncToBones(_scene);
                }
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _needsRender = true;
                    Viewer?.InvalidateSurface();
                });
                try
                {
                    await Task.Delay(16, _physicsCts.Token).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        });
    }


    private void OnViewTouch(object? sender, SKTouchEventArgs e)
    {
        if (_poseMode)
        {
            try
            {
                HandlePoseTouch(e);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
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

        switch (e.ActionType)
        {
            case SKTouchAction.Pressed:
                HandleViewPressed(e);
                break;
            case SKTouchAction.Moved:
                HandleViewMoved(e);
                break;
            case SKTouchAction.Released:
            case SKTouchAction.Cancelled:
                HandleViewReleased(e);
                break;
        }
        e.Handled = true;
        _needsRender = true;
    }

    private void HandlePoseTouch(SKTouchEventArgs e)
    {
        switch (e.ActionType)
        {
            case SKTouchAction.Pressed:
                HandlePosePressed(e);
                break;
            case SKTouchAction.Moved:
                HandlePoseMoved(e);
                break;
            case SKTouchAction.Released:
            case SKTouchAction.Cancelled:
                HandlePoseReleased(e);
                break;
        }
        if (IkManager.InvalidateViewer != null)
            IkManager.InvalidateViewer();
    }

    private void HandlePosePressed(SKTouchEventArgs e)
    {
        IkManager.PickBone(e.Location.X, e.Location.Y);
    }

    private void HandlePoseMoved(SKTouchEventArgs e)
    {
        var ray = _renderer.ScreenPointToRay(e.Location.X, e.Location.Y);
        var pos = IkManager.IntersectDragPlane(ray);
        if (pos.HasValue && IkManager.SelectedBoneIndex >= 0)
        {
            IkManager.UpdateTarget(IkManager.SelectedBoneIndex, pos.Value);
        }
    }

    private void HandlePoseReleased(SKTouchEventArgs e)
    {
        IkManager.ReleaseSelection();
    }

    private void HandleViewPressed(SKTouchEventArgs e)
    {
        _touchPoints[e.Id] = e.Location;
    }

    private void HandleViewMoved(SKTouchEventArgs e)
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

    private void HandleViewReleased(SKTouchEventArgs e)
    {
        _touchPoints.Remove(e.Id);
    }

    private void UpdatePhysicsViewRigidBodies()
    {
        if (_currentModel != null && _bottomViews.TryGetValue("PHYSICS", out var view) && view is PhysicsView pv)
        {
            pv.SetRigidBodies(_currentModel.RigidBodies, _currentModel.Bones);
        }
    }
}

