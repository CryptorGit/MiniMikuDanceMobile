using Microsoft.Maui.Hosting;
using Microsoft.Maui.Controls.Hosting;
using SkiaSharp.Views.Maui.Controls.Hosting;
using System.Diagnostics;
using System;
using MiniMikuDance.Data;
using Microsoft.Maui.Storage;
using CommunityToolkit.Maui;

namespace MiniMikuDanceMaui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
#if DEBUG
        LogService.MinimumLevel = LogService.LogLevel.Debug;
#else
        LogService.MinimumLevel = LogService.LogLevel.Info;
#endif
        Trace.Listeners.Add(new LogTraceListener());
        Console.SetOut(new LogConsoleWriter(Console.Out));

        DataManager.OpenPackageFileFunc = path =>
        {
            try
            {
                return FileSystem.OpenAppPackageFileAsync(path).GetAwaiter().GetResult();
            }
            catch
            {
                return null;
            }
        };

        builder
            .UseMauiApp<App>()
            .UseSkiaSharp()
            .UseMauiCommunityToolkit();

        return builder.Build();
    }
}
