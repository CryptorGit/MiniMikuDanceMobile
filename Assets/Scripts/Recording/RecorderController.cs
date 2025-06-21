using System;
using System.Collections;
using System.IO;
using UnityEngine;
#if NATCORDER
using NatSuite.Recorders;
using NatSuite.Recorders.Clocks;
using NatSuite.Recorders.Inputs;
#endif

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
    private string _thumbnailPath;
#if NATCORDER
    private MP4Recorder _recorder;
    private CameraInput _cameraInput;
    private IClock _clock;
#endif

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

#if NATCORDER
        _savedPath = Path.Combine(Application.persistentDataPath,
            $"recording_{DateTime.Now:yyyyMMdd_HHmmss}.mp4");
        _clock = new RealtimeClock();
        _recorder = new MP4Recorder(width, height, fps, _savedPath);
        _cameraInput = new CameraInput(_recorder, _clock, Camera.main);
        _recording = true;
        Debug.Log($"RecorderController: recording video to {_savedPath}");
#else
        _outputDir = Path.Combine(Application.persistentDataPath,
            $"recording_{DateTime.Now:yyyyMMdd_HHmmss}");
        Directory.CreateDirectory(_outputDir);

        _frameIndex = 0;
        Time.captureFramerate = fps;
        _recording = true;
        _savedPath = _outputDir;
        StartCoroutine(CaptureFrames(width, height));

        Debug.Log($"RecorderController: recording started to {_outputDir}");
#endif
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
#if NATCORDER
        _cameraInput?.Dispose();
        _cameraInput = null;
        if (_recorder != null)
        {
            _recorder.FinishWriting().ContinueWith(path =>
            {
                _savedPath = path;
                SaveThumbnail();
                Debug.Log($"RecorderController: video saved to {path}");
            });
        }
#else
        Time.captureFramerate = 0;
        SaveThumbnail();
        Debug.Log($"RecorderController: recording stopped. Frames saved to {_outputDir}");
#endif
    }

    /// <summary>
    /// Return the directory containing captured frames.
    /// </summary>
    public string GetSavedPath() => _savedPath;

    /// <summary>
    /// Return the path to a small thumbnail image of the recording.
    /// </summary>
    public string GetThumbnailPath() => _thumbnailPath;

    /// <summary>
    /// Invoke the platform share dialog for the saved recording.
    /// </summary>
    public void ShareRecording()
    {
        string target = _thumbnailPath;
        if (string.IsNullOrEmpty(target) || !File.Exists(target))
        {
            target = _savedPath;
        }

        if (string.IsNullOrEmpty(target))
        {
            Debug.LogWarning("RecorderController.ShareRecording: nothing to share");
            return;
        }

        ShareUtility.ShareFile(target, "Check out my dance!");
    }

#if !NATCORDER
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
#endif

    private void SaveThumbnail()
    {
#if NATCORDER
        _thumbnailPath = Path.Combine(Application.persistentDataPath, "thumbnail.png");
        ScreenCapture.CaptureScreenshot(_thumbnailPath);
#else
        var firstFrame = Path.Combine(_outputDir, "frame_0000.png");
        if (!File.Exists(firstFrame))
            return;

        _thumbnailPath = Path.Combine(_outputDir, "thumbnail.png");
        try
        {
            File.Copy(firstFrame, _thumbnailPath, true);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"RecorderController.SaveThumbnail: {ex}");
            _thumbnailPath = null;
        }
#endif
    }
}
