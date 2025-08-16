using System.Numerics;

namespace MiniMikuDance.Util;

public static class NumericsExtensions
{
    public static Matrix4x4 ToMatrix4(this Matrix4x4 m)
    {
        return m;
    }

    public static Vector4 ToVector4(this Vector4 v)
    {
        return v;
    }

    public static Matrix4x4 ToMatrix4(this Quaternion q)
    {
        return Matrix4x4.CreateFromQuaternion(q);
    }

    // Z→X→Y 順を使用する
    public static Quaternion FromEulerDegrees(this Vector3 degrees)
    {
        var rad = degrees * (MathF.PI / 180f);
        var qz = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, rad.Z);
        var qx = Quaternion.CreateFromAxisAngle(Vector3.UnitX, rad.X);
        var qy = Quaternion.CreateFromAxisAngle(Vector3.UnitY, rad.Y);
        return qy * qx * qz;
    }
}
