using Microsoft.ML.OnnxRuntime;

namespace MiniMikuDance.PoseEstimation;

public class JointData
{
    public float Timestamp { get; set; }
    public System.Numerics.Vector3[] Positions { get; set; } = Array.Empty<System.Numerics.Vector3>();
    public float[] Confidences { get; set; } = Array.Empty<float>();
}

public class PoseEstimator
{
    private readonly InferenceSession _session;

    public PoseEstimator(string modelPath)
    {
        _session = new InferenceSession(modelPath);
    }

    public Task<JointData[]> EstimateAsync(string videoPath, Action<float>? onProgress = null)
    {
        // Placeholder: actual video processing and inference not implemented
        onProgress?.Invoke(1f);
        return Task.FromResult(Array.Empty<JointData>());
    }
}
