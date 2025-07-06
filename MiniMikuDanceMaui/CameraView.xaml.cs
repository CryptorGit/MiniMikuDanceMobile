using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.IO;

namespace MiniMikuDanceMaui;

public partial class CameraView : ContentView
{
    private readonly string _movieDir;

    public CameraView()
    {
        InitializeComponent();
        _movieDir = MmdFileSystem.Ensure("Movie");
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
                await App.Initializer.AnalyzeVideoAsync(dstPath);
            }
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert("Error", ex.Message, "OK");
        }
    }
}
