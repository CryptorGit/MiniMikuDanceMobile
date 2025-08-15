using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Controls.Hosting;
using SkiaSharp.Views.Maui.Controls.Hosting;
using MiniMikuDance.Data;
using Microsoft.Maui.Storage;

namespace MiniMikuDanceMaui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        DataManager.OpenPackageFileFunc = path =>
            FileSystem.OpenAppPackageFileAsync(path).GetAwaiter().GetResult();

        builder
            // ← 型パラメータで自分の App クラスを渡す
            .UseMauiApp<App>()
            .UseSkiaSharp();

        return builder.Build();
    }
}
