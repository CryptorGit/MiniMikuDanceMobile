using System.Numerics;

namespace MiniMikuDance.Util;

public static class IkMath
{
    private const float Epsilon = 1e-6f;

    public static bool SafeNormalize(ref Vector3 v)
    {
        var lenSq = v.LengthSquared();
        if (lenSq > Epsilon)
        {
            v /= MathF.Sqrt(lenSq);
            return true;
        }
        v = Vector3.Zero;
        return false;
    }

    public static bool KeepLength(ref Vector3 to, ref Vector3 from, float length)
    {
        var diff = to - from;
        var len = diff.Length();
        if (len > Epsilon)
        {
            diff *= length / len;
            to = from + diff;
            return true;
        }
        return false;
    }
}
