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
using System.Runtime.Loader;
using System.Runtime.InteropServices;

namespace MiniMikuDanceMaui;

public static class MauiProgram
{
    static class GlobalUnmanagedResolver
    {
        static bool _ = Install();

        public static bool Install()
        {
#if ANDROID
            var dir = Android.App.Application.Context.ApplicationInfo.NativeLibraryDir;

            AssemblyLoadContext.Default.ResolvingUnmanagedDll += (assembly, name) =>
            {
                System.Diagnostics.Debug.WriteLine($"[Resolver] {assembly?.GetName().Name} requests '{name}'");

                try
                {
                    if (name == "bgfx.dll" || name == "bgfx")
                        return NativeLibrary.Load(System.IO.Path.Combine(dir, "libbgfx.so"));
                    if (name == "c++_shared" || name == "libc++_shared.so")
                        return NativeLibrary.Load(System.IO.Path.Combine(dir, "libc++_shared.so"));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Resolver] error: {ex}");
                }
                return IntPtr.Zero; // Fallback to default resolution
            };
#endif
            return true;
        }
    }

    public static MauiApp CreateMauiApp()
    {
        // Ensure the resolver is installed by accessing the class
        GlobalUnmanagedResolver.Install();

        // Hook into first-chance exceptions to get detailed loading errors
        AppDomain.CurrentDomain.FirstChanceException += (s, e) =>
        {
            if (e.Exception is DllNotFoundException || e.Exception is EntryPointNotFoundException)
            {
                System.Diagnostics.Debug.WriteLine("FirstChance: " + e.Exception);
                System.Diagnostics.Debug.WriteLine(Environment.StackTrace);
            }
        };

        var builder = MauiApp.CreateBuilder();

        DataManager.OpenPackageFileFunc = path =>
            FileSystem.OpenAppPackageFileAsync(path).GetAwaiter().GetResult();

        builder
            .UseMauiApp<App>()
            .UseMaterialOutlinedMauiIcons()
            .UseMaterialMauiIcons();

        builder.Services.AddSingleton<IRenderer, BgfxRenderer>();

        return builder.Build();
    }
}
