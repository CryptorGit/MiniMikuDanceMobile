using System;
using System.IO;

namespace MiniMikuDance.Recording;

public class RecorderController
{
    private bool _recording;
    private string _savedPath = string.Empty;

    public void StartRecording(int width, int height, int fps)
    {
        Directory.CreateDirectory("Recordings");
        _savedPath = Path.Combine("Recordings", $"record_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        File.WriteAllText(_savedPath, $"Resolution:{width}x{height} FPS:{fps} Started:{DateTime.Now}\n");
        _recording = true;
    }

    public void StopRecording()
    {
        if (!_recording) return;
        _recording = false;
        File.AppendAllText(_savedPath, $"Stopped:{DateTime.Now}\n");
    }

    public string GetSavedPath() => _savedPath;
}
