namespace MiniMikuDance.Physics;

using System;
using System.Collections.Generic;
using System.Numerics;
using MiniMikuDance.Import;

/// <summary>
/// 物理シミュレーション世界。
/// </summary>
public class PhysicsWorld
{
    private readonly List<RigidBody> _rigidBodies = new();
    private readonly List<Joint> _joints = new();

    public Vector3 Gravity { get; set; } = new(0f, 0f, -9.8f);

    public Func<RigidBody, BoneData>? BoneProvider { get; set; }
    public Action<RigidBody>? BoneUpdateHook { get; set; }

    public IReadOnlyList<RigidBody> RigidBodies => _rigidBodies;
    public IReadOnlyList<Joint> Joints => _joints;

    public RigidBody CreateRigidBody(RigidBodyData data)
    {
        switch (data.TransformType)
        {
            case RigidBodyTransformType.FromBoneToSimulation:
            case RigidBodyTransformType.FromSimulationToBone:
            case RigidBodyTransformType.FromBoneOrientationAndSimulationToBone:
            case RigidBodyTransformType.FromBoneTranslationAndSimulationToBone:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(data.TransformType), data.TransformType, "Unknown transform type");
        }

        var body = new RigidBody(
            data.Name,
            data.BoneIndex,
            data.Mass,
            data.Shape,
            data.Size,
            data.Origin,
            data.Orientation,
            data.LinearDamping,
            data.AngularDamping,
            data.Restitution,
            data.Friction,
            data.TransformType,
            data.IsBoneRelative,
            data.IsMorph,
            data.Torque,
            data.Type,
            data.Gravity,
            data.CollisionGroup,
            data.CollisionMask);
        _rigidBodies.Add(body);
        return body;
    }

    public Joint CreateJoint(JointData data)
    {
        var bodyA = data.RigidBodyA >= 0 && data.RigidBodyA < _rigidBodies.Count
            ? _rigidBodies[data.RigidBodyA]
            : null;
        var bodyB = data.RigidBodyB >= 0 && data.RigidBodyB < _rigidBodies.Count
            ? _rigidBodies[data.RigidBodyB]
            : null;
        var joint = new Joint(
            data.Name,
            bodyA,
            bodyB,
            data.Origin,
            data.Orientation,
            data.LinearLowerLimit,
            data.LinearUpperLimit,
            data.AngularLowerLimit,
            data.AngularUpperLimit,
            data.LinearStiffness,
            data.AngularStiffness);
        _joints.Add(joint);
        return joint;
    }

    public void Step(float deltaTime)
    {
        // Forces
        foreach (var body in _rigidBodies)
        {
            body.ApplyAllForces();
        }

        // ToSimulation
        foreach (var body in _rigidBodies)
        {
            if (body.TransformType == RigidBodyTransformType.FromBoneToSimulation ||
                body.Type == RigidBodyType.Kinematic)
            {
                var bone = BoneProvider?.Invoke(body);
                if (bone != null)
                    body.SyncToSimulation(bone);
            }
        }

        // Integrate and solve forces
        foreach (var body in _rigidBodies)
        {
            if (!body.IsActive)
                continue;
            body.ApplyGravity(Gravity, deltaTime);
            body.Integrate(deltaTime);
        }

        // Collision resolution
        for (int i = 0; i < _rigidBodies.Count; i++)
        {
            if (!_rigidBodies[i].IsActive)
                continue;
            for (int j = i + 1; j < _rigidBodies.Count; j++)
            {
                if (!_rigidBodies[j].IsActive)
                    continue;
                ResolveCollision(_rigidBodies[i], _rigidBodies[j]);
            }
        }

        // Joint solving
        foreach (var joint in _joints)
        {
            joint.Solve();
        }

        // FromSimulation and bone update
        foreach (var body in _rigidBodies)
        {
            if (!body.IsActive)
                continue;
            if (body.TransformType == RigidBodyTransformType.FromBoneToSimulation ||
                body.Type == RigidBodyType.Kinematic)
                continue;
            var bone = BoneProvider?.Invoke(body);
            if (bone != null)
            {
                body.SyncFromSimulation(bone, true);
                BoneUpdateHook?.Invoke(body);
            }
        }
    }

    private void ResolveCollision(RigidBody a, RigidBody b)
    {
        if (!ShouldCollide(a, b))
            return;

        if (a.Shape == RigidBodyShape.Sphere && b.Shape == RigidBodyShape.Sphere)
        {
            ResolveSphereSphere(a, b);
        }
        else if (a.Shape == RigidBodyShape.Box && b.Shape == RigidBodyShape.Box)
        {
            ResolveBoxBox(a, b);
        }
        else if (a.Shape == RigidBodyShape.Capsule && b.Shape == RigidBodyShape.Capsule)
        {
            ResolveCapsuleCapsule(a, b);
        }
        else
        {
            ResolveSphereSphere(a, b);
        }
    }

    private static bool ShouldCollide(RigidBody a, RigidBody b)
    {
        return (a.CollisionGroup & b.CollisionMask) != 0 &&
               (b.CollisionGroup & a.CollisionMask) != 0;
    }

    private static void ResolveSphereSphere(RigidBody a, RigidBody b)
    {
        var delta = b.Position - a.Position;
        var dist = delta.Length();
        var radiusA = a.BoundingRadius;
        var radiusB = b.BoundingRadius;
        var penetration = radiusA + radiusB - dist;
        if (penetration <= 0f || dist == 0f)
            return;
        var normal = delta / dist;
        var totalMass = a.Mass + b.Mass;
        if (totalMass <= 0f)
            return;
        a.Position -= normal * (penetration * (b.Mass / totalMass));
        b.Position += normal * (penetration * (a.Mass / totalMass));

        var relative = Vector3.Dot(b.Velocity - a.Velocity, normal);
        if (relative >= 0f)
            return;

        var restitution = MathF.Min(a.Restitution, b.Restitution);
        var impulse = -(1f + restitution) * relative;
        var invMassA = a.Mass > 0f ? 1f / a.Mass : 0f;
        var invMassB = b.Mass > 0f ? 1f / b.Mass : 0f;
        var impulseVec = impulse / (invMassA + invMassB) * normal;
        a.Velocity -= impulseVec * invMassA;
        b.Velocity += impulseVec * invMassB;

        var friction = MathF.Min(a.Friction, b.Friction);
        var tangentVel = (b.Velocity - a.Velocity) - relative * normal;
        if (tangentVel.LengthSquared() > 0f)
        {
            var tangent = Vector3.Normalize(tangentVel);
            var jt = -Vector3.Dot(b.Velocity - a.Velocity, tangent);
            jt /= (invMassA + invMassB);
            var frictionVec = jt * friction * tangent;
            a.Velocity -= frictionVec * invMassA;
            b.Velocity += frictionVec * invMassB;
        }
    }

    private static void ResolveBoxBox(RigidBody a, RigidBody b)
    {
        // TODO: Box の衝突判定を実装
        ResolveSphereSphere(a, b);
    }

    private static void ResolveCapsuleCapsule(RigidBody a, RigidBody b)
    {
        // TODO: Capsule の衝突判定を実装
        ResolveSphereSphere(a, b);
    }
}
