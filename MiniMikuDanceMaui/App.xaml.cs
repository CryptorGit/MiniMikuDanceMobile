using Microsoft.Maui.Controls;
namespace MiniMikuDanceMaui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new NavigationPage(new CameraPage());
    }
}
