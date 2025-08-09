using MiniMikuDance.Import;
using MiniMikuDance.Recording;
using MiniMikuDance.UI;
using MiniMikuDance.Data;
using MiniMikuDance.Util;
using MiniMikuDance.PoseEstimation;
using System;
using System.IO;
using System.Threading.Tasks;
// ViewerApp の依存を取り除くため、ビューアー関連のインターフェースを利用します。

namespace MiniMikuDance.App;

public partial class AppInitializer : IDisposable
{
    /// <summary>
    /// IViewer 実装を外部から注入するためのファクトリ。
    /// </summary>
    public Func<string, float, IViewer>? ViewerFactory { get; set; }

    public IViewer? Viewer { get; private set; }
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
        if (ViewerFactory == null)
        {
            throw new InvalidOperationException("ViewerFactory が設定されていません。");
        }
        Viewer = ViewerFactory(modelPath, settings.ModelScale);
        ModelImporter.CacheCapacity = settings.TextureCacheSize;
        using var importer = new ModelImporter { Scale = settings.ModelScale };
        var model = importer.ImportModel(modelPath);

        Viewer.FrameUpdated += async dt =>
        {
            if (Recorder != null && Recorder.IsRecording)
            {
                var pixels = Viewer.CaptureFrame();
                await Recorder.Capture(pixels, Viewer.Size.X, Viewer.Size.Y);
            }
        };
    }

    public async Task<string?> AnalyzeVideoAsync(string videoPath,
        IProgress<float>? extractProgress = null,
        IProgress<float>? poseProgress = null)
    {
        if (PoseEstimator == null)
            return null;
        DataManager.Instance.CleanupTemp();
        UIManager.Instance.SetMessage("Analyzing video...");
        Joints = await PoseEstimator.EstimateAsync(
            videoPath,
            DataManager.Instance.TempDir,
            new Progress<float>(p =>
            {
                UIManager.Instance.ExtractProgress = p;
                extractProgress?.Report(p);
            }),
            new Progress<float>(p =>
            {
                UIManager.Instance.PoseProgress = p;
                poseProgress?.Report(p);
            }));
        string outPath = Path.Combine(_poseOutputDir,
            Path.GetFileNameWithoutExtension(videoPath) + ".json");
        JSONUtil.Save(outPath, Joints);
        UIManager.Instance.SetMessage("Analyze complete");
        UIManager.Instance.ExtractProgress = 0f;
        UIManager.Instance.PoseProgress = 0f;
        return outPath;
    }

    public async Task<string?> AnalyzePhotoAsync(string imagePath,
        IProgress<float>? poseProgress = null)
    {
        if (PoseEstimator == null)
            return null;
        DataManager.Instance.CleanupTemp();
        UIManager.Instance.SetMessage("Analyzing photo...");
        Joints = await PoseEstimator.EstimateImageAsync(
            imagePath,
            new Progress<float>(p =>
            {
                UIManager.Instance.PoseProgress = p;
                poseProgress?.Report(p);
            }));
        string outPath = Path.Combine(_poseOutputDir,
            Path.GetFileNameWithoutExtension(imagePath) + ".json");
        JSONUtil.Save(outPath, Joints);
        UIManager.Instance.SetMessage("Analyze complete");
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
        if (Viewer != null)
        {
            Viewer.Dispose();
        }

        if (Recorder is IDisposable recorderDisposable)
        {
            recorderDisposable.Dispose();
        }

        PoseEstimator?.Dispose();
    }
}
