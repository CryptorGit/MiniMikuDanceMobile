using System;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Controls.Hosting;
using SkiaSharp.Views.Maui.Controls.Hosting;
using MauiIcons.Material.Outlined;
using MauiIcons.Material;
using MiniMikuDance.Data;
using MauiFileSystem = Microsoft.Maui.Storage.FileSystem;

namespace MiniMikuDanceMaui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        DataManager.OpenPackageFileFunc = path =>
            MauiFileSystem.OpenAppPackageFileAsync(path).GetAwaiter().GetResult();

        builder
            // ← 型パラメータで自分の App クラスを渡す
            .UseMauiApp<App>()
            .UseMaterialOutlinedMauiIcons()
            .UseMaterialMauiIcons()
            .UseSkiaSharp();

        return builder.Build();
    }
}
