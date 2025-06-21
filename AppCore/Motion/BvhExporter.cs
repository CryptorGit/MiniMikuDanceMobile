using System.Globalization;
using System.Text;

namespace MiniMikuDance.Motion;

public static class BvhExporter
{
    public static void Export(MotionData data, string path)
    {
        if (data.Frames.Length == 0)
            throw new InvalidOperationException("No frames to export");

        int jointCount = data.Frames[0].Positions.Length;

        using var writer = new StreamWriter(path);
        writer.WriteLine("HIERARCHY");
        writer.WriteLine("ROOT root");
        writer.WriteLine("{");
        writer.WriteLine("    OFFSET 0 0 0");
        writer.WriteLine("    CHANNELS 3 Xposition Yposition Zposition");
        for (int i = 0; i < jointCount; i++)
        {
            writer.WriteLine($"    JOINT J{i}");
            writer.WriteLine("    {");
            writer.WriteLine("        OFFSET 0 0 0");
            writer.WriteLine("        CHANNELS 3 Xposition Yposition Zposition");
            writer.WriteLine("        End Site");
            writer.WriteLine("        {");
            writer.WriteLine("            OFFSET 0 0 0");
            writer.WriteLine("        }");
            writer.WriteLine("    }");
        }
        writer.WriteLine("}");
        writer.WriteLine("MOTION");
        writer.WriteLine($"Frames: {data.Frames.Length}");
        writer.WriteLine($"Frame Time: {data.FrameInterval.ToString(CultureInfo.InvariantCulture)}");
        foreach (var frame in data.Frames)
        {
            var line = new StringBuilder();
            if (frame.Positions.Length > 0)
            {
                var rootPos = frame.Positions[0];
                line.Append(rootPos.X.ToString(CultureInfo.InvariantCulture));
                line.Append(' ');
                line.Append(rootPos.Y.ToString(CultureInfo.InvariantCulture));
                line.Append(' ');
                line.Append(rootPos.Z.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                line.Append("0 0 0");
            }
            for (int j = 1; j < frame.Positions.Length; j++)
            {
                var pos = frame.Positions[j];
                line.Append(' ');
                line.Append(pos.X.ToString(CultureInfo.InvariantCulture));
                line.Append(' ');
                line.Append(pos.Y.ToString(CultureInfo.InvariantCulture));
                line.Append(' ');
                line.Append(pos.Z.ToString(CultureInfo.InvariantCulture));
            }
            writer.WriteLine(line.ToString());
        }
    }
}
