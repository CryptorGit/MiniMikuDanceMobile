using System;
using Foundation;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using SharpBgfx;
using UIKit;

namespace MiniMikuDanceMaui;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        var handle = Window?.Handle ?? IntPtr.Zero;
        var data = new PlatformData { WindowHandle = handle };
        Bgfx.SetPlatformData(data);
        Bgfx.Init(new InitSettings { Backend = MainApplication.Backend });
        return base.FinishedLaunching(application, launchOptions);
    }

    public override void WillTerminate(UIApplication application)
    {
        Bgfx.Shutdown();
        base.WillTerminate(application);
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
