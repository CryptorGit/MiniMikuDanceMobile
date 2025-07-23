using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Advanced;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
// OpenCV を使用しない実装へ移行したため依存関係を削除

namespace MiniMikuDance.PoseEstimation;

public class JointData
{
    public float Timestamp { get; set; }
    public Vector3[] Positions { get; set; } = Array.Empty<Vector3>();
    public Vector3[] Rotations { get; set; } = Array.Empty<Vector3>();
    public float[] Confidences { get; set; } = Array.Empty<float>();
}

public class PoseEstimator : IDisposable
{
    private readonly InferenceSession? _session;
    private readonly IVideoFrameExtractor _extractor;

    public PoseEstimator(string modelPath, IVideoFrameExtractor? extractor = null)
    {
        if (File.Exists(modelPath))
        {
            _session = new InferenceSession(modelPath);
        }
        else
        {
            _session = null;
        }
        _extractor = extractor ?? new FfmpegFrameExtractor();
    }

    public Task<JointData[]> EstimateAsync(string videoPath, Action<float>? onProgress = null)
    {
        return Task.Run(() =>
        {
            const int jointCount = 33;

            if (_session == null)
            {
                throw new InvalidOperationException("Pose estimation model not loaded.");
            }

            var meta = _session.InputMetadata.First();
            var dims = meta.Value.Dimensions.Select(d => d <= 0 ? 1 : d).ToArray();
            int height = dims.Length > 1 ? dims[1] : 256;
            int width = dims.Length > 2 ? dims[2] : 256;

            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            try
            {
                var files = _extractor.ExtractFrames(videoPath, 30, tempDir).GetAwaiter().GetResult();

                var results = new List<JointData>(files.Length);
                for (int i = 0; i < files.Length; i++)
                {
                    using var image = Image.Load<Rgb24>(files[i]);
                    image.Mutate(x => x.Resize(width, height));
                    var tensor = new DenseTensor<float>(dims);

                    for (int y = 0; y < height; y++)
                    {
                        var span = image.DangerousGetPixelRowMemory(y).Span;
                        for (int x = 0; x < width; x++)
                        {
                            var p = span[x];
                            tensor[0, y, x, 0] = p.R / 255f;
                            tensor[0, y, x, 1] = p.G / 255f;
                            tensor[0, y, x, 2] = p.B / 255f;
                        }
                    }

                    using var output = _session.Run(new[] { NamedOnnxValue.CreateFromTensor(meta.Key, tensor) });

                    var jd = new JointData
                    {
                        Timestamp = i / 30f,
                        Positions = new Vector3[jointCount],
                        Rotations = new Vector3[jointCount],
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
                    onProgress?.Invoke((i + 1) / (float)files.Length);
                }

                return results.ToArray();
            }
            finally
            {
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch
                {
                    // ignore cleanup failure
                }
            }
        });
    }

    public void Dispose()
    {
        _session?.Dispose();
    }
}
