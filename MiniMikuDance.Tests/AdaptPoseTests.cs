using System;
using System.IO;
using System.Numerics;
using System.Text.Json;
using MiniMikuDance.Import;
using MiniMikuDance.PoseEstimation;
using MiniMikuDance.Motion;
using MiniMikuDance.Util;
using Xunit;

namespace MiniMikuDance.Tests;

public class AdaptPoseTests
{
    private static Vector3 ToEulerAngles(Quaternion q)
    {
        const float rad2deg = 180f / MathF.PI;
        var angles = Vector3.Zero;
        double sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
        double cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
        angles.X = (float)Math.Atan2(sinr_cosp, cosr_cosp) * rad2deg;
        double sinp = 2 * (q.W * q.Y - q.Z * q.X);
        if (Math.Abs(sinp) >= 1)
            angles.Y = (float)Math.CopySign(Math.PI / 2, sinp) * rad2deg;
        else
            angles.Y = (float)Math.Asin(sinp) * rad2deg;
        double siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
        double cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
        angles.Z = (float)Math.Atan2(siny_cosp, cosy_cosp) * rad2deg;
        return angles;
    }

    [Fact]
    public void PoseJsonGeneratesNonZeroRotationKeyframes()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var modelPath = Path.Combine(root, "StreamingAssets", "VrmModel", "AliciaSolid.vrm");
        var importer = new ModelImporter();
        using var fs = File.OpenRead(modelPath);
        var model = importer.ImportModel(fs);
        var applier = new MotionApplier(model);

        var jsonPath = Path.Combine(root, "Documents", "DanceMovie.json");
        var json = File.ReadAllText(jsonPath);
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        opts.Converters.Add(new Vector3JsonConverter());
        var joints = JsonSerializer.Deserialize<JointData[]>(json, opts)!;
        var motion = new MotionGenerator().Generate(joints);
        Assert.NotEmpty(motion.Frames);

        var (rotations, _) = applier.Apply(motion.Frames[0]);
        Assert.NotEmpty(rotations);
        bool anyNonZero = false;
        foreach (var q in rotations.Values)
        {
            var e = ToEulerAngles(q);
            if (e.LengthSquared() > 1e-6f)
            {
                anyNonZero = true;
                break;
            }
        }
        Assert.True(anyNonZero, "All rotations are zero");
    }
}
