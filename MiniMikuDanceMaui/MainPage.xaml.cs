using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using MiniMikuDance.App;
using MiniMikuDance.UI;

namespace MiniMikuDanceMaui;

public partial class MainPage : ContentPage
{
    private readonly AppInitializer _initializer = new();

    public MainPage()
    {
        InitializeComponent();
    }

    private void OnStartClicked(object sender, EventArgs e)
    {
        LoadUIConfig();
        GenerateDynamicUI();
        StartButton.IsEnabled = false;
    }

    private async void LoadUIConfig()
    {
        using var stream = await FileSystem.OpenAppPackageFileAsync("UIConfig.json");
        var temp = Path.Combine(FileSystem.CacheDirectory, "UIConfig.json");
        using (var fs = File.Create(temp))
        {
            await stream.CopyToAsync(fs);
        }
        UIManager.Instance.LoadConfig(temp);
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

    private Task OnUIButtonPressed(string message)
    {
        UIManager.Instance.SetMessage($"Pressed: {message}");
        MessageLabel.Text = UIManager.Instance.Message;

        if (message == "toggle_record")
        {
            UIManager.Instance.IsRecording = !UIManager.Instance.IsRecording;
            RecordingLabel.IsVisible = UIManager.Instance.IsRecording;
        }

        return DisplayAlert("Button", message, "OK");
    }
}
