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
            var bone = BoneProvider?.Invoke(body);
            if (bone != null)
                body.SyncToSimulation(bone);
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
                bool follow = body.TransformType != RigidBodyTransformType.FromSimulationToBone;
                body.SyncFromSimulation(bone, follow);
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
        var halfA = a.Size * 0.5f;
        var halfB = b.Size * 0.5f;
        var rotA = Matrix4x4.CreateFromQuaternion(a.Orientation);
        var rotB = Matrix4x4.CreateFromQuaternion(b.Orientation);

        Vector3 extA = new(
            MathF.Abs(rotA.M11) * halfA.X + MathF.Abs(rotA.M12) * halfA.Y + MathF.Abs(rotA.M13) * halfA.Z,
            MathF.Abs(rotA.M21) * halfA.X + MathF.Abs(rotA.M22) * halfA.Y + MathF.Abs(rotA.M23) * halfA.Z,
            MathF.Abs(rotA.M31) * halfA.X + MathF.Abs(rotA.M32) * halfA.Y + MathF.Abs(rotA.M33) * halfA.Z);
        Vector3 extB = new(
            MathF.Abs(rotB.M11) * halfB.X + MathF.Abs(rotB.M12) * halfB.Y + MathF.Abs(rotB.M13) * halfB.Z,
            MathF.Abs(rotB.M21) * halfB.X + MathF.Abs(rotB.M22) * halfB.Y + MathF.Abs(rotB.M23) * halfB.Z,
            MathF.Abs(rotB.M31) * halfB.X + MathF.Abs(rotB.M32) * halfB.Y + MathF.Abs(rotB.M33) * halfB.Z);

        var minA = a.Position - extA;
        var maxA = a.Position + extA;
        var minB = b.Position - extB;
        var maxB = b.Position + extB;

        if (maxA.X < minB.X || minA.X > maxB.X ||
            maxA.Y < minB.Y || minA.Y > maxB.Y ||
            maxA.Z < minB.Z || minA.Z > maxB.Z)
            return;

        float overlapX = MathF.Min(maxA.X, maxB.X) - MathF.Max(minA.X, minB.X);
        float overlapY = MathF.Min(maxA.Y, maxB.Y) - MathF.Max(minA.Y, minB.Y);
        float overlapZ = MathF.Min(maxA.Z, maxB.Z) - MathF.Max(minA.Z, minB.Z);
        float penetration = overlapX;
        Vector3 normal = new(MathF.Sign(b.Position.X - a.Position.X), 0, 0);
        if (overlapY < penetration)
        {
            penetration = overlapY;
            normal = new(0, MathF.Sign(b.Position.Y - a.Position.Y), 0);
        }
        if (overlapZ < penetration)
        {
            penetration = overlapZ;
            normal = new(0, 0, MathF.Sign(b.Position.Z - a.Position.Z));
        }
        normal = Vector3.Normalize(normal);
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

    private static void ResolveCapsuleCapsule(RigidBody a, RigidBody b)
    {
        float radiusA = a.Size.X * 0.5f;
        float radiusB = b.Size.X * 0.5f;
        float halfHeightA = a.Size.Y * 0.5f;
        float halfHeightB = b.Size.Y * 0.5f;
        var axisA = Vector3.Transform(Vector3.UnitY, a.Orientation);
        var axisB = Vector3.Transform(Vector3.UnitY, b.Orientation);
        var a0 = a.Position + axisA * halfHeightA;
        var a1 = a.Position - axisA * halfHeightA;
        var b0 = b.Position + axisB * halfHeightB;
        var b1 = b.Position - axisB * halfHeightB;

        ClosestSegmentPoints(a0, a1, b0, b1, out var pA, out var pB);
        var delta = pB - pA;
        var dist = delta.Length();
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

    private static void ClosestSegmentPoints(Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2, out Vector3 c1, out Vector3 c2)
    {
        var d1 = q1 - p1;
        var d2 = q2 - p2;
        var r = p1 - p2;
        float a = Vector3.Dot(d1, d1);
        float e = Vector3.Dot(d2, d2);
        float f = Vector3.Dot(d2, r);
        float s, t;

        if (a <= float.Epsilon && e <= float.Epsilon)
        {
            s = t = 0f;
        }
        else if (a <= float.Epsilon)
        {
            s = 0f;
            t = Math.Clamp(f / e, 0f, 1f);
        }
        else
        {
            float c = Vector3.Dot(d1, r);
            if (e <= float.Epsilon)
            {
                t = 0f;
                s = Math.Clamp(-c / a, 0f, 1f);
            }
            else
            {
                float b = Vector3.Dot(d1, d2);
                float denom = a * e - b * b;
                if (denom != 0f)
                    s = Math.Clamp((b * f - c * e) / denom, 0f, 1f);
                else
                    s = 0f;
                t = (b * s + f) / e;
                if (t < 0f)
                {
                    t = 0f;
                    s = Math.Clamp(-c / a, 0f, 1f);
                }
                else if (t > 1f)
                {
                    t = 1f;
                    s = Math.Clamp((b - c) / a, 0f, 1f);
                }
            }
        }
        c1 = p1 + d1 * s;
        c2 = p2 + d2 * t;
    }
}
