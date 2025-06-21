using System.Numerics;
using MiniMikuDance.Motion;
using MiniMikuDance.PoseEstimation;

namespace AppCore.Tests;

public class BvhExporterTests
{
    [Fact]
    public void Export_CreatesBvhFileWithFrames()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var path = Path.Combine(tempDir, "test.bvh");

        var motion = new MotionData
        {
            FrameInterval = 1f / 30f,
            Frames = new[]
            {
                new JointData { Positions = new[] { new Vector3(1,2,3) } },
                new JointData { Positions = new[] { new Vector3(4,5,6) } }
            }
        };

        BvhExporter.Export(motion, path);

        Assert.True(File.Exists(path));
        var text = File.ReadAllText(path);
        Assert.Contains("Frames: 2", text);
        Assert.Contains("1 2 3 0 0 0", text);
        Assert.Contains("4 5 6 0 0 0", text);

        Directory.Delete(tempDir, true);
    }
}
