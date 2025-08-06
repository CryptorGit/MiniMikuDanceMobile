using MiniMikuDance.Motion;
using MiniMikuDance.Camera;
using MiniMikuDance.Recording;
using MiniMikuDance.UI;
using MiniMikuDance.Data;
using MiniMikuDance.Util;
using MiniMikuDance.PoseEstimation;
using System;
using System.IO;
using System.Threading.Tasks;
using OpenTK.Mathematics;

namespace MiniMikuDance.App;

public partial class AppInitializer : IDisposable
{
    public IViewer? Viewer { get; private set; }
    public Func<string, float, IViewer>? ViewerFactory { get; set; }
    public MotionPlayer MotionPlayer { get; private set; } = new();
    public MotionApplier? Applier { get; private set; }

    /// <summary>
    /// PMXモデルを読み込んだあとに呼び出し、MotionApplier を更新する。
    /// </summary>
    /// <param name="model">ロード済みのモデルデータ</param>
    public void UpdateApplier(MiniMikuDance.Import.ModelData model)
        => Applier = new MotionApplier(model);
    public RecorderController? Recorder { get; private set; }
    public CameraController? Camera { get; private set; }
    public PoseEstimator? PoseEstimator { get; private set; }
    /// <summary>
    /// 動画フレームを抽出する実装。各プラットフォームで必要に応じて差し替える。
    /// </summary>
    public IVideoFrameExtractor FrameExtractor { get; set; } = new FfmpegFrameExtractor();
    public MotionGenerator? MotionGenerator { get; private set; }
    public JointData[]? Joints { get; private set; }
    public MotionData? Motion { get; set; }
    public BonesConfig? BonesConfig { get; set; }
    public Action<(Dictionary<int, System.Numerics.Quaternion> rotations, System.Numerics.Matrix4x4 transform)>? OnMotionApplied { get; set; }
    private string _poseOutputDir = string.Empty;


    public void Initialize(string? modelPath, string poseModelPath, string baseDir)
    {
        // FrameExtractor はプラットフォーム側で差し替えられる
        PoseEstimator = new PoseEstimator(poseModelPath, FrameExtractor);
        MotionGenerator = new MotionGenerator();

        _poseOutputDir = Path.Combine(baseDir, "Poses");
        Directory.CreateDirectory(_poseOutputDir);

        Camera = new CameraController();
        Recorder = new RecorderController(Path.Combine(baseDir, "Recordings"));

        if (!string.IsNullOrEmpty(modelPath) && File.Exists(modelPath))
        {
            LoadModel(modelPath);
        }

        // Additional processing can be handled by the host application
    }

    public void LoadModel(string modelPath)
    {
        if (string.IsNullOrEmpty(modelPath) || !File.Exists(modelPath))
        {
            return;
        }

        var settings = AppSettings.Load();
        var importer = new MiniMikuDance.Import.ModelImporter { Scale = settings.ModelScale };
        var model = importer.ImportModel(modelPath);

        Applier = new MotionApplier(model);
        MotionPlayer.OnFramePlayed += (joint) =>
        {
            if (Applier == null) return;
            var result = Applier.Apply(joint);
            OnMotionApplied?.Invoke(result);
        };
        if (ViewerFactory == null)
            throw new InvalidOperationException("ViewerFactory is not set.");
        Viewer = ViewerFactory(modelPath, settings.ModelScale);

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

            if (Recorder != null && Recorder.IsRecording)
            {
                var pixels = Viewer.CaptureFrame();
                Recorder.Capture(pixels, Viewer.Size.X, Viewer.Size.Y);
            }
        };
    }

    public async Task<string?> AnalyzeVideoAsync(string videoPath,
        Action<float>? extractProgress = null,
        Action<float>? poseProgress = null)
    {
        if (PoseEstimator == null)
            return null;
        DataManager.Instance.CleanupTemp();
        UIManager.Instance.SetMessage("Analyzing video...");
        Joints = await PoseEstimator.EstimateAsync(
            videoPath,
            DataManager.Instance.TempDir,
            p =>
            {
                UIManager.Instance.ExtractProgress = p;
                extractProgress?.Invoke(p);
            },
            p =>
            {
                UIManager.Instance.PoseProgress = p;
                poseProgress?.Invoke(p);
            });
        string outPath = Path.Combine(_poseOutputDir,
            Path.GetFileNameWithoutExtension(videoPath) + ".json");
        JSONUtil.Save(outPath, Joints);
        UIManager.Instance.SetMessage("Analyze complete");
        DataManager.Instance.CleanupTemp();
        UIManager.Instance.ExtractProgress = 0f;
        UIManager.Instance.PoseProgress = 0f;
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
        if (Motion == null)
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
}
