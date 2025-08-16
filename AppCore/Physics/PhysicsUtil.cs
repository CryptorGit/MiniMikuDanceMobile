namespace MiniMikuDance.Physics;

using System.Numerics;

internal static class PhysicsUtil
{
    public static void ExtractTransform(float[] matrix, out Vector3 translation, out Quaternion rotation)
    {
        var m = new Matrix4x4(
            matrix[0], matrix[1], matrix[2], matrix[3],
            matrix[4], matrix[5], matrix[6], matrix[7],
            matrix[8], matrix[9], matrix[10], matrix[11],
            matrix[12], matrix[13], matrix[14], matrix[15]);
        Matrix4x4.Decompose(m, out _, out rotation, out translation);
    }
}
