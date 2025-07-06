using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;
using System;
using System.IO;

namespace MiniMikuDanceMaui;

public partial class CapturePage : ContentPage
{
    private readonly string _movieDir;

    public CapturePage()
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
        _movieDir = MmdFileSystem.Ensure("Movie");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
#if ANDROID || IOS
        var cam = await Permissions.RequestAsync<Permissions.Camera>();
        var mic = await Permissions.RequestAsync<Permissions.Microphone>();
        if (cam != PermissionStatus.Granted || mic != PermissionStatus.Granted)
        {
            StatusLabel.Text = "Permission denied";
        }
#endif
    }

    private async void OnRecordClicked(object? sender, EventArgs e)
    {
        try
        {
            var result = await MediaPicker.Default.CaptureVideoAsync();
            if (result != null)
            {
                var ext = Path.GetExtension(result.FileName);
                var name = $"video_{DateTime.Now:yyyyMMdd_HHmmss}{ext}";
                var dstPath = Path.Combine(_movieDir, name);
                await using var source = await result.OpenReadAsync();
                await using var dest = File.Create(dstPath);
                await source.CopyToAsync(dest);
                StatusLabel.Text = $"Saved: {dstPath}";
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void OnHomeClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnPageCameraClicked(object? sender, EventArgs e)
    {
        // Already on camera page; do nothing.
    }

    private async void OnPageHomeClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnSettingClicked(object? sender, EventArgs e)
    {
        await DisplayAlert("Setting", "Not implemented", "OK");
    }
}
