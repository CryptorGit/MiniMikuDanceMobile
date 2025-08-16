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
    public Quaternion OrientationQuaternion { get; }
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
        OrientationQuaternion = Quaternion.CreateFromYawPitchRoll(orientation.Y, orientation.X, orientation.Z);
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

        // 線形拘束（スプリング）
        var invMassA = BodyA.Mass > 0f ? 1f / BodyA.Mass : 0f;
        var invMassB = BodyB.Mass > 0f ? 1f / BodyB.Mass : 0f;
        var invSum = invMassA + invMassB;
        var diff = BodyB.Position - BodyA.Position - Origin;
        var clamped = Vector3.Clamp(diff, LinearLowerLimit, LinearUpperLimit);
        var error = diff - clamped;
        var force = error * LinearStiffness;
        var posDelta = force * 0.5f;
        BodyA.Position += posDelta;
        BodyB.Position -= posDelta;
        if (invSum > 0f)
        {
            BodyA.Velocity += force * (invMassA / invSum);
            BodyB.Velocity -= force * (invMassB / invSum);
        }

        // 角度拘束（スプリング）
        var relRot = Quaternion.Inverse(BodyA.Orientation) * BodyB.Orientation;
        relRot = Quaternion.Inverse(OrientationQuaternion) * relRot;
        var relEuler = relRot.ToEulerRadians();
        var angClamped = Vector3.Clamp(relEuler, AngularLowerLimit, AngularUpperLimit);
        var angError = relEuler - angClamped;
        var angForce = angError * AngularStiffness;
        var angDelta = angForce * 0.5f;
        var eulerA = BodyA.Orientation.ToEulerRadians() + angDelta;
        var eulerB = BodyB.Orientation.ToEulerRadians() - angDelta;
        BodyA.Orientation = Quaternion.CreateFromYawPitchRoll(eulerA.Y, eulerA.X, eulerA.Z);
        BodyB.Orientation = Quaternion.CreateFromYawPitchRoll(eulerB.Y, eulerB.X, eulerB.Z);
        if (invSum > 0f)
        {
            BodyA.AngularVelocity += angForce * (invMassA / invSum);
            BodyB.AngularVelocity -= angForce * (invMassB / invSum);
        }
    }
}

