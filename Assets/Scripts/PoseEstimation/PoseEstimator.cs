using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// High level pose estimation component that extracts video frames and uses
/// <see cref="EstimatorWorker"/> to generate joint data.
/// </summary>
public class PoseEstimator : MonoBehaviour
{
    [SerializeField] private string modelFile = "pose_estimation.onnx";
    private EstimatorWorker _worker;

    private void Awake()
    {
        _worker = new EstimatorWorker();
        _worker.LoadModel(modelFile);
    }

    private void OnDestroy()
    {
        _worker?.Dispose();
    }

    /// <summary>
    /// Estimate motion from the specified video file and return the generated
    /// joint frames. This is a simplified placeholder implementation.
    /// </summary>
    public async Task<JointData[]> EstimateMotion(string videoPath)
    {
        var results = new List<JointData>();

        var player = gameObject.AddComponent<VideoPlayer>();
        player.source = VideoSource.Url;
        player.url = Path.Combine(Application.streamingAssetsPath, videoPath);
        player.renderMode = VideoRenderMode.APIOnly;
        player.playOnAwake = false;
        player.waitForFirstFrame = true;
        player.skipOnDrop = true;
        player.Prepare();

        while (!player.isPrepared)
            await Task.Yield();

        player.Play();
        while (player.isPlaying)
        {
            if (player.texture != null)
            {
                var renderTex = player.texture as RenderTexture;
                var tex = new Texture2D(renderTex.width, renderTex.height, TextureFormat.RGB24, false);
                RenderTexture.active = renderTex;
                tex.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
                tex.Apply();

                Vector3[] poses = _worker.Execute(tex);
                results.Add(new JointData
                {
                    timestamp = (float)player.time,
                    positions = poses,
                    confidences = new float[poses.Length]
                });

                Object.Destroy(tex);
            }
            await Task.Yield();
        }

        Destroy(player);
        return results.ToArray();
    }
}
