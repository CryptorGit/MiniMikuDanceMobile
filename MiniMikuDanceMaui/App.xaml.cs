using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using MiniMikuDance.App;
using MiniMikuDance.Data;
using System.IO;
using System.Threading.Tasks;

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
        MmdFileSystem.Ensure("Movie");
        MmdFileSystem.Ensure("Poses");
        Directory.SetCurrentDirectory(MmdFileSystem.BaseDir);

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        var bonesConfig = await DataManager.Instance.LoadConfigAsync<BonesConfig>("BonesConfig");
        Initializer.BonesConfig = bonesConfig;

        var modelName = "pose_landmark_full.onnx";
        var poseModel = Path.Combine(FileSystem.AppDataDirectory, modelName);
        var modelExists = File.Exists(poseModel);
        if (!modelExists)
        {
            try
            {
                var packagePath = Path.Combine("StreamingAssets", "PoseEstimation", modelName);
                using var src = await FileSystem.OpenAppPackageFileAsync(packagePath);
                if (src != null)
                {
                    await using var dst = File.Create(poseModel);
                    await src.CopyToAsync(dst);
                }
                else
                {
                    throw new FileNotFoundException($"Package file not found: {packagePath}");
                }
                modelExists = File.Exists(poseModel);
            }
            catch (Exception)
            {
                LogService.WriteLine("Error copying pose model.", LogService.LogLevel.Error);
            }
        }
        Initializer.Initialize(null, poseModel, MmdFileSystem.WorkDir);
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
