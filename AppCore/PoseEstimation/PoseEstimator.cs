using System;
using System.Linq;
using System.Numerics;
using Microsoft.ML.OnnxRuntime;

namespace MiniMikuDance.PoseEstimation;

public class JointData
{
    public float Timestamp { get; set; }
    public Vector3[] Positions { get; set; } = Array.Empty<Vector3>();
    public float[] Confidences { get; set; } = Array.Empty<float>();
}

public class PoseEstimator
{
    private readonly InferenceSession? _session;

    public PoseEstimator(string modelPath)
    {
        if (File.Exists(modelPath))
        {
            _session = new InferenceSession(modelPath);
        }
        else
        {
            _session = null;
        }
    }

    public Task<JointData[]> EstimateAsync(string videoPath, Action<float>? onProgress = null)
    {
        return Task.Run(() =>
        {
            const int frameCount = 30;
            const int jointCount = 33;
            var data = new JointData[frameCount];
            var rand = new Random(0);

            // Optionally warm up the model if available
            if (_session != null)
            {
                try
                {
                    var meta = _session.InputMetadata.First();
                    var dims = meta.Value.Dimensions.Select(d => d <= 0 ? 1 : d).ToArray();
                    var tensor = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(dims);
                    using var _ = _session.Run(new[] { NamedOnnxValue.CreateFromTensor(meta.Key, tensor) });
                }
                catch
                {
                    // ignore errors in dummy inference
                }
            }

            for (int i = 0; i < frameCount; i++)
            {
                var jd = new JointData
                {
                    Timestamp = i / 30f,
                    Positions = new Vector3[jointCount],
                    Confidences = new float[jointCount]
                };
                for (int j = 0; j < jointCount; j++)
                {
                    jd.Positions[j] = new Vector3(
                        (float)rand.NextDouble(),
                        (float)rand.NextDouble(),
                        (float)rand.NextDouble());
                    jd.Confidences[j] = 1f;
                }
                data[i] = jd;
                onProgress?.Invoke((i + 1) / (float)frameCount);
            }

            return data;
        });
    }
}
