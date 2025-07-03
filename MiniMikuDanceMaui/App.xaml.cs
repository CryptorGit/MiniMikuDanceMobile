using Microsoft.Maui.Controls;
namespace MiniMikuDanceMaui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        System.Diagnostics.Debug.Listeners.Add(new LogTraceListener());
        MainPage = new NavigationPage(new CameraPage());
    }
}
