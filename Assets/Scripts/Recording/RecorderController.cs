using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Very simple frame recorder that saves PNG sequences.
/// This acts as a placeholder until a proper video plugin is integrated.
/// </summary>
public class RecorderController : MonoBehaviour
{
    private bool _recording;
    private float _interval;
    private float _nextFrame;
    private string _outputDir;
    private readonly List<string> _frames = new List<string>();

    /// <summary>
    /// Begin recording to a numbered PNG sequence.
    /// </summary>
    public void StartRecording(int width, int height, int fps)
    {
        if (_recording) return;

        _interval = 1f / Mathf.Max(1, fps);
        _nextFrame = 0f;
        _outputDir = Path.Combine(Application.persistentDataPath,
            $"recording_{DateTime.Now:yyyyMMdd_HHmmss}");
        Directory.CreateDirectory(_outputDir);
        _frames.Clear();
        _recording = true;
        Debug.Log($"Recorder: started {_outputDir}");
    }

    /// <summary>
    /// Stop recording and return the output directory path.
    /// </summary>
    public string StopRecording()
    {
        if (!_recording) return _outputDir;

        _recording = false;
        Debug.Log($"Recorder: stopped with {_frames.Count} frames");
        return _outputDir;
    }

    /// <summary>
    /// Get the directory where frames were saved.
    /// </summary>
    public string GetSavedPath() => _outputDir;

    private void LateUpdate()
    {
        if (!_recording) return;

        if (Time.time >= _nextFrame)
        {
            CaptureFrame();
            _nextFrame = Time.time + _interval;
        }
    }

    private void CaptureFrame()
    {
        var texture = ScreenCapture.CaptureScreenshotAsTexture();
        var bytes = texture.EncodeToPNG();
        UnityEngine.Object.Destroy(texture);

        var file = Path.Combine(_outputDir, $"frame_{_frames.Count:D04}.png");
        File.WriteAllBytes(file, bytes);
        _frames.Add(file);
    }
}
