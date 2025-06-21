using MiniMikuDance.Import;
using MiniMikuDance.PoseEstimation;
using MiniMikuDance.Motion;
using MiniMikuDance.Camera;
using MiniMikuDance.Recording;
using MiniMikuDance.UI;
using MiniMikuDance.Data;

namespace MiniMikuDance.App;

public class AppInitializer
{
    public void Initialize(string uiConfigPath, string modelPath, string poseModelPath)
    {
        UIManager.Instance.LoadConfig(uiConfigPath);
        var importer = new ModelImporter();
        var model = importer.ImportModel(modelPath);
        var estimator = new PoseEstimator(poseModelPath);
        var generator = new MotionGenerator();
        var motionPlayer = new MotionPlayer();
        var camera = new CameraController();
        var recorder = new RecorderController();
        DataManager.Instance.CleanupTemp();
        // Real wiring omitted
    }
}
