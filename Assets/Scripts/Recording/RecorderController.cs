using System;
using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
/// Very simple screen recorder that saves individual frame PNGs.
/// This acts as a placeholder until a proper video capture plugin
/// such as NatCorder is integrated.
/// </summary>
public class RecorderController : MonoBehaviour
{
    private bool _recording;
    private int _frameIndex;
    private string _outputDir;
    private string _savedPath;

    /// <summary>
    /// Begin recording at the specified resolution and framerate.
    /// Frames are saved under Application.persistentDataPath.
    /// </summary>
    public void StartRecording(int width, int height, int fps)
    {
        if (_recording)
        {
            Debug.LogWarning("RecorderController: already recording");
            return;
        }

        // Clear temporary cache so old frames do not accumulate
        DataManager.CleanupTemp();

        _outputDir = Path.Combine(Application.persistentDataPath,
            $"recording_{DateTime.Now:yyyyMMdd_HHmmss}");
        Directory.CreateDirectory(_outputDir);

        _frameIndex = 0;
        Time.captureFramerate = fps;
        _recording = true;
        _savedPath = _outputDir;
        StartCoroutine(CaptureFrames(width, height));

        Debug.Log($"RecorderController: recording started to {_outputDir}");
    }

    /// <summary>
    /// Stop recording and reset state.
    /// </summary>
    public void StopRecording()
    {
        if (!_recording)
        {
            Debug.LogWarning("RecorderController: not recording");
            return;
        }

        _recording = false;
        Time.captureFramerate = 0;
        Debug.Log($"RecorderController: recording stopped. Frames saved to {_outputDir}");
    }

    /// <summary>
    /// Return the directory containing captured frames.
    /// </summary>
    public string GetSavedPath() => _savedPath;

    private IEnumerator CaptureFrames(int width, int height)
    {
        while (_recording)
        {
            var filename = Path.Combine(_outputDir, $"frame_{_frameIndex:D04}.png");
            ScreenCapture.CaptureScreenshot(filename, 1);
            _frameIndex++;
            yield return new WaitForEndOfFrame();
        }
    }
}
