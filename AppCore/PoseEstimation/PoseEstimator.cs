using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Drawing.Processing;
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

    private DenseTensor<float>? _tensorCache;
    private Rgb24[]? _pixelCache;
    private int _cachedWidth;
    private int _cachedHeight;

    private static readonly int[] LrMap = new int[]
    {
        0,4,5,6,1,2,3,8,7,10,9,
        12,11,14,13,16,15,18,17,20,19,22,21,
        24,23,26,25,28,27,30,29,32,31
    };

    private static readonly float[] Angles = { -40f, -20f, 0f, 20f, 40f };
    private static readonly float[] Scales = { 0.32f, 0.38f, 0.44f };

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

    private static float AverageScore(float[] conf)
    {
        if (conf.Length == 0) return float.NegativeInfinity;
        float sum = 0f; int n = 0;
        foreach (var c in conf)
        {
            if (float.IsNaN(c)) continue;
            sum += c; n++;
        }
        return n > 0 ? sum / n : float.NegativeInfinity;
    }

    private void EnsureBuffers(int[] dims)
    {
        int h = dims.Length > 1 ? dims[1] : 256;
        int w = dims.Length > 2 ? dims[2] : 256;
        if (_tensorCache == null || _cachedWidth != w || _cachedHeight != h)
        {
            _tensorCache = new DenseTensor<float>(dims);
            _pixelCache = new Rgb24[w * h];
            _cachedWidth = w;
            _cachedHeight = h;
        }
        else if (_pixelCache == null || _pixelCache.Length != w * h)
        {
            _pixelCache = new Rgb24[w * h];
        }
    }

    private (Vector3[] pos, float[] conf) RunModel(Image<Rgb24> img, string inputName, int[] dims, int jointCount)
    {
        EnsureBuffers(dims);
        var tensor = _tensorCache!;
        var pixels = _pixelCache!;
        img.CopyPixelDataTo(pixels);

        float inv = 1f / 255f;
        var dst = MemoryMarshal.Cast<float, Vector3>(tensor.Buffer.Span);

        for (int i = 0; i < pixels.Length; i++)
        {
            var p = pixels[i];
            dst[i] = new Vector3(p.R, p.G, p.B) * inv;
        }

        using var output = _session!.Run(new[] { NamedOnnxValue.CreateFromTensor(inputName, tensor) });
        var outTensor = (DenseTensor<float>)output.First().AsTensor<float>();
        var span = outTensor.Buffer.Span;
        int stride;

        if (outTensor.Dimensions.Length >= 3 && outTensor.Dimensions[^2] == jointCount)
            stride = outTensor.Dimensions[^1];
        else
            stride = span.Length / jointCount;

        var pos = new Vector3[jointCount];
        var conf = new float[jointCount];

        for (int j = 0; j < jointCount && j * stride + 2 < span.Length; j++)
        {
            int idx = j * stride;
            pos[j] = new Vector3(span[idx], span[idx + 1], span[idx + 2]);
            conf[j] = stride > 3 && idx + 3 < span.Length ? span[idx + 3] : 1f;
        }

        return (pos, conf);
    }

    private (Vector3[] pos, float[] conf) InferPatch(Image<Rgb24> patch, string inputName, int[] dims, int jointCount, bool flipTta)
    {
        var (p1, c1) = RunModel(patch, inputName, dims, jointCount);
        if (!flipTta) return (p1, c1);

        using var flipped = patch.Clone(ctx => ctx.Flip(FlipMode.Horizontal));
        var (p2orig, c2orig) = RunModel(flipped, inputName, dims, jointCount);
        int w = dims.Length > 2 ? dims[2] : 256;

        var p2 = new Vector3[jointCount];
        var c2 = new float[jointCount];
        for (int i = 0; i < jointCount; i++)
        {
            int k = LrMap[i];
            var pp = p2orig[k];
            p2[i] = new Vector3(w - pp.X, pp.Y, pp.Z);
            c2[i] = c2orig[k];
        }

        var pos = new Vector3[jointCount];
        var conf = new float[jointCount];
        for (int i = 0; i < jointCount; i++)
        {
            float w1 = Math.Clamp(c1[i], 0.1f, 1f);
            float w2 = Math.Clamp(c2[i], 0.1f, 1f);
            pos[i] = (p1[i] * w1 + p2[i] * w2) / (w1 + w2);
            conf[i] = (c1[i] + c2[i]) * 0.5f;
        }
        return (pos, conf);
    }

    private JointData SearchBest(Image<Rgb24> frame, string inputName, int[] dims, int jointCount, bool flipTta)
    {
        int H = frame.Height;
        int W = frame.Width;
        int dstH = dims.Length > 1 ? dims[1] : 256;
        int dstW = dims.Length > 2 ? dims[2] : 256;

        var center = (cx: W * 0.5f, cy: H * 0.55f);

        float bestScore = float.NegativeInfinity;
        Vector3[] bestPos = new Vector3[jointCount];
        float[] bestConf = new float[jointCount];

        using var rotated = new Image<Rgb24>(dstW, dstH);

        foreach (var s in Scales)
        {
            float half = s * Math.Min(W, H);
            int size = (int)(half * 2f);
            int x0 = (int)Math.Round(center.cx - half);
            int y0 = (int)Math.Round(center.cy - half);
            var rect = new Rectangle(x0, y0, size, size);

            using var patch = frame.Clone(ctx =>
            {
                ctx.Crop(rect);
                ctx.Resize(dstW, dstH);
            });

            foreach (var ang in Angles)
            {
                rotated.Mutate(ctx =>
                {
                    ctx.Clear(Color.Black);
                    ctx.DrawImage(patch, 1f);
                    if (Math.Abs(ang) > 0.1f)
                    {
                        ctx.Rotate((float)ang);
                        ctx.Crop(new Rectangle(0, 0, dstW, dstH));
                    }
                });

                var (pos, conf) = InferPatch(rotated, inputName, dims, jointCount, flipTta);
                float sc = AverageScore(conf);
                if (sc > bestScore)
                {
                    bestScore = sc;
                    Array.Copy(pos, bestPos, jointCount);
                    Array.Copy(conf, bestConf, jointCount);
                }
            }
        }

        return new JointData
        {
            Positions = bestPos,
            Confidences = bestConf,
            Rotations = new Vector3[jointCount]
        };
    }

    public async Task<JointData[]> EstimateAsync(
        string videoPath,
        string tempDir,
        IProgress<float>? extractProgress = null,
        IProgress<float>? poseProgress = null)
    {
        const int jointCount = 33;

        if (_session == null)
        {
            throw new InvalidOperationException("Pose estimation model not loaded.");
        }

        var meta = _session.InputMetadata.First();
        var dims = meta.Value.Dimensions.Select(d => d <= 0 ? 1 : d).ToArray();

        Directory.CreateDirectory(tempDir);

        try
        {
            var files = await _extractor.ExtractFrames(
                videoPath,
                30,
                tempDir,
                p => extractProgress?.Report(p));

            var results = new List<JointData>(files.Length);
            for (int i = 0; i < files.Length; i++)
            {
                using var image = await Image.LoadAsync<Rgb24>(files[i]);
                var jd = SearchBest(image, meta.Key, dims, jointCount, true);
                jd.Timestamp = i / 30f;
                results.Add(jd);
                poseProgress?.Report((i + 1) / (float)files.Length);
            }

            return results.ToArray();
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch
            {
                // ignore
            }
        }
    }

    public async Task<JointData[]> EstimateImageAsync(string imagePath, IProgress<float>? poseProgress = null)
    {
        const int jointCount = 33;
        if (_session == null)
        {
            throw new InvalidOperationException("Pose estimation model not loaded.");
        }

        var meta = _session.InputMetadata.First();
        var dims = meta.Value.Dimensions.Select(d => d <= 0 ? 1 : d).ToArray();

        using var image = await Image.LoadAsync<Rgb24>(imagePath);
        var jd = SearchBest(image, meta.Key, dims, jointCount, true);
        jd.Timestamp = 0f;
        poseProgress?.Report(1f);
        return new[] { jd };
    }

    public void Dispose()
    {
        _session?.Dispose();
    }
}
