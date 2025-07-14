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
            if (_session == null || !File.Exists(videoPath))
                return Array.Empty<JointData>();

            const int jointCount = 33;
            using var capture = new VideoCapture(videoPath);
            int totalFrames = (int)capture.Get(VideoCaptureProperties.FrameCount);
            if (totalFrames <= 0)
                totalFrames = int.MaxValue; // fallback when unknown
            double fps = capture.Get(VideoCaptureProperties.Fps);
            if (fps <= 0)
                fps = 30.0;

            var meta = _session.InputMetadata.First();
            var dims = meta.Value.Dimensions.Select(d => d <= 0 ? 1 : d).ToArray();
            int height = dims[1];
            int width = dims[2];

            var results = new List<JointData>(totalFrames);
            using var frame = new Mat();
            int frameIndex = 0;
            while (capture.Read(frame) && !frame.Empty())
            {
                using var resized = new Mat();
                Cv2.Resize(frame, resized, new Size(width, height));
                Cv2.CvtColor(resized, resized, ColorConversionCodes.BGR2RGB);
                resized.ConvertTo(resized, MatType.CV_32FC3, 1.0 / 255.0);

                resized.GetArray(out Vec3f[] pixels);
                var tensor = new DenseTensor<float>(dims);
                int idx = 0;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var v = pixels[idx++];
                        tensor[0, y, x, 0] = v.Item0;
                        tensor[0, y, x, 1] = v.Item1;
                        tensor[0, y, x, 2] = v.Item2;
                    }
                }

                using var output = _session.Run(new[] { NamedOnnxValue.CreateFromTensor(meta.Key, tensor) });
                var jd = new JointData
                {
                    Timestamp = (float)(frameIndex / fps),
                    Positions = new Vector3[jointCount],
                    Confidences = new float[jointCount]
                };

                var outTensor = output.First().AsTensor<float>();
                var data = outTensor.ToArray();
                int stride = 5; // x,y,z,visibility,presence
                int landmarkCount = data.Length / stride;
                for (int j = 0; j < jointCount && j < landmarkCount; j++)
                {
                    int di = j * stride;
                    jd.Positions[j] = new Vector3(data[di], data[di + 1], data[di + 2]);
                    jd.Confidences[j] = data[di + 4];
                }

                results.Add(jd);
                frameIndex++;
                if (totalFrames != int.MaxValue)
                    onProgress?.Invoke(frameIndex / (float)totalFrames);
            }
            onProgress?.Invoke(1f);
            return results.ToArray();
        });
    }
}
