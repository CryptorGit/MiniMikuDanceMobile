using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Controls.Hosting;
using SkiaSharp.Views.Maui.Controls.Hosting;
using MauiIcons.Material.Outlined;
using MauiIcons.Material;
using MiniMikuDance.Data;
using Microsoft.Maui.Storage;
using System;

namespace MiniMikuDanceMaui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        DataManager.OpenPackageFileFunc = async path =>
        {
            try
            {
                return await FileSystem.OpenAppPackageFileAsync(path);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return null;
            }
        };

        builder
            // ← 型パラメータで自分の App クラスを渡す
            .UseMauiApp<App>()
            .UseMaterialOutlinedMauiIcons()
            .UseMaterialMauiIcons()
            .UseSkiaSharp();

        return builder.Build();
    }
}
