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

        // 線形拘束
        var diff = BodyB.Position - BodyA.Position - Origin;
        var clamped = Vector3.Min(Vector3.Max(diff, LinearLowerLimit), LinearUpperLimit);
        var correction = diff - clamped;
        var linStiff = LinearStiffness * 0.5f;
        var corr = Vector3.Multiply(correction, linStiff);
        BodyA.Position += corr;
        BodyB.Position -= corr;

        // 角度拘束
        var eulerA = BodyA.Orientation.ToEulerRadians();
        var eulerB = BodyB.Orientation.ToEulerRadians();
        var angDiff = eulerB - eulerA - Orientation;
        var angClamped = Vector3.Min(Vector3.Max(angDiff, AngularLowerLimit), AngularUpperLimit);
        var angCorrection = angDiff - angClamped;
        var angStiff = AngularStiffness * 0.5f;
        var angCorr = Vector3.Multiply(angCorrection, angStiff);
        eulerA += angCorr;
        eulerB -= angCorr;
        BodyA.Orientation = Quaternion.CreateFromYawPitchRoll(eulerA.Y, eulerA.X, eulerA.Z);
        BodyB.Orientation = Quaternion.CreateFromYawPitchRoll(eulerB.Y, eulerB.X, eulerB.Z);

        BodyA.Velocity = Vector3.Zero;
        BodyB.Velocity = Vector3.Zero;
    }
}

