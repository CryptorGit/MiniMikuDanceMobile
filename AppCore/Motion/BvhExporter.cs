using System.IO;

namespace MiniMikuDance.Motion;

public static class BvhExporter
{
    public static void Export(MotionData motion, string path)
    {
        using var writer = new StreamWriter(path);

        writer.WriteLine("HIERARCHY");
        writer.WriteLine("ROOT Hip");
        writer.WriteLine("{");
        writer.WriteLine("  OFFSET 0 0 0");
        writer.WriteLine("  CHANNELS 6 Xposition Yposition Zposition Zrotation Xrotation Yrotation");
        writer.WriteLine("  End Site");
        writer.WriteLine("  {");
        writer.WriteLine("    OFFSET 0 0 0");
        writer.WriteLine("  }");
        writer.WriteLine("}");

        writer.WriteLine("MOTION");
        writer.WriteLine($"Frames: {motion.Frames.Length}");
        writer.WriteLine($"Frame Time: {motion.FrameInterval}");

        foreach (var frame in motion.Frames)
        {
            var pos = frame.Positions.Length > 0 ? frame.Positions[0] : System.Numerics.Vector3.Zero;
            writer.WriteLine($"{pos.X} {pos.Y} {pos.Z} 0 0 0");
        }
    }
}
