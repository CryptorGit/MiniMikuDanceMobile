using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;

namespace MiniMikuDance.Import;

public static class BmxParser
{
    public static Dictionary<string, Vector3> Parse(string path, out Vector3? globalGravity)
    {
        var result = new Dictionary<string, Vector3>(StringComparer.OrdinalIgnoreCase);
        globalGravity = null;
        foreach (var line in File.ReadLines(path))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                continue;
            var parts = trimmed.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4)
                continue;
            if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
                !float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var y) ||
                !float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
                continue;
            if (parts[0].Equals("global", StringComparison.OrdinalIgnoreCase) ||
                parts[0].Equals("gravity", StringComparison.OrdinalIgnoreCase))
            {
                globalGravity = new Vector3(x, y, z);
            }
            else
            {
                result[parts[0]] = new Vector3(x, y, z);
            }
        }
        return result;
    }
}
