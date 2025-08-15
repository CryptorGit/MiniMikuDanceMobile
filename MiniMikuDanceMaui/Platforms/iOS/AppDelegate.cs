using Foundation;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using MiniMikuDanceMaui.Helpers;
using UIKit;

namespace MiniMikuDanceMaui;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    public override bool FinishedLaunching(UIApplication app, NSDictionary options)
    {
        CrashLogger.RegisterGlobal();
        CrashLogger.RegisteriOS();
        return base.FinishedLaunching(app, options);
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
