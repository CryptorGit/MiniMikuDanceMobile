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

    public string StartRecording(int width, int height, int fps)
    {
        Directory.CreateDirectory("Recordings");
        string folder = Path.Combine("Recordings", $"record_{DateTime.Now:yyyyMMdd_HHmmss}");
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

    public bool IsRecording => _recording;

    public void Capture(byte[] rgba, int width, int height)
    {
        if (!_recording || _writer == null) return;

        using var mat = new Mat(height, width, MatType.CV_8UC4);
        Marshal.Copy(rgba, 0, mat.Data, rgba.Length);
        Cv2.CvtColor(mat, mat, ColorConversionCodes.RGBA2BGR);
        Cv2.Flip(mat, mat, FlipMode.X);
        _writer.Write(mat);
        _frameIndex++;
    }

    public string GetSavedPath() => _savedPath;
}
