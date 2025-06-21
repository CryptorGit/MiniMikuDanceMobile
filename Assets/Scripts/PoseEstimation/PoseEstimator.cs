using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// High level API that converts a video file into a sequence of pose data.
/// Utilises EstimatorWorker for each video frame.
/// </summary>
public class PoseEstimator : MonoBehaviour
{
    private EstimatorWorker _worker = new EstimatorWorker();

    /// <summary>
    /// Estimate body pose for each frame of the specified video.
    /// </summary>
    public async Task<JointData[]> EstimateMotion(string videoPath)
    {
        if (!File.Exists(videoPath))
        {
            Debug.LogError($"PoseEstimator: video not found at {videoPath}");
            return new JointData[0];
        }

        var playerGO = new GameObject("VideoPlayer");
        var videoPlayer = playerGO.AddComponent<VideoPlayer>();
        var renderTexture = new RenderTexture(256, 256, 0);
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = videoPath;
        videoPlayer.isLooping = false;
        videoPlayer.playOnAwake = false;
        videoPlayer.prepareCompleted += vp => vp.Play();
        videoPlayer.Prepare();

        var results = new List<JointData>();
        while (!videoPlayer.isPrepared)
        {
            await Task.Yield();
        }

        while (videoPlayer.isPlaying)
        {
            var texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();
            var joints = _worker.Run(texture);

            var data = new JointData
            {
                timestamp = (float)videoPlayer.time,
                positions = joints,
                confidences = new float[joints.Length]
            };
            results.Add(data);
            Object.Destroy(texture);
            await Task.Yield();
        }

        Object.Destroy(renderTexture);
        Object.Destroy(playerGO);

        return results.ToArray();
    }
}
