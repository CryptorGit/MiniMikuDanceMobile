using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MiniMikuDance.Recording;

public class RecorderController
{
    private bool _recording;
    private string _savedDir = string.Empty;
    private string _infoPath = string.Empty;
    private int _frameIndex;
    private string _thumbnailPath = string.Empty;
    private readonly string _baseDir;

    public RecorderController(string baseDir = "Recordings")
    {
        _baseDir = baseDir;
    }

    public string StartRecording(int width, int height, int fps)
    {
        Directory.CreateDirectory(_baseDir);
        string folder = Path.Combine(_baseDir, $"record_{DateTime.Now:yyyyMMdd_HHmmss}");
        Directory.CreateDirectory(folder);
        _savedDir = folder;
        _infoPath = Path.Combine(folder, "info.txt");
        File.WriteAllText(_infoPath, $"Resolution:{width}x{height} FPS:{fps} Started:{DateTime.Now}\n");
        _frameIndex = 0;
        _recording = true;
        return _savedDir;
    }

    public string StopRecording()
    {
        if (!_recording)
        {
            return _savedDir;
        }

        _recording = false;
        File.AppendAllText(_infoPath, $"Stopped:{DateTime.Now}\n");
        return _savedDir;
    }

    public string ThumbnailPath => _thumbnailPath;

    public bool IsRecording => _recording;

    public void Capture(byte[] rgba, int width, int height)
    {
        if (!_recording) return;

        using var image = Image.LoadPixelData<Rgba32>(rgba, width, height);
        image.Mutate(x => x.Flip(FlipMode.Vertical));
        string path = Path.Combine(_savedDir, $"frame_{_frameIndex:D04}.png");
        image.Save(path);
        if (_frameIndex == 0)
        {
            _thumbnailPath = path;
        }
        _frameIndex++;
    }
}
