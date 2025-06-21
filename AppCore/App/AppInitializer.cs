using MiniMikuDance.PoseEstimation;
using MiniMikuDance.Motion;
using MiniMikuDance.Camera;
using MiniMikuDance.Recording;
using MiniMikuDance.UI;
using MiniMikuDance.Data;
using System.Threading.Tasks;

namespace MiniMikuDance.App;

public class AppInitializer
{
    public async Task Initialize(string uiConfigPath, string videoPath, string poseModelPath)
    {
        UIManager.Instance.LoadConfig(uiConfigPath);
        var estimator = new PoseEstimator(poseModelPath);
        var generator = new MotionGenerator();
        var motionPlayer = new MotionPlayer();
        var camera = new CameraController();
        var recorder = new RecorderController();
        DataManager.Instance.CleanupTemp();

        var joints = await estimator.EstimateAsync(videoPath, p => { });
        var motion = generator.Generate(joints);
        motionPlayer.Play(motion);
    }
}
