using System;
using System.Collections.Generic;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MiniMikuDance.Recording;

public class RecorderController
{
    private bool _recording;
    private readonly List<Image<Rgba32>> _frames = new();
    private string _savedPath = string.Empty;

    public void StartRecording(int width, int height, int fps)
    {
        _recording = true;
        _frames.Clear();
    }

    public void CaptureFrame(byte[] rgbaData, int width, int height)
    {
        if (!_recording) return;
        var img = Image.WrapMemory<Rgba32>(rgbaData, width, height);
        _frames.Add(img.Clone());
        img.Dispose();
    }

    public void StopRecording()
    {
        if (!_recording) return;
        _recording = false;
        string dir = Path.Combine(Path.GetTempPath(), "MiniMikuDance", "recordings");
        Directory.CreateDirectory(dir);
        _savedPath = Path.Combine(dir, $"rec_{DateTime.Now:yyyyMMdd_HHmmss}");
        Directory.CreateDirectory(_savedPath);
        for (int i = 0; i < _frames.Count; i++)
        {
            string file = Path.Combine(_savedPath, $"frame_{i:D04}.png");
            _frames[i].Save(file);
            _frames[i].Dispose();
        }
        _frames.Clear();
    }

    public string GetSavedPath() => _savedPath;
}
