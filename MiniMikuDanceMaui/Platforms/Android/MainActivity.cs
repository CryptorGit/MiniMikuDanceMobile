using System;
using Android.App;
using Android.Content.Res;
using Android.Content.PM;
using Android.OS;
using Android.Opengl;
using Android.Views;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using SharpBgfx;

namespace MiniMikuDanceMaui;

[Activity(Theme = "@style/Maui.SplashTheme",
          MainLauncher = true,
          ScreenOrientation = ScreenOrientation.Portrait,
          ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        var display = EGL14.EglGetDisplay(EGL14.EglDefaultDisplay);
        var context = EGL14.EglGetCurrentContext();
        var windowHandle = Window?.DecorView?.Handle ?? IntPtr.Zero;

        MainApplication.BgfxPlatformData = new PlatformData
        {
            DisplayType = (nint)display.Handle,
            Context = (nint)context.Handle,
            WindowHandle = (nint)windowHandle
        };
    }

    public override void OnConfigurationChanged(Configuration newConfig)
    {
        base.OnConfigurationChanged(newConfig);
        var metrics = Resources?.DisplayMetrics;
        //if (metrics != null)
        //{
        //    Bgfx.Reset(metrics.WidthPixels, metrics.HeightPixels, ResetFlags.Vsync);
        //}
    }

    protected override void OnPause()
    {
        base.OnPause();
        Bgfx.Frame();
    }

    protected override void OnResume()
    {
        base.OnResume();
        var metrics = Resources?.DisplayMetrics;
        //if (metrics != null)
        //{
        //    Bgfx.Reset(metrics.WidthPixels, metrics.HeightPixels, ResetFlags.Vsync);
        //}
    }

    protected override void OnDestroy()
    {
        //Bgfx.Shutdown();
        base.OnDestroy();
    }
}
