namespace MiniMikuDance.Physics;

using System.Numerics;
using MiniMikuDance.Util;

/// <summary>
/// ジョイントを表すクラス。
/// </summary>
public class Joint
{
    public string Name { get; }
    public RigidBody? BodyA { get; }
    public RigidBody? BodyB { get; }

    public Vector3 Origin { get; }
    public Vector3 Orientation { get; }
    public Vector3 LinearLowerLimit { get; }
    public Vector3 LinearUpperLimit { get; }
    public Vector3 AngularLowerLimit { get; }
    public Vector3 AngularUpperLimit { get; }
    public Vector3 LinearStiffness { get; }
    public Vector3 AngularStiffness { get; }

    internal Joint(string name, RigidBody? bodyA, RigidBody? bodyB,
        Vector3 origin, Vector3 orientation,
        Vector3 linearLowerLimit, Vector3 linearUpperLimit,
        Vector3 angularLowerLimit, Vector3 angularUpperLimit,
        Vector3 linearStiffness, Vector3 angularStiffness)
    {
        Name = name;
        BodyA = bodyA;
        BodyB = bodyB;
        Origin = origin;
        Orientation = orientation;
        LinearLowerLimit = linearLowerLimit;
        LinearUpperLimit = linearUpperLimit;
        AngularLowerLimit = angularLowerLimit;
        AngularUpperLimit = angularUpperLimit;
        LinearStiffness = linearStiffness;
        AngularStiffness = angularStiffness;
    }

    internal void Solve()
    {
        if (BodyA is null || BodyB is null)
            return;

        // 線形拘束（スプリングと制限）
        var diff = BodyB.Position - BodyA.Position - Origin;
        var clamped = Vector3.Min(Vector3.Max(diff, LinearLowerLimit), LinearUpperLimit);
        var error = diff - clamped;
        var corr = error * LinearStiffness;
        BodyA.Position += corr * 0.5f;
        BodyB.Position -= corr * 0.5f;
        BodyA.Velocity += corr;
        BodyB.Velocity -= corr;

        // 角度拘束（スプリングと角度制限）
        var rel = Quaternion.Inverse(BodyA.Orientation) * BodyB.Orientation;
        var ang = rel.ToEulerRadians() - Orientation;
        var angClamped = Vector3.Min(Vector3.Max(ang, AngularLowerLimit), AngularUpperLimit);
        var angError = ang - angClamped;
        var angCorr = angError * AngularStiffness;
        var qCorr = Quaternion.CreateFromYawPitchRoll(angCorr.Y, angCorr.X, angCorr.Z);
        BodyA.Orientation = Quaternion.Normalize(qCorr * BodyA.Orientation);
        BodyB.Orientation = Quaternion.Normalize(Quaternion.Inverse(qCorr) * BodyB.Orientation);
        BodyA.AngularVelocity += angCorr;
        BodyB.AngularVelocity -= angCorr;
    }
}

