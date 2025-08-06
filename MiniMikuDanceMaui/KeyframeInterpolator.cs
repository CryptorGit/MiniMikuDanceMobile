using System.Collections.Generic;
using OpenTK.Mathematics;

namespace MiniMikuDanceMaui;

public static class KeyframeInterpolator
{
    public static void Recalculate(
        string boneName,
        Dictionary<string, List<int>> keyframes,
        Dictionary<string, Dictionary<int, Vector3>> translations,
        Dictionary<string, Dictionary<int, Vector3>> rotations,
        Dictionary<string, HashSet<int>> autoKeyframes)
    {
        if (!autoKeyframes.TryGetValue(boneName, out var autos))
        {
            autos = new HashSet<int>();
            autoKeyframes[boneName] = autos;
        }

        if (translations.TryGetValue(boneName, out var tdict))
        {
            foreach (var f in autos)
                tdict.Remove(f);
        }
        if (rotations.TryGetValue(boneName, out var rdict))
        {
            foreach (var f in autos)
                rdict.Remove(f);
        }
        autos.Clear();

        if (!keyframes.TryGetValue(boneName, out var list) || list.Count < 2)
            return;

        list.Sort();
        for (int i = 0; i < list.Count - 1; i++)
        {
            int start = list[i];
            int end = list[i + 1];
            if (end - start <= 1)
                continue;

            var t1 = translations[boneName][start];
            var t2 = translations[boneName][end];
            var r1 = rotations[boneName][start];
            var r2 = rotations[boneName][end];
            for (int f = start + 1; f < end; f++)
            {
                float ratio = (float)(f - start) / (end - start);
                var t = Vector3.Lerp(t1, t2, ratio);
                var r = Vector3.Lerp(r1, r2, ratio);
                tdict[f] = t;
                rdict[f] = r;
                autos.Add(f);
            }
        }
    }
}
