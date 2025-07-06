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
        // カメラプレビューを表示
        if (CameraPreview != null)
        {
            CameraPreview.Source = new HtmlWebViewSource
            {
                Html = @"<!DOCTYPE html><html><body style='margin:0;padding:0;overflow:hidden;background:black;'>" +
                       "<video id='v' autoplay playsinline style='width:100%;height:100%;object-fit:cover;'></video>" +
                       "<script>navigator.mediaDevices.getUserMedia({video:true}).then(s=>{document.getElementById('v').srcObject=s;})" +
                       ".catch(e=>{document.body.innerText='Camera Error: '+e;});</script></body></html>"
            };
        }
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
                await App.Initializer.AnalyzeVideoAsync(dstPath);
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
