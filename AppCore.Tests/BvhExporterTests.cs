using System;
using System.IO;
using System.Numerics;
using MiniMikuDance.Motion;
using MiniMikuDance.PoseEstimation;
using Xunit;

public class BvhExporterTests
{
    [Fact]
    public void Export_WritesExpectedContent()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".bvh");
        var motion = new MotionData
        {
            FrameInterval = 0.1f,
            Frames = new[]
            {
                new JointData { Positions = new[] { new Vector3(1, 2, 3) } }
            }
        };
        BvhExporter.Export(motion, path);
        var lines = File.ReadAllLines(path);
        File.Delete(path);

        string[] expected =
        {
            "HIERARCHY",
            "ROOT Hip",
            "{",
            "  OFFSET 0 0 0",
            "  CHANNELS 6 Xposition Yposition Zposition Zrotation Xrotation Yrotation",
            "  End Site",
            "  {",
            "    OFFSET 0 0 0",
            "  }",
            "}",
            "MOTION",
            "Frames: 1",
            "Frame Time: 0.1",
            "1 2 3 0 0 0"
        };
        Assert.Equal(expected, lines);
    }
}
