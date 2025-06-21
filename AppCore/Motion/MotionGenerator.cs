using MiniMikuDance.PoseEstimation;
using System.Numerics;

namespace MiniMikuDance.Motion;

public class MotionData
{
    public float FrameInterval { get; set; }
    public JointData[] Frames { get; set; } = Array.Empty<JointData>();
}

public class MotionGenerator
{
    private readonly int _smoothWindow;

    public MotionGenerator(int smoothWindow = 2)
    {
        _smoothWindow = Math.Max(0, smoothWindow);
    }

    private static JointData Lerp(JointData a, JointData b, float timestamp)
    {
        int jointCount = a.Positions.Length;
        var result = new JointData
        {
            Timestamp = timestamp,
            Positions = new Vector3[jointCount],
            Confidences = new float[jointCount]
        };

        float alpha = (timestamp - a.Timestamp) / (b.Timestamp - a.Timestamp);
        for (int i = 0; i < jointCount; i++)
        {
            result.Positions[i] = Vector3.Lerp(a.Positions[i], b.Positions[i], alpha);
            result.Confidences[i] = a.Confidences[i] + (b.Confidences[i] - a.Confidences[i]) * alpha;
        }
        return result;
    }

    public MotionData Generate(JointData[] joints)
    {
        if (joints.Length == 0)
            return new MotionData { FrameInterval = 1f / 30f };

        float interval = joints.Length > 1
            ? (joints[^1].Timestamp - joints[0].Timestamp) / (joints.Length - 1)
            : 1f / 30f;

        // --- interpolation ---
        int frameCount = (int)MathF.Round((joints[^1].Timestamp - joints[0].Timestamp) / interval) + 1;
        var interpolated = new JointData[frameCount];
        int idx = 0;
        for (int i = 0; i < frameCount; i++)
        {
            float t = joints[0].Timestamp + i * interval;
            while (idx < joints.Length - 2 && joints[idx + 1].Timestamp < t)
                idx++;

            var a = joints[idx];
            var b = joints[Math.Min(idx + 1, joints.Length - 1)];
            interpolated[i] = MathF.Abs(b.Timestamp - a.Timestamp) < 1e-5f
                ? a
                : Lerp(a, b, t);
        }

        // --- smoothing ---
        int jointCount = interpolated[0].Positions.Length;
        var smoothed = new JointData[frameCount];
        for (int i = 0; i < frameCount; i++)
        {
            int start = Math.Max(0, i - _smoothWindow);
            int end = Math.Min(frameCount - 1, i + _smoothWindow);
            int span = end - start + 1;

            var jd = new JointData
            {
                Timestamp = interpolated[i].Timestamp,
                Positions = new Vector3[jointCount],
                Confidences = new float[jointCount]
            };

            for (int j = 0; j < jointCount; j++)
            {
                Vector3 posSum = Vector3.Zero;
                float confSum = 0f;
                for (int k = start; k <= end; k++)
                {
                    posSum += interpolated[k].Positions[j];
                    confSum += interpolated[k].Confidences[j];
                }
                jd.Positions[j] = posSum / span;
                jd.Confidences[j] = confSum / span;
            }

            smoothed[i] = jd;
        }

        return new MotionData
        {
            FrameInterval = interval,
            Frames = smoothed
        };
    }
}
