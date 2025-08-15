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

    // Z→X→Y 順を使用する
    public static System.Numerics.Quaternion FromEulerDegrees(this System.Numerics.Vector3 degrees)
    {
        var rad = degrees * (MathF.PI / 180f);
        var qz = System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.UnitZ, rad.Z);
        var qx = System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.UnitX, rad.X);
        var qy = System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.UnitY, rad.Y);
        return qy * qx * qz;
    }

    public static System.Numerics.Vector3 ToEulerRadians(this System.Numerics.Quaternion q)
    {
        // 標準的なロール(X)、ピッチ(Y)、ヨー(Z)の順で計算する
        float sinr_cosp = 2f * (q.W * q.X + q.Y * q.Z);
        float cosr_cosp = 1f - 2f * (q.X * q.X + q.Y * q.Y);
        float roll = MathF.Atan2(sinr_cosp, cosr_cosp);

        float sinp = 2f * (q.W * q.Y - q.Z * q.X);
        float pitch = MathF.Abs(sinp) >= 1f ? MathF.CopySign(MathF.PI / 2f, sinp) : MathF.Asin(sinp);

        float siny_cosp = 2f * (q.W * q.Z + q.X * q.Y);
        float cosy_cosp = 1f - 2f * (q.Y * q.Y + q.Z * q.Z);
        float yaw = MathF.Atan2(siny_cosp, cosy_cosp);

        return new System.Numerics.Vector3(roll, pitch, yaw);
    }

    public static System.Numerics.Vector3 ToEulerDegrees(this System.Numerics.Quaternion q)
    {
        return ToEulerRadians(q) * (180f / MathF.PI);
    }

    public static OpenTK.Mathematics.Vector3 ToOpenTK(this System.Numerics.Vector3 v)
    {
        return new OpenTK.Mathematics.Vector3(v.X, v.Y, v.Z);
    }

    public static System.Numerics.Vector3 ToNumerics(this OpenTK.Mathematics.Vector3 v)
    {
        return new System.Numerics.Vector3(v.X, v.Y, v.Z);
    }
}
