using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;

namespace MiniMikuDance.Recording;

public class RecorderController
{
    private bool _recording;
    private string _savedPath = string.Empty;
    private int _frameIndex;

    public void StartRecording(int width, int height, int fps)
    {
        Directory.CreateDirectory("Recordings");
        _savedPath = Path.Combine("Recordings", $"record_{DateTime.Now:yyyyMMdd_HHmmss}");
        Directory.CreateDirectory(_savedPath);
        File.WriteAllText(Path.Combine(_savedPath, "info.txt"), $"Resolution:{width}x{height} FPS:{fps} Started:{DateTime.Now}\n");
        _frameIndex = 0;
        _recording = true;
    }

    public void StopRecording()
    {
        if (!_recording) return;
        _recording = false;
        File.AppendAllText(Path.Combine(_savedPath, "info.txt"), $"Stopped:{DateTime.Now}\n");
    }

    public bool IsRecording => _recording;

    public void Capture(byte[] rgba, int width, int height)
    {
        if (!_recording) return;
        using var image = SixLabors.ImageSharp.Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(rgba, width, height);
        image.Mutate(x => x.Flip(FlipMode.Vertical));
        string file = Path.Combine(_savedPath, $"frame_{_frameIndex:D05}.png");
        image.Save(file);
        _frameIndex++;
    }

    public string GetSavedPath() => _savedPath;
}
