using Microsoft.Maui;
using Microsoft.Maui.Controls;
using MiniMikuDance.App;
using MiniMikuDance.Data;
using MiniMikuDance.UI;
using System.IO;

namespace MiniMikuDanceMaui;

public partial class App : Application, IDisposable
{
    public static AppInitializer Initializer { get; } = new();

    public App()
    {
        InitializeComponent();
        MmdFileSystem.Ensure("Movie");

        Directory.SetCurrentDirectory(MmdFileSystem.BaseDir);
        var uiConfig = DataManager.Instance.LoadConfig<UIConfig>("UIConfig");
        var bonesConfig = DataManager.Instance.LoadConfig<BonesConfig>("BonesConfig");
        Initializer.BonesConfig = bonesConfig;

        Initializer.Initialize(uiConfig, null, MmdFileSystem.BaseDir);
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new NavigationPage(new MainPage()));
    }

    public void Dispose()
    {
        Initializer.Dispose();
    }
}
