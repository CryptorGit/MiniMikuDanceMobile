using Android.App;
using Android.Runtime;

namespace MiniMikuDanceMaui;

[Application]
public class MainApplication : Microsoft.Maui.MauiApplication
{
    public MainApplication(System.IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    protected override Microsoft.Maui.Hosting.MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
