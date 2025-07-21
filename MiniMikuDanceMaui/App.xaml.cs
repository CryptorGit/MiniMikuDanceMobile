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
        MmdFileSystem.Ensure("Poses");

        Directory.SetCurrentDirectory(MmdFileSystem.BaseDir);
        var uiConfig = DataManager.Instance.LoadConfig<UIConfig>("UIConfig");
        var bonesConfig = DataManager.Instance.LoadConfig<BonesConfig>("BonesConfig");
        Initializer.BonesConfig = bonesConfig;

        var modelName = "pose_landmark_full.onnx";
        var poseModel = Path.Combine(FileSystem.AppDataDirectory, modelName);
        if (!File.Exists(poseModel))
        {
            try
            {
                var src = Path.Combine(AppContext.BaseDirectory, "StreamingAssets", "PoseEstimation", modelName);
                if (File.Exists(src))
                {
                    File.Copy(src, poseModel);
                }
            }
            catch (Exception)
            {
                // Log the exception or handle it appropriately
                Console.WriteLine($"Error copying pose model.");
            }

        }

        Initializer.Initialize(uiConfig, null, poseModel, MmdFileSystem.BaseDir);

        MainPage = new NavigationPage(new MainPage());
    }
}
