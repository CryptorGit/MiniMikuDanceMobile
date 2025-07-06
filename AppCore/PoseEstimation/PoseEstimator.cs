using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;

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
            const int jointCount = 33;

            using var capture = new VideoCapture(videoPath);
            float fps = (float)capture.Fps;
            if (fps <= 0f || float.IsNaN(fps))
            {
                fps = 30f;
            }

            if (_session == null || !capture.IsOpened())
            {
                // fallback to dummy values
                int frameCount = 30;
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

            int total = (int)capture.FrameCount;
            var results = new List<JointData>(total);
            var meta = _session.InputMetadata.First();
            var dims = meta.Value.Dimensions.Select(d => d <= 0 ? 1 : d).ToArray();
            for (int i = 0; i < total; i++)
            {
                using var frame = new Mat();
                if (!capture.Read(frame) || frame.Empty()) break;
                using var resized = frame.Resize(new Size(dims[3], dims[2]));
                var tensor = new DenseTensor<float>(dims);
                var span = tensor.Buffer.Span;
                for (int y = 0; y < dims[2]; y++)
                {
                    for (int x = 0; x < dims[3]; x++)
                    {
                        var color = resized.At<Vec3b>(y, x);
                        int idx = (y * dims[3] + x) * 3;
                        span[idx + 0] = color.Item2 / 255f; // R
                        span[idx + 1] = color.Item1 / 255f; // G
                        span[idx + 2] = color.Item0 / 255f; // B
                    }
                }
                using var output = _session.Run(new[] { NamedOnnxValue.CreateFromTensor(meta.Key, tensor) });
                var jd = new JointData
                {
                    Timestamp = i / fps,
                    Positions = new Vector3[jointCount],
                    Confidences = new float[jointCount]
                };

                // BlazePose などの ONNX モデルは [1, 33, N] 形式で座標を返すことが多い。
                // 取得したテンソルをフラットな配列に変換し、33 関節分の
                // (x, y, z, score) を JointData に格納する。
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
                onProgress?.Invoke((i + 1) / (float)total);
            }
            return results.ToArray();
        });
    }
}
