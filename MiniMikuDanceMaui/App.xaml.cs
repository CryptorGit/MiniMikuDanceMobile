using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using MiniMikuDance.App;
using MiniMikuDance.Data;
using MiniMikuDance.UI;
using System.IO;
#if IOS
using MiniMikuDanceMaui;
#endif

namespace MiniMikuDanceMaui;

public partial class App : Application, IDisposable
{
    public static AppInitializer Initializer { get; } = new();

    public App()
    {
        InitializeComponent();
#if IOS
        Initializer.FrameExtractor = new IosFrameExtractor();
#endif
#if ANDROID
        Initializer.FrameExtractor = new AndroidFrameExtractor();
#endif
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
                var packagePath = Path.Combine("StreamingAssets", "PoseEstimation", modelName);
                using var src = FileSystem.OpenAppPackageFileAsync(packagePath).GetAwaiter().GetResult();
                if (src != null)
                {
                    using var dst = File.Create(poseModel);
                    src.CopyTo(dst);
                }
                else
                {
                    throw new FileNotFoundException($"Package file not found: {packagePath}");
                }
            }
            catch (Exception)
            {
                LogService.WriteLine($"Error copying pose model.");
            }
        }

        Initializer.Initialize(uiConfig, null, poseModel, MmdFileSystem.BaseDir);
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
