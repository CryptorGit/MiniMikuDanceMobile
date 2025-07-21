using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
// OpenCV を使用しない実装へ移行したため依存関係を削除

namespace MiniMikuDance.PoseEstimation;

public class JointData
{
    public float Timestamp { get; set; }
    public Vector3[] Positions { get; set; } = Array.Empty<Vector3>();
    public float[] Confidences { get; set; } = Array.Empty<float>();
}

public class PoseEstimator : IDisposable
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
            const int jointCount = 33;
            const int frameCount = 30;
            const float fps = 30f;

            // OpenCV 依存を排除したため、現状はダミーフレームのみ返す
            if (_session == null)
            {
                var rand = new Random(0);
                var dummy = new JointData[frameCount];
                for (int i = 0; i < frameCount; i++)
                {
                    var jd = new JointData
                    {
                        Timestamp = i / fps,
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
                    dummy[i] = jd;
                    onProgress?.Invoke((i + 1) / (float)frameCount);
                }
                return dummy;
            }

            var results = new List<JointData>(frameCount);
            var meta = _session.InputMetadata.First();
            var dims = meta.Value.Dimensions.Select(d => d <= 0 ? 1 : d).ToArray();
            for (int i = 0; i < frameCount; i++)
            {
                // 入力画像なしでゼロテンソルを与える簡易推論
                var tensor = new DenseTensor<float>(dims);
                using var output = _session.Run(new[] { NamedOnnxValue.CreateFromTensor(meta.Key, tensor) });
                var jd = new JointData
                {
                    Timestamp = i / fps,
                    Positions = new Vector3[jointCount],
                    Confidences = new float[jointCount]
                };

                var outTensor = output.First().AsTensor<float>();
                var data = outTensor.ToArray();
                int stride;

                if (outTensor.Dimensions.Length >= 3 && outTensor.Dimensions[^2] == jointCount)
                {
                    stride = outTensor.Dimensions[^1];
                }
                else
                {
                    stride = data.Length / jointCount;
                }

                for (int j = 0; j < jointCount && j * stride + 2 < data.Length; j++)
                {
                    int idx = j * stride;
                    jd.Positions[j] = new Vector3(data[idx], data[idx + 1], data[idx + 2]);
                    jd.Confidences[j] = stride > 3 && idx + 3 < data.Length ? data[idx + 3] : 1f;
                }

                results.Add(jd);
                onProgress?.Invoke((i + 1) / (float)frameCount);
            }
            return results.ToArray();
        });
    }

    public void Dispose()
    {
        _session?.Dispose();
    }
}
