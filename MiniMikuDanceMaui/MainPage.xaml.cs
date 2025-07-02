using System;
using System.IO;
using SystemPath = System.IO.Path;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Layouts;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using MiniMikuDance.App;
using MiniMikuDance.UI;
using MiniMikuDance.Util;

namespace MiniMikuDanceMaui;

public partial class MainPage : ContentPage
{
    private readonly AppInitializer _initializer = new();
    private GyroService? _gyroService;
    private bool _uiLoaded;
    private string? _configPath;

    public MainPage()
    {
        InitializeComponent();
        this.On<iOS>().SetUseSafeArea(true);
        this.SizeChanged += OnSizeChanged;
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        UpdateLayout();
    }

    private void UpdateLayout()
    {
        double W = this.Width;
        double H = this.Height;

        Thickness safe = this.SafeAreaInsets();

        double statusHeight = 24;
        AbsoluteLayout.SetLayoutBounds(StatusRibbon, new Rect(safe.Left, safe.Top, W - safe.Left - safe.Right, statusHeight));
        AbsoluteLayout.SetLayoutFlags(StatusRibbon, AbsoluteLayoutFlags.None);

        AbsoluteLayout.SetLayoutBounds(ProgressBar, new Rect(safe.Left, safe.Top + statusHeight, W - safe.Left - safe.Right, 4));
        AbsoluteLayout.SetLayoutFlags(ProgressBar, AbsoluteLayoutFlags.None);

        AbsoluteLayout.SetLayoutBounds(MessageLabel, new Rect(safe.Left + 8, safe.Top + statusHeight + 4, W - safe.Left - safe.Right - 16, 20));
        AbsoluteLayout.SetLayoutFlags(MessageLabel, AbsoluteLayoutFlags.None);

        AbsoluteLayout.SetLayoutBounds(RecordingLabel, new Rect(safe.Left + 8, H - 72 - safe.Bottom - 20, 80, 20));
        AbsoluteLayout.SetLayoutFlags(RecordingLabel, AbsoluteLayoutFlags.None);

        AbsoluteLayout.SetLayoutBounds(ThumbnailImage, new Rect(safe.Left + 88, H - 72 - safe.Bottom - 44, 40, 40));
        AbsoluteLayout.SetLayoutFlags(ThumbnailImage, AbsoluteLayoutFlags.None);

        double dockWidth = ContextDock.WidthRequest;

        double centerTop = safe.Top + statusHeight + 4 + 20; // plus progress/message
        double centerHeight = H - centerTop - 72 - safe.Bottom;
        double centerWidth = W - dockWidth - safe.Left - safe.Right;
        AbsoluteLayout.SetLayoutBounds(CenterStage, new Rect(safe.Left, centerTop, centerWidth, centerHeight));
        AbsoluteLayout.SetLayoutFlags(CenterStage, AbsoluteLayoutFlags.None);

        AbsoluteLayout.SetLayoutBounds(CommandRail, new Rect(safe.Left, H - 72 - safe.Bottom, W - safe.Left - safe.Right, 72));
        AbsoluteLayout.SetLayoutFlags(CommandRail, AbsoluteLayoutFlags.None);

        AbsoluteLayout.SetLayoutBounds(ContextDock, new Rect(W - dockWidth - safe.Right, centerTop, dockWidth, centerHeight));
        AbsoluteLayout.SetLayoutFlags(ContextDock, AbsoluteLayoutFlags.None);

        AbsoluteLayout.SetLayoutBounds(QuickStack, new Rect(safe.Left + 16, H / 2 - 64, 72, 128));
        AbsoluteLayout.SetLayoutFlags(QuickStack, AbsoluteLayoutFlags.None);

        if (W <= 799)
        {
            AbsoluteLayout.SetLayoutBounds(ContextDock, new Rect(safe.Left, H * 0.2, W - safe.Left - safe.Right, H * 0.8 - safe.Bottom));
        }
        else if (W >= 1280)
        {
            ContextDock.WidthRequest = 320;
            AbsoluteLayout.SetLayoutBounds(CommandRail, new Rect(safe.Left, H - 88 - safe.Bottom, W - safe.Left - safe.Right, 88));
        }
    }

    private Thickness SafeAreaInsets()
    {
        return this.Padding;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await InitializeApp();
        UpdateLayout();
    }

    private async Task InitializeApp()
    {
        if (!_uiLoaded)
        {
            _configPath = await LoadUIConfig();
            GenerateDynamicUI();
            UIManager.Instance.OnToggleChanged += OnToggleChanged;
            _uiLoaded = true;
        }

        if (_configPath == null)
            return;

        var posePath = SystemPath.Combine(FileSystem.CacheDirectory, "pose_model.onnx");
        _initializer.Initialize(_configPath, null, posePath, MmdFileSystem.BaseDir);
        if (_initializer.Camera != null)
        {
            _gyroService = new GyroService(_initializer.Camera);
            if (UIManager.Instance.GetToggle("gyro_cam"))
                _gyroService.Start();
        }
    }

    private async Task<string?> LoadUIConfig()
    {
        var temp = SystemPath.Combine(FileSystem.CacheDirectory, "UIConfig.json");
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("UIConfig.json");
            using var fs = File.Create(temp);
            await stream.CopyToAsync(fs);
            UIManager.Instance.LoadConfig(temp);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"UIConfig.json not found: {ex.Message}\nUsing default UI.", "OK");
            var cfg = CreateDefaultConfig();
            JSONUtil.Save(temp, cfg);
            UIManager.Instance.LoadConfig(temp);
        }

        if (UIManager.Instance.Config.Buttons.Count == 0 && UIManager.Instance.Config.Toggles.Count == 0)
        {
            await DisplayAlert("Error", "UI configuration is empty. Using default UI.", "OK");
            var cfg = CreateDefaultConfig();
            JSONUtil.Save(temp, cfg);
            UIManager.Instance.LoadConfig(temp);
        }

        return temp;
    }

    private static UIConfig CreateDefaultConfig()
    {
        return new UIConfig
        {
            Buttons = new()
            {
                new UIButton { Label = "Load Model", Message = "load_model" },
                new UIButton { Label = "Analyze Video", Message = "analyze_video" },
                new UIButton { Label = "Generate Motion", Message = "generate_motion" },
                new UIButton { Label = "Play Motion", Message = "play_motion" },
                new UIButton { Label = "Record", Message = "toggle_record" },
                new UIButton { Label = "Export BVH", Message = "export_bvh" },
                new UIButton { Label = "Share", Message = "share_recording" },
            },
            Toggles = new()
            {
                new UIToggle { Label = "Gyro Cam", Id = "gyro_cam", DefaultValue = true },
                new UIToggle { Label = "Smoothing", Id = "smoothing", DefaultValue = true },
            },
            ShowProgressBar = true,
            ShowMessage = true,
            ShowRecordingIndicator = true,
            ShowThumbnail = true,
        };
    }

    private void GenerateDynamicUI()
    {
        foreach (var btn in UIManager.Instance.Config.Buttons)
        {
            var b = new Button { Text = btn.Label };
            b.Clicked += async (s, e) => await OnUIButtonPressed(btn.Message);
            ButtonContainer.Children.Add(b);
        }

        foreach (var t in UIManager.Instance.Config.Toggles)
        {
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                }
            };
            var label = new Label { Text = t.Label, VerticalTextAlignment = TextAlignment.Center };
            var sw = new Switch { IsToggled = t.DefaultValue };
            sw.Toggled += (s, e) => UIManager.Instance.SetToggleState(t.Id, e.Value);
            grid.Add(label);
            grid.Add(sw, 1, 0);
            ToggleContainer.Children.Add(grid);
        }

        ProgressBar.IsVisible = UIManager.Instance.Config.ShowProgressBar;
        MessageLabel.IsVisible = UIManager.Instance.Config.ShowMessage;
        RecordingLabel.IsVisible = UIManager.Instance.Config.ShowRecordingIndicator && UIManager.Instance.IsRecording;
        ThumbnailImage.IsVisible = UIManager.Instance.Config.ShowThumbnail;
    }

    private async Task OnUIButtonPressed(string message)
    {
        switch (message)
        {
            case "load_model":
                var modelFile = await FilePicker.Default.PickAsync();
                if (modelFile != null)
                {
                    _initializer.LoadModel(modelFile.FullPath);
                    UIManager.Instance.SetMessage("Model loaded");
                }
                break;
            case "analyze_video":
                var videoFile = await FilePicker.Default.PickAsync();
                if (videoFile != null)
                {
                    ProgressBar.Progress = 0;
                    ProgressBar.IsVisible = true;
                    await _initializer.AnalyzeVideoAsync(videoFile.FullPath);
                    ProgressBar.IsVisible = false;
                }
                break;
            case "generate_motion":
                _initializer.GenerateMotion();
                UIManager.Instance.SetMessage("Motion generated");
                break;
            case "play_motion":
                _initializer.PlayMotion();
                break;
            case "toggle_record":
                _initializer.ToggleRecord();
                RecordingLabel.IsVisible = UIManager.Instance.IsRecording;
                if (!string.IsNullOrEmpty(_initializer.Recorder?.ThumbnailPath))
                {
                    ThumbnailImage.Source = ImageSource.FromFile(_initializer.Recorder.ThumbnailPath);
                    ThumbnailImage.IsVisible = true;
                }
                break;
            case "export_bvh":
                var exportDir = MmdFileSystem.Ensure("Exported");
                var bvhPath = SystemPath.Combine(exportDir, "motion.bvh");
                _initializer.ExportBvh(bvhPath);
                UIManager.Instance.SetMessage($"BVH exported: {bvhPath}");
                break;
            case "share_recording":
                var recPath = _initializer.Recorder?.GetSavedPath();
                if (!string.IsNullOrEmpty(recPath))
                {
                    await Share.Default.RequestAsync(new ShareFileRequest
                    {
                        Title = "Share Recording",
                        File = new ShareFile(recPath)
                    });
                }
                break;
        }

        MessageLabel.Text = UIManager.Instance.Message;
    }

    private void OnToggleChanged(string id, bool value)
    {
        if (id == "gyro_cam" && _gyroService != null)
        {
            if (value) _gyroService.Start();
            else _gyroService.Stop();
        }
    }
}
