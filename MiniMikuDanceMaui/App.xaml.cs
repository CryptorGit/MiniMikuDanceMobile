using Microsoft.Maui.Controls;
namespace MiniMikuDanceMaui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MmdFileSystem.Ensure("Movie");
        MainPage = new NavigationPage(new CameraPage());
    }
}
