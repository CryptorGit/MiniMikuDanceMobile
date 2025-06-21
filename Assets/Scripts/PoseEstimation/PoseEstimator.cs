using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// High level wrapper that performs pose estimation on video files.
/// </summary>
public class PoseEstimator : MonoBehaviour
{
    [SerializeField] private string modelFile = "pose_model.onnx";
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
    /// Analyze the given video and return an array of joint data.
    /// </summary>
    public async Task<JointData[]> EstimateMotion(string videoPath)
    {
        // Placeholder implementation - frame extraction not yet implemented.
        await Task.Yield();
        Debug.Log($"PoseEstimator.EstimateMotion: {videoPath}");
        return Array.Empty<JointData>();
    }
}
