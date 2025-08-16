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
            if (!body.IsActive)
                continue;
            var bone = BoneProvider?.Invoke(body);
            if (bone == null)
                continue;
            if (body.Type == RigidBodyType.Kinematic ||
                body.TransformType == RigidBodyTransformType.FromBoneToSimulation ||
                body.TransformType == RigidBodyTransformType.FromBoneOrientationAndSimulationToBone ||
                body.TransformType == RigidBodyTransformType.FromBoneTranslationAndSimulationToBone)
            {
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
            if (bone == null)
                continue;
            var followBone = body.TransformType != RigidBodyTransformType.FromSimulationToBone;
            body.SyncFromSimulation(bone, followBone);
            BoneUpdateHook?.Invoke(body);
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
        var axes = new Vector3[6];
        axes[0] = Vector3.Transform(Vector3.UnitX, a.Orientation);
        axes[1] = Vector3.Transform(Vector3.UnitY, a.Orientation);
        axes[2] = Vector3.Transform(Vector3.UnitZ, a.Orientation);
        axes[3] = Vector3.Transform(Vector3.UnitX, b.Orientation);
        axes[4] = Vector3.Transform(Vector3.UnitY, b.Orientation);
        axes[5] = Vector3.Transform(Vector3.UnitZ, b.Orientation);

        float minPenetration = float.MaxValue;
        Vector3 bestAxis = Vector3.Zero;
        for (int i = 0; i < axes.Length; i++)
        {
            var axis = Vector3.Normalize(axes[i]);
            if (axis.LengthSquared() < 1e-6f)
                continue;
            GetInterval(a, axis, out var minA, out var maxA);
            GetInterval(b, axis, out var minB, out var maxB);
            var overlap = MathF.Min(maxA, maxB) - MathF.Max(minA, minB);
            if (overlap <= 0f)
                return;
            if (overlap < minPenetration)
            {
                minPenetration = overlap;
                bestAxis = axis;
            }
        }

        var normal = Vector3.Normalize(bestAxis) * MathF.Sign(Vector3.Dot(b.Position - a.Position, bestAxis));
        var totalMass = a.Mass + b.Mass;
        if (totalMass <= 0f)
            return;
        a.Position -= normal * (minPenetration * (b.Mass / totalMass));
        b.Position += normal * (minPenetration * (a.Mass / totalMass));

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

    private static void ResolveCapsuleCapsule(RigidBody a, RigidBody b)
    {
        GetCapsulePoints(a, out var a1, out var a2, out var ra);
        GetCapsulePoints(b, out var b1, out var b2, out var rb);
        ClosestPtSegmentSegment(a1, a2, b1, b2, out var pA, out var pB);
        var delta = pB - pA;
        var dist = delta.Length();
        var penetration = ra + rb - dist;
        if (penetration <= 0f)
            return;
        var normal = dist > 0f ? delta / dist : Vector3.UnitY;
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

    private static void GetInterval(RigidBody box, Vector3 axis, out float min, out float max)
    {
        var center = box.Position;
        var half = box.Size * 0.5f;
        var x = Vector3.Transform(Vector3.UnitX, box.Orientation);
        var y = Vector3.Transform(Vector3.UnitY, box.Orientation);
        var z = Vector3.Transform(Vector3.UnitZ, box.Orientation);
        float c = Vector3.Dot(center, axis);
        float r =
            MathF.Abs(Vector3.Dot(x * half.X, axis)) +
            MathF.Abs(Vector3.Dot(y * half.Y, axis)) +
            MathF.Abs(Vector3.Dot(z * half.Z, axis));
        min = c - r;
        max = c + r;
    }

    private static void GetCapsulePoints(RigidBody body, out Vector3 p1, out Vector3 p2, out float radius)
    {
        radius = body.Size.X * 0.5f;
        var halfHeight = MathF.Max(0f, body.Size.Y * 0.5f - radius);
        var axis = Vector3.Transform(Vector3.UnitY, body.Orientation);
        p1 = body.Position + axis * halfHeight;
        p2 = body.Position - axis * halfHeight;
    }

    private static void ClosestPtSegmentSegment(Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2, out Vector3 c1, out Vector3 c2)
    {
        var d1 = q1 - p1;
        var d2 = q2 - p2;
        var r = p1 - p2;
        var a = Vector3.Dot(d1, d1);
        var e = Vector3.Dot(d2, d2);
        var f = Vector3.Dot(d2, r);
        float s, t;
        if (a <= float.Epsilon && e <= float.Epsilon)
        {
            s = t = 0f;
        }
        else if (a <= float.Epsilon)
        {
            s = 0f;
            t = MathF.Clamp(f / e, 0f, 1f);
        }
        else
        {
            var c = Vector3.Dot(d1, r);
            if (e <= float.Epsilon)
            {
                t = 0f;
                s = MathF.Clamp(-c / a, 0f, 1f);
            }
            else
            {
                var b = Vector3.Dot(d1, d2);
                var denom = a * e - b * b;
                s = denom != 0f ? MathF.Clamp((b * f - c * e) / denom, 0f, 1f) : 0f;
                t = (b * s + f) / e;
                if (t < 0f)
                {
                    t = 0f;
                    s = MathF.Clamp(-c / a, 0f, 1f);
                }
                else if (t > 1f)
                {
                    t = 1f;
                    s = MathF.Clamp((b - c) / a, 0f, 1f);
                }
            }
        }
        c1 = p1 + d1 * s;
        c2 = p2 + d2 * t;
    }
}
