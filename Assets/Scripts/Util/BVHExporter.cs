using System.IO;
using UnityEngine;

/// <summary>
/// Simple exporter that writes MotionData to a minimal BVH file.
/// Only the root joint is recorded as a demonstration.
/// </summary>
public static class BVHExporter
{
    public static void Export(MotionData data, string path)
    {
        if (data == null || data.boneCurves.Count == 0)
        {
            Debug.LogWarning("BVHExporter.Export: no motion data");
            return;
        }

        if (!data.boneCurves.TryGetValue("Joint0", out var root))
        {
            Debug.LogWarning("BVHExporter.Export: root joint missing");
            return;
        }

        try
        {
            using (var writer = new StreamWriter(path))
            {
                writer.WriteLine("HIERARCHY");
                writer.WriteLine("ROOT Hips");
                writer.WriteLine("{");
                writer.WriteLine("\tOFFSET 0 0 0");
                writer.WriteLine("\tCHANNELS 6 Xposition Yposition Zposition Xrotation Yrotation Zrotation");
                writer.WriteLine("\tEnd Site");
                writer.WriteLine("\t{");
                writer.WriteLine("\t\tOFFSET 0 1 0");
                writer.WriteLine("\t}");
                writer.WriteLine("}");
                writer.WriteLine("MOTION");
                writer.WriteLine($"Frames: {root.positions.Length}");
                writer.WriteLine($"Frame Time: {data.frameInterval}");

                for (int i = 0; i < root.positions.Length; i++)
                {
                    Vector3 p = root.positions[i];
                    Vector3 euler = root.rotations != null && root.rotations.Length > i
                        ? root.rotations[i].eulerAngles
                        : Vector3.zero;
                    writer.WriteLine($"{p.x} {p.y} {p.z} {euler.x} {euler.y} {euler.z}");
                }
            }
            Debug.Log($"BVHExporter.Export: saved {path}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"BVHExporter.Export: {ex}");
        }
    }
}
