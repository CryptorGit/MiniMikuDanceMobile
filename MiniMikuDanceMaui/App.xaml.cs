using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using MiniMikuDance.App;
using MiniMikuDance.Data;
using System.IO;

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
        var bonesConfig = DataManager.Instance.LoadConfig<BonesConfig>("BonesConfig");
        Initializer.BonesConfig = bonesConfig;

        var modelName = "pose_landmark_full.onnx";
        var poseModel = Path.Combine(FileSystem.AppDataDirectory, modelName);
        var modelExists = File.Exists(poseModel);
        if (!modelExists)
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
                modelExists = File.Exists(poseModel);
            }
            catch (Exception)
            {
                LogService.WriteLine("Error copying pose model.", LogService.LogLevel.Error);
            }
        }

        if (modelExists)
        {
            Initializer.Initialize(null, poseModel, MmdFileSystem.BaseDir);
        }
        else
        {
            LogService.WriteLine("Pose model not found. Initialization skipped.", LogService.LogLevel.Warning);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Application.Current?.MainPage?.DisplayAlert("エラー", "ポーズ推定モデルのコピーに失敗したため、初期化をスキップしました。", "OK");
            });
        }
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
