using MiniMikuDance.Import;
using MiniMikuDance.Motion;
using MiniMikuDance.Camera;
using MiniMikuDance.Recording;
using MiniMikuDance.UI;
using MiniMikuDance.Data;
using MiniMikuDance.Util;
using MiniMikuDance.PoseEstimation;
using System.IO;
using System.Numerics;
using ViewerApp;
using OpenTK.Mathematics;

namespace MiniMikuDance.App;

public class AppInitializer
{
    public Viewer? Viewer { get; private set; }
    public MotionPlayer? MotionPlayer { get; private set; }
    public MotionApplier? Applier { get; private set; }
    public RecorderController? Recorder { get; private set; }
    public CameraController? Camera { get; private set; }

    public void Initialize(string uiConfigPath, string? modelPath, string poseModelPath)
    {
        UIManager.Instance.LoadConfig(uiConfigPath);
        ModelData? model = null;
        if (!string.IsNullOrEmpty(modelPath) && File.Exists(modelPath))
        {
            var importer = new ModelImporter();
            model = importer.ImportModel(modelPath);
        }
        var estimator = new PoseEstimator(poseModelPath);
        var generator = new MotionGenerator();

        MotionPlayer = new MotionPlayer();
        Camera = new CameraController();
        Camera.EnableGyro(UIManager.Instance.GetToggle("gyro_cam"));
        Recorder = new RecorderController();
        if (model != null)
        {
            Applier = new MotionApplier(model);
            MotionPlayer.OnFramePlayed += Applier.Apply;
            Viewer = new Viewer(modelPath!);
            Viewer.FrameUpdated += dt =>
            {
                MotionPlayer.Update(dt);
                Camera?.Update();
                if (Camera != null)
                {
                    var pos = Camera.Position;
                    var rot = Camera.Rotation;
                    var forward = System.Numerics.Vector3.Transform(-System.Numerics.Vector3.UnitZ, rot);
                    var lookAt = pos + forward;
                    Viewer.SetViewMatrix(Matrix4.LookAt(
                        new OpenTK.Mathematics.Vector3(pos.X, pos.Y, pos.Z),
                        new OpenTK.Mathematics.Vector3(lookAt.X, lookAt.Y, lookAt.Z),
                        OpenTK.Mathematics.Vector3.UnitY));
                }
            };
            Viewer.FrameUpdated += dt =>
            {
                if (Recorder != null && Recorder.IsRecording)
                {
                    var pixels = Viewer.CaptureFrame();
                    Recorder.Capture(pixels, Viewer.Size.X, Viewer.Size.Y);
                }
            };
        }
        DataManager.Instance.CleanupTemp();
        // Additional processing can be handled by the host application
    }
}
