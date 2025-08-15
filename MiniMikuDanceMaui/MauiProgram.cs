using System;
using System.IO;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Controls.Hosting;
using SkiaSharp.Views.Maui.Controls.Hosting;
using MauiIcons.Material.Outlined;
using MauiIcons.Material;
using MiniMikuDance.Data;
using MiniMikuDanceMaui.Helpers;
using MauiFileSystem = Microsoft.Maui.Storage.FileSystem;

namespace MiniMikuDanceMaui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        DataManager.OpenPackageFileFunc = path =>
        {
            try
            {
                return MauiFileSystem
                    .OpenAppPackageFileAsync(path)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (FileNotFoundException)
            {
                return null;    // パッケージに存在しない場合は既定値で進む
            }
        };

        builder
            // ← 型パラメータで自分の App クラスを渡す
            .UseMauiApp<App>()
            .UseMaterialOutlinedMauiIcons()
            .UseMaterialMauiIcons()
            .UseSkiaSharp();

        var app = builder.Build();
        CrashLogger.RegisterGlobal();
        return app;
    }
}
