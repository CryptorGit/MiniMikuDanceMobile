using System;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Controls.Hosting;
using MauiIcons.Material.Outlined;
using MauiIcons.Material;
using MiniMikuDance.Data;
using Microsoft.Maui.Storage;
using Microsoft.Extensions.DependencyInjection;
using MiniMikuDance.App;

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
            .UseMaterialOutlinedMauiIcons()
            .UseMaterialMauiIcons();

        builder.Services.AddSingleton<IRenderer, BgfxRenderer>();

        return builder.Build();
    }
}
