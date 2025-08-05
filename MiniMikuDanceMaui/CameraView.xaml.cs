using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.IO;
using System.Linq;

namespace MiniMikuDanceMaui;

public partial class CameraView : ContentView
{
    private readonly string _movieDir;

    public CameraView(PmxRenderer renderer)
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
            var page = this.Window?.Page ?? Shell.Current ?? Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page != null)
            {
                await page.DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }

}
