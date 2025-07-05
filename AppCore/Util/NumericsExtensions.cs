using System.Numerics;
using OpenTK.Mathematics;

namespace MiniMikuDance.Util;

public static class NumericsExtensions
{
    public static Matrix4 ToMatrix4(this Matrix4x4 m)
    {
        return new Matrix4(
            m.M11, m.M12, m.M13, m.M14,
            m.M21, m.M22, m.M23, m.M24,
            m.M31, m.M32, m.M33, m.M34,
            m.M41, m.M42, m.M43, m.M44);
    }

    public static OpenTK.Mathematics.Vector4 ToVector4(this System.Numerics.Vector4 v)
    {
        return new OpenTK.Mathematics.Vector4(v.X, v.Y, v.Z, v.W);
    }

    public static Matrix4 ToMatrix4(this Quaternion q)
    {
        var oq = new OpenTK.Mathematics.Quaternion(q.X, q.Y, q.Z, q.W);
        return Matrix4.CreateFromQuaternion(oq);
    }
}
