using Android.App;
using Android.Runtime;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using SharpBgfx;

namespace MiniMikuDanceMaui;

[Application]
public class MainApplication : MauiApplication
{
    public static readonly RendererBackend Backend = RendererBackend.OpenGLES;

    public MainApplication(System.IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
