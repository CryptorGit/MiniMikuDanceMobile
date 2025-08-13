using System.Numerics;

namespace MiniMikuDance.IK;

public static class IkMath
{
    private const float Epsilon = 1e-6f;

    public static Quaternion LookRotation(Vector3 forward, Vector3 up)
    {
        if (forward.LengthSquared() < Epsilon || up.LengthSquared() < Epsilon)
            return Quaternion.Identity;
        forward = Vector3.Normalize(forward);
        var proj = Vector3.Dot(up, forward);
        up -= proj * forward;
        if (up.LengthSquared() < Epsilon)
            return Quaternion.Identity;
        up = Vector3.Normalize(up);
        var right = Vector3.Cross(up, forward);
        if (right.LengthSquared() < Epsilon)
            return Quaternion.Identity;
        right = Vector3.Normalize(right);
        var newUp = Vector3.Cross(forward, right);
        if (newUp.LengthSquared() < Epsilon)
            return Quaternion.Identity;
        IkDebug.LogAxes(forward, newUp, right);
        var m = new Matrix4x4(
            right.X, right.Y, right.Z, 0,
            newUp.X, newUp.Y, newUp.Z, 0,
            forward.X, forward.Y, forward.Z, 0,
            0, 0, 0, 1);
        return Quaternion.CreateFromRotationMatrix(m);
    }
}

