using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using MiniMikuDance.App;
using MiniMikuDance.Data;
using MiniMikuDance.UI;
using System.IO;

namespace MiniMikuDanceMaui;

public partial class App : Application
{
    public static AppInitializer Initializer { get; } = new();

    public App()
    {
        InitializeComponent();
        MmdFileSystem.Ensure("Movie");

        Directory.SetCurrentDirectory(MmdFileSystem.BaseDir);
        var uiConfig = DataManager.Instance.LoadConfig<UIConfig>("UIConfig");
        var poseModel = Path.Combine(FileSystem.AppDataDirectory, "pose_landmark_full.onnx");
        Initializer.Initialize(uiConfig, null, poseModel, MmdFileSystem.BaseDir);

        MainPage = new NavigationPage(new CameraPage());
    }
}
