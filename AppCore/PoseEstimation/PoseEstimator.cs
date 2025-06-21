using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
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

    public async Task<JointData[]> EstimateAsync(string videoPath, Action<float>? onProgress = null)
    {
        if (!File.Exists(videoPath))
            throw new FileNotFoundException(videoPath);

        const int frameCount = 30;
        var results = new List<JointData>(frameCount);
        for (int i = 0; i < frameCount; i++)
        {
            await Task.Delay(10); // simulate work
            results.Add(new JointData { Timestamp = i / 30f });
            onProgress?.Invoke((i + 1) / (float)frameCount);
        }
        return results.ToArray();
    }
}
