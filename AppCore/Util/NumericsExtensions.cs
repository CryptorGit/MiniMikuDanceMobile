using System.Numerics;
using OpenTK.Mathematics;

namespace MiniMikuDance.Util;

/// <summary>
/// 数値型と OpenTK 型の相互変換ヘルパー。
/// 
/// 本アプリケーションではモデル読込時に Z 軸を反転し、右手系で統一しています。
/// OpenGL ビュー空間は「前方 = -Z」のため、必要に応じて Z 軸を再反転できるよう
/// オプションを用意します。
/// </summary>
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

    public static OpenTK.Mathematics.Vector3 ToOpenTK(this System.Numerics.Vector3 v, bool flipZ = false)
    {
        return new OpenTK.Mathematics.Vector3(v.X, v.Y, flipZ ? -v.Z : v.Z);
    }

    public static System.Numerics.Vector3 ToNumerics(this OpenTK.Mathematics.Vector3 v, bool flipZ = false)
    {
        return new System.Numerics.Vector3(v.X, v.Y, flipZ ? -v.Z : v.Z);
    }
}
