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

    public static Matrix4 ToMatrix4(this System.Numerics.Quaternion q)
    {
        var oq = new OpenTK.Mathematics.Quaternion(q.X, q.Y, q.Z, q.W);
        return Matrix4.CreateFromQuaternion(oq);
    }

    public static System.Numerics.Quaternion FromEulerDegrees(this System.Numerics.Vector3 degrees)
    {
        var rad = degrees * (MathF.PI / 180f);
        return System.Numerics.Quaternion.CreateFromYawPitchRoll(rad.Y, rad.X, rad.Z);
    }

    public static OpenTK.Mathematics.Vector3 ToOpenTK(this System.Numerics.Vector3 v)
    {
        return new OpenTK.Mathematics.Vector3(v.X, v.Y, v.Z);
    }

    public static System.Numerics.Vector3 ToNumerics(this OpenTK.Mathematics.Vector3 v)
    {
        return new System.Numerics.Vector3(v.X, v.Y, v.Z);
    }

    public static System.Numerics.Vector3 ToEulerDegrees(this System.Numerics.Quaternion q)
    {
        var m = System.Numerics.Matrix4x4.CreateFromQuaternion(q);
        float sy = -m.M31;
        float cy = MathF.Sqrt(1 - sy * sy);
        float x, y, z;
        if (cy > 1e-6f)
        {
            x = MathF.Atan2(m.M32, m.M33);
            y = MathF.Asin(sy);
            z = MathF.Atan2(m.M21, m.M11);
        }
        else
        {
            x = MathF.Atan2(-m.M23, m.M22);
            y = MathF.Asin(sy);
            z = 0;
        }
        const float rad2deg = 180f / MathF.PI;
        return new System.Numerics.Vector3(x * rad2deg, y * rad2deg, z * rad2deg);
    }
}
