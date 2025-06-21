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
        writer.WriteLine("}");
        writer.WriteLine("MOTION");
        writer.WriteLine($"Frames: {motion.Frames.Length}");
        writer.WriteLine($"Frame Time: {motion.FrameInterval}");
        for (int i = 0; i < motion.Frames.Length; i++)
        {
            writer.WriteLine("0 0 0 0 0 0");
        }
    }
}
