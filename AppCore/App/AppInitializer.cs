using MiniMikuDance.Import;
using MiniMikuDance.Motion;
using MiniMikuDance.Camera;
using MiniMikuDance.Recording;
using MiniMikuDance.UI;
using MiniMikuDance.Data;
using MiniMikuDance.Util;
using MiniMikuDance.PoseEstimation;
using System;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using System.Diagnostics;
using ViewerApp;
using OpenTK.Mathematics;

namespace MiniMikuDance.App;

public partial class AppInitializer : IDisposable
{
    public Viewer? Viewer { get; private set; }
    public MotionPlayer? MotionPlayer { get; private set; }
    public MotionApplier? Applier { get; private set; }
    public RecorderController? Recorder { get; private set; }
    public CameraController? Camera { get; private set; }
    public PoseEstimator? PoseEstimator { get; private set; }
    public IVideoFrameExtractor FrameExtractor { get; set; } = new FfmpegFrameExtractor();
    public MotionGenerator? MotionGenerator { get; private set; }
    public JointData[]? Joints { get; private set; }
    public MotionData? Motion { get; set; }
    public BonesConfig? BonesConfig { get; set; }
    public Action<(Dictionary<int, System.Numerics.Quaternion> rotations, System.Numerics.Matrix4x4 transform)>? OnMotionApplied { get; set; }
    private string _poseModelPath = string.Empty;
    private string _poseOutputDir = string.Empty;

    partial void ConfigureFrameExtractor(ref IVideoFrameExtractor extractor);

    public void Initialize(UIConfig uiConfig, string? modelPath, string poseModelPath, string baseDir)
    {

        UIManager.Instance.LoadConfig(uiConfig);
        _poseModelPath = poseModelPath;
        IVideoFrameExtractor extractor = new FfmpegFrameExtractor();
        ConfigureFrameExtractor(ref extractor);
        PoseEstimator = new PoseEstimator(poseModelPath, extractor);
        MotionGenerator = new MotionGenerator();

        _poseOutputDir = Path.Combine(baseDir, "Poses");
        Directory.CreateDirectory(_poseOutputDir);

        MotionPlayer = new MotionPlayer();
        Camera = new CameraController();
        Camera.EnableGyro(UIManager.Instance.GetToggle("gyro_cam"));
        Recorder = new RecorderController(Path.Combine(baseDir, "Recordings"));

        if (!string.IsNullOrEmpty(modelPath) && File.Exists(modelPath))
        {
            LoadModel(modelPath);
        }

        DataManager.Instance.CleanupTemp();
        // Additional processing can be handled by the host application
    }

    public void LoadModel(string modelPath)
    {
        if (string.IsNullOrEmpty(modelPath) || !File.Exists(modelPath))
        {

            return;
        }



        var importer = new ModelImporter();
        var model = importer.ImportModel(modelPath);

        Applier = new MotionApplier(model);
        MotionPlayer ??= new MotionPlayer();
        MotionPlayer.OnFramePlayed += (joint) =>
        {
            if (Applier == null) return;
            var result = Applier.Apply(joint);
            OnMotionApplied?.Invoke(result);
        };
        Viewer = new Viewer(modelPath);

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

    public async Task<string?> AnalyzeVideoAsync(string videoPath)
    {
        if (PoseEstimator == null)
            return null;
        UIManager.Instance.SetMessage("Analyzing video...");
        Joints = await PoseEstimator.EstimateAsync(videoPath, p => UIManager.Instance.Progress = p);
        string outPath = Path.Combine(_poseOutputDir,
            Path.GetFileNameWithoutExtension(videoPath) + ".json");
        JSONUtil.Save(outPath, Joints);
        UIManager.Instance.SetMessage("Analyze complete");
        UIManager.Instance.Progress = 0f;
        return outPath;
    }

    public void GenerateMotion()
    {
        if (MotionGenerator == null || Joints == null)
            return;
        Motion = MotionGenerator.Generate(Joints);
    }

    public void PlayMotion()
    {
        if (MotionPlayer == null || Motion == null)
            return;
        MotionPlayer.Play(Motion);
    }

    public void ToggleRecord()
    {
        if (Viewer == null || Recorder == null)
            return;

        if (Recorder.IsRecording)
        {
            var path = Recorder.StopRecording();
            UIManager.Instance.IsRecording = false;
            if (!string.IsNullOrEmpty(Recorder.ThumbnailPath))
            {
                UIManager.Instance.SetThumbnail(Recorder.ThumbnailPath);
            }
            UIManager.Instance.SetMessage($"Saved: {path}");
        }
        else
        {
            var size = Viewer.Size;
            var path = Recorder.StartRecording(size.X, size.Y, 30);
            UIManager.Instance.IsRecording = true;
            UIManager.Instance.SetMessage($"Recording: {path}");
        }
    }

    public void ExportBvh(string path)
    {
        if (Motion == null)
            return;
        BvhExporter.Export(Motion, path);
    }

    public void Dispose()
    {
        PoseEstimator?.Dispose();
    }

    partial void ConfigureFrameExtractor(ref IVideoFrameExtractor extractor)
    {
    }
}
