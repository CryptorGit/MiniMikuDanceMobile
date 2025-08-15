namespace MiniMikuDance.Physics;

using System;
using System.Numerics;

/// <summary>
/// 剛体を表すクラス。
/// </summary>
public class RigidBody
{
    public string Name { get; }
    public int BoneIndex { get; }
    public float Mass { get; set; }
    public Import.RigidBodyShape Shape { get; }
    public Vector3 Size { get; }
    public Vector3 Origin { get; }
    public Quaternion Orientation { get; internal set; }
    public float LinearDamping { get; set; }
    public float AngularDamping { get; set; }
    public float Restitution { get; set; }
    public float Friction { get; set; }
    public Import.RigidBodyTransformType TransformType { get; }
    public bool IsBoneRelative { get; }
    public Vector3 Position { get; internal set; }
    public Vector3 Velocity { get; internal set; }
    public Vector3 AngularVelocity { get; internal set; }
    public Vector3 Torque { get; internal set; }
    public Import.RigidBodyType Type { get; }
    public Vector3? Gravity { get; set; }
    public ushort CollisionGroup { get; set; }
    public ushort CollisionMask { get; set; }

    internal float BoundingRadius => MathF.Max(MathF.Max(Size.X, Size.Y), Size.Z) * 0.5f;

    public RigidBody(string name, int boneIndex, float mass, Import.RigidBodyShape shape,
        Vector3 size, Vector3 origin, Quaternion orientation,
        float linearDamping, float angularDamping, float restitution, float friction,
        Import.RigidBodyTransformType transformType, bool isBoneRelative,
        Vector3 torque, Import.RigidBodyType type, Vector3? gravity,
        ushort collisionGroup, ushort collisionMask)
    {
        if (!Enum.IsDefined(typeof(Import.RigidBodyTransformType), transformType))
            throw new ArgumentOutOfRangeException(nameof(transformType), transformType, "Unknown transform type");

        Name = name;
        BoneIndex = boneIndex;
        Mass = mass;
        Shape = shape;
        Size = size;
        Origin = origin;
        Orientation = orientation;
        LinearDamping = linearDamping;
        AngularDamping = angularDamping;
        Restitution = restitution;
        Friction = friction;
        TransformType = transformType;
        IsBoneRelative = isBoneRelative;
        Position = origin;
        Velocity = Vector3.Zero;
        AngularVelocity = Vector3.Zero;
        Torque = torque;
        Type = type;
        Gravity = gravity;
        CollisionGroup = collisionGroup;
        CollisionMask = collisionMask;
    }

    internal void ApplyGravity(Vector3 worldGravity, float dt)
    {
        if (Mass <= 0f || Type != Import.RigidBodyType.Dynamic)
            return;
        var g = Gravity ?? worldGravity;
        Velocity += g * dt;
    }

    internal void Integrate(float dt)
    {
        if (Type == Import.RigidBodyType.Static)
            return;

        Position += Velocity * dt;
        Velocity *= 1f - LinearDamping * dt;
        if (Position.Z < 0f)
        {
            Position = new(Position.X, Position.Y, 0f);
            Velocity = new(
                Velocity.X * (1f - Friction),
                Velocity.Y * (1f - Friction),
                -Velocity.Z * Restitution);
        }

        if (Mass > 0f)
            AngularVelocity += (Torque / Mass) * dt;
        AngularVelocity *= 1f - AngularDamping * dt;
        var angDelta = AngularVelocity * dt;
        var dq = Quaternion.CreateFromYawPitchRoll(angDelta.Y, angDelta.X, angDelta.Z);
        Orientation = Quaternion.Normalize(dq * Orientation);
    }
}
