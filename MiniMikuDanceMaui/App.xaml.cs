using Microsoft.Maui.Controls;
using MiniMikuDance.App;
using System.IO;

namespace MiniMikuDanceMaui;

public partial class App : Application, IDisposable
{
    public static AppInitializer Initializer { get; } = new();

    public App()
    {
        InitializeComponent();

        Directory.SetCurrentDirectory(MmdFileSystem.BaseDir);
        Initializer.Initialize(null, MmdFileSystem.BaseDir);
    }

    protected override Window CreateWindow(Microsoft.Maui.IActivationState? activationState)
    {
        return new Window(new NavigationPage(new MainPage()));
    }

    public void Dispose()
    {
        Initializer.Dispose();
    }
}
