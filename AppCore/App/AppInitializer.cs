using MiniMikuDance.Import;
using MiniMikuDance.Recording;
using MiniMikuDance.UI;
using MiniMikuDance.Data;
using MiniMikuDance.Util;
using MiniMikuDance.PoseEstimation;
using System;
using System.IO;
using System.Threading.Tasks;
using ViewerApp;

namespace MiniMikuDance.App;

public partial class AppInitializer : IDisposable
{
    public Viewer? Viewer { get; private set; }
    public RecorderController? Recorder { get; private set; }
    public PoseEstimator? PoseEstimator { get; private set; }
    /// <summary>
    /// 動画フレームを抽出する実装。各プラットフォームで必要に応じて差し替える。
    /// </summary>
    public IVideoFrameExtractor FrameExtractor { get; set; } = new FfmpegFrameExtractor();
    public JointData[]? Joints { get; private set; }
    public BonesConfig? BonesConfig { get; set; }
    private string _poseModelPath = string.Empty;
    private string _poseOutputDir = string.Empty;


    public void Initialize(UIConfig uiConfig, string? modelPath, string poseModelPath, string baseDir)
    {

        UIManager.Instance.LoadConfig(uiConfig);
        _poseModelPath = poseModelPath;
        // FrameExtractor はプラットフォーム側で差し替えられる
        PoseEstimator = new PoseEstimator(poseModelPath, FrameExtractor);

        _poseOutputDir = Path.Combine(baseDir, "Poses");
        Directory.CreateDirectory(_poseOutputDir);

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



        var settings = AppSettings.Load();
        var importer = new ModelImporter { Scale = settings.ModelScale };
        var model = importer.ImportModel(modelPath);

        Viewer = new Viewer(modelPath, settings.ModelScale);

        Viewer.FrameUpdated += dt =>
        {
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
        UIManager.Instance.ExtractProgress = 0f;
        UIManager.Instance.PoseProgress = 0f;
        return outPath;
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

    public void Dispose()
    {
        PoseEstimator?.Dispose();
    }
}
