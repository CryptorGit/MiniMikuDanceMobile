using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
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
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await InitializeApp();
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

        var posePath = Path.Combine(FileSystem.CacheDirectory, "pose_model.onnx");
        _initializer.Initialize(_configPath, null, posePath);
        if (_initializer.Camera != null)
        {
            _gyroService = new GyroService(_initializer.Camera);
            if (UIManager.Instance.GetToggle("gyro_cam"))
                _gyroService.Start();
        }
    }

    private async Task<string?> LoadUIConfig()
    {
        var temp = Path.Combine(FileSystem.CacheDirectory, "UIConfig.json");
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
                var bvhPath = Path.Combine(FileSystem.CacheDirectory, "motion.bvh");
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
