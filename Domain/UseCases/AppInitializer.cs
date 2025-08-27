using MiniMikuDance.Import;
using MiniMikuDance.Recording;
using MiniMikuDance.UI;
using MiniMikuDance.Domain.Interfaces;
using MiniMikuDance.App;
using System;
using System.IO;
using System.Threading.Tasks;
// ビューアーの依存を取り除くため、ビューアー関連のインターフェースを利用します。

namespace MiniMikuDance.Domain.UseCases;

public partial class AppInitializer : IDisposable
{
    /// <summary>
    /// IViewer 実装を外部から注入するためのファクトリ。
    /// </summary>
    public Func<string, float, IViewer>? ViewerFactory { get; set; }

    public IViewer? Viewer { get; private set; }
    public RecorderController? Recorder { get; private set; }
    private Action<float>? _frameUpdatedHandler;
    public ISettingsRepository? SettingsRepository { get; set; }


    public void Initialize(string? modelPath, string baseDir)
    {
        Recorder = new RecorderController(Path.Combine(baseDir, "Recordings"));
        if (!string.IsNullOrEmpty(modelPath) && File.Exists(modelPath))
        {
            LoadModel(modelPath);
        }

        SettingsRepository?.CleanupTemp();
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
        PmxImporter.CacheCapacity = settings.TextureCacheSize;

        _frameUpdatedHandler = async dt =>
        {
            if (Recorder != null && Recorder.IsRecording && Viewer != null)
            {
                var pixels = Viewer.CaptureFrame();
                await Recorder.Capture(pixels, Viewer.Size.X, Viewer.Size.Y);
            }
        };
        Viewer.FrameUpdated += _frameUpdatedHandler;
    }

    public async Task ToggleRecord()
    {
        if (Viewer == null || Recorder == null)
            return;

        if (Recorder.IsRecording)
        {
            var path = await Recorder.StopRecording();
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
            if (_frameUpdatedHandler != null)
            {
                Viewer.FrameUpdated -= _frameUpdatedHandler;
            }
            Viewer.Dispose();
        }

        if (Recorder is IDisposable recorderDisposable)
        {
            recorderDisposable.Dispose();
        }
    }
}
