using System.Numerics;
using MiniMikuDance.Motion;
using MiniMikuDance.PoseEstimation;

namespace AppCore.Tests;

public class BvhExporterTests
{
    [Fact]
    public void Export_CreatesBvhFile()
    {
        var data = new MotionData
        {
            FrameInterval = 0.1f,
            Frames = new[]
            {
                new JointData { Positions = new[]{ new Vector3(1,2,3), new Vector3(4,5,6) }, Confidences = new float[2] },
                new JointData { Positions = new[]{ new Vector3(7,8,9), new Vector3(10,11,12) }, Confidences = new float[2] }
            }
        };

        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "out.bvh");

        BvhExporter.Export(data, path);

        Assert.True(File.Exists(path));
        var text = File.ReadAllText(path);
        Assert.Contains("HIERARCHY", text);
        Assert.Contains("Frames: 2", text);
        Assert.Contains("Frame Time: 0.1", text);

        Directory.Delete(dir, true);
    }
}
