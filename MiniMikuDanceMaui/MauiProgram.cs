using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Controls.Hosting;
using SkiaSharp.Views.Maui.Controls.Hosting;
using System.Diagnostics;
using System;
using System.IO;
using MauiIcons.Material.Outlined;

namespace MiniMikuDanceMaui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        Trace.Listeners.Add(new LogTraceListener());
        Console.SetOut(new LogConsoleWriter(Console.Out));

        builder
            // ← 型パラメータで自分の App クラスを渡す
            .UseMauiApp<App>()
            .UseMaterialOutlinedMauiIcons()
            .UseSkiaSharp()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        return builder.Build();
    }
}
