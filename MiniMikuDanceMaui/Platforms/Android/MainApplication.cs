using Android.App;
using Android.Runtime;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using MiniMikuDanceMaui.Helpers;

namespace MiniMikuDanceMaui;

[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(System.IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    public override void OnCreate()
    {
        base.OnCreate();
        CrashLogger.RegisterGlobal();
        CrashLogger.RegisterAndroid();
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
