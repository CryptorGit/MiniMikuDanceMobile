using System.Globalization;
using System.Text;
using System.IO;

namespace MiniMikuDance.Motion
{
    public static class BvhExporter
    {
        public static void Export(MotionData motion, string path)
        {
            if (motion.Frames.Length == 0)
                throw new InvalidOperationException("No frames to export");

            int jointCount = motion.Frames[0].Positions.Length;

            using var writer = new StreamWriter(path);
            // HIERARCHY
            writer.WriteLine("HIERARCHY");
            writer.WriteLine("ROOT Hip");
            writer.WriteLine("{");
            writer.WriteLine("    OFFSET 0 0 0");
            // ルートは Xposition Yposition Zposition Zrotation Xrotation Yrotation
            writer.WriteLine("    CHANNELS 6 Xposition Yposition Zposition Zrotation Xrotation Yrotation");

            // 子ジョイントは回転のみ (例として jointCount-1 個)
            for (int i = 1; i < jointCount; i++)
            {
                writer.WriteLine($"    JOINT J{i}");
                writer.WriteLine("    {");
                writer.WriteLine("        OFFSET 0 0 0");
                writer.WriteLine("        CHANNELS 3 Xrotation Yrotation Zrotation");
                writer.WriteLine("        End Site");
                writer.WriteLine("        {");
                writer.WriteLine("            OFFSET 0 0 0");
                writer.WriteLine("        }");
                writer.WriteLine("    }");
            }
            writer.WriteLine("}");

            // MOTION
            writer.WriteLine("MOTION");
            writer.WriteLine($"Frames: {motion.Frames.Length}");
            writer.WriteLine($"Frame Time: {motion.FrameInterval.ToString(CultureInfo.InvariantCulture)}");

            // 各フレームの出力
            foreach (var frame in motion.Frames)
            {
                var sb = new StringBuilder();

                // ルート位置
                var rootPos = frame.Positions[0];
                sb.Append(rootPos.X.ToString(CultureInfo.InvariantCulture)).Append(' ')
                  .Append(rootPos.Y.ToString(CultureInfo.InvariantCulture)).Append(' ')
                  .Append(rootPos.Z.ToString(CultureInfo.InvariantCulture)).Append(' ');

                // ルート回転 (仮に frame.Rotations がある場合)
                var rootRot = frame.Rotations[0];
                sb.Append(rootRot.Z.ToString(CultureInfo.InvariantCulture)).Append(' ')
                  .Append(rootRot.X.ToString(CultureInfo.InvariantCulture)).Append(' ')
                  .Append(rootRot.Y.ToString(CultureInfo.InvariantCulture));

                // 子ジョイント回転
                for (int j = 1; j < jointCount; j++)
                {
                    var rot = frame.Rotations[j];
                    sb.Append(' ')
                      .Append(rot.X.ToString(CultureInfo.InvariantCulture)).Append(' ')
                      .Append(rot.Y.ToString(CultureInfo.InvariantCulture)).Append(' ')
                      .Append(rot.Z.ToString(CultureInfo.InvariantCulture));
                }

                writer.WriteLine(sb.ToString());
            }
        }
    }
}

