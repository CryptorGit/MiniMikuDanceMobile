using System;
using System.IO;
using System.Runtime.InteropServices;
using OpenCvSharp;

namespace MiniMikuDance.Recording;

public class RecorderController
{
    private bool _recording;
    private string _savedPath = string.Empty;
    private string _infoPath = string.Empty;
    private int _frameIndex;
    private VideoWriter? _writer;
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
        _savedPath = Path.Combine(folder, "output.mp4");
        _infoPath = Path.Combine(folder, "info.txt");
        File.WriteAllText(_infoPath, $"Resolution:{width}x{height} FPS:{fps} Started:{DateTime.Now}\n");

        _writer = new VideoWriter(_savedPath, FourCC.H264, fps, new Size(width, height));
        _frameIndex = 0;
        _recording = true;
        return _savedPath;
    }

    public string StopRecording()
    {
        if (!_recording)
        {
            return _savedPath;
        }

        _recording = false;
        _writer?.Release();
        File.AppendAllText(_infoPath, $"Stopped:{DateTime.Now}\n");
        return _savedPath;
    }

    public string ThumbnailPath => _thumbnailPath;

    public bool IsRecording => _recording;

    public void Capture(byte[] rgba, int width, int height)
    {
        if (!_recording || _writer == null) return;

        using var mat = new Mat(height, width, MatType.CV_8UC4);
        Marshal.Copy(rgba, 0, mat.Data, rgba.Length);
        Cv2.CvtColor(mat, mat, ColorConversionCodes.RGBA2BGR);
        Cv2.Flip(mat, mat, FlipMode.X);
        if (_frameIndex == 0)
        {
            _thumbnailPath = Path.Combine(Path.GetDirectoryName(_savedPath)!, "thumb.png");
            Cv2.ImWrite(_thumbnailPath, mat);
        }
        _writer.Write(mat);
        _frameIndex++;
    }

    public string GetSavedPath() => _savedPath;
}
