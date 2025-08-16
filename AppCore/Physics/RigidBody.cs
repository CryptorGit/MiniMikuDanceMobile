namespace MiniMikuDance.Physics;

using System;
using System.Numerics;
using MiniMikuDance.Import;

/// <summary>
/// 剛体を表すクラス。
/// </summary>
public class RigidBody
{
    public string Name { get; }
    public int BoneIndex { get; }
    public float Mass { get; set; }
    public RigidBodyShape Shape { get; }
    public Vector3 Size { get; }
    public Vector3 Origin { get; }
    public Quaternion Orientation { get; internal set; }
    public float LinearDamping { get; set; }
    public float AngularDamping { get; set; }
    public float Restitution { get; set; }
    public float Friction { get; set; }
    public RigidBodyTransformType TransformType { get; }
    public bool IsBoneRelative { get; }
    public bool IsMorph { get; }
    private bool _isActive = true;
    public bool IsActive => _isActive;
    public Vector3 Position { get; internal set; }
    public Vector3 Velocity { get; internal set; }
    public Vector3 AngularVelocity { get; internal set; }
    public Vector3 Torque { get; internal set; }
    public RigidBodyType Type { get; }
    public Vector3? Gravity { get; set; }
    public ushort CollisionGroup { get; set; }
    public ushort CollisionMask { get; set; }

    // 初期状態保持用
    private readonly Vector3 _initialLocalPosition;
    private readonly Quaternion _initialLocalOrientation;
    private Vector3 _initialWorldPosition;
    private Quaternion _initialWorldOrientation;
    private Vector3 _initialBoneTranslation;
    private Quaternion _initialBoneRotation;
    private bool _initialized;

    // インパルス適用用
    public Vector3 TorqueImpulse { get; set; }
    public Vector3 VelocityImpulse { get; set; }

    internal float BoundingRadius => MathF.Max(MathF.Max(Size.X, Size.Y), Size.Z) * 0.5f;

    public RigidBody(string name, int boneIndex, float mass, Import.RigidBodyShape shape,
        Vector3 size, Vector3 origin, Quaternion orientation,
        float linearDamping, float angularDamping, float restitution, float friction,
        RigidBodyTransformType transformType, bool isBoneRelative, bool isMorph,
        Vector3 torque, RigidBodyType type, Vector3? gravity,
        ushort collisionGroup, ushort collisionMask)
    {
        if (!Enum.IsDefined(typeof(RigidBodyTransformType), transformType))
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
        IsMorph = isMorph;
        Position = origin;
        Velocity = Vector3.Zero;
        AngularVelocity = Vector3.Zero;
        Torque = torque;
        Type = type;
        Gravity = gravity;
        CollisionGroup = collisionGroup;
        CollisionMask = collisionMask;

        _initialLocalPosition = origin;
        _initialLocalOrientation = orientation;
        _initialWorldPosition = origin;
        _initialWorldOrientation = orientation;
    }

    private void InitializeTransforms(BoneData bone)
    {
        if (_initialized)
            return;
        _initialBoneTranslation = bone.Translation;
        _initialBoneRotation = bone.Rotation;
        if (IsBoneRelative)
        {
            _initialWorldPosition = bone.Translation + Vector3.Transform(_initialLocalPosition, bone.Rotation);
            _initialWorldOrientation = Quaternion.Normalize(bone.Rotation * _initialLocalOrientation);
        }
        else
        {
            _initialWorldPosition = _initialLocalPosition;
            _initialWorldOrientation = _initialLocalOrientation;
        }
        Position = _initialWorldPosition;
        Orientation = _initialWorldOrientation;
        _initialized = true;
    }

    public void ApplyAllForces()
    {
        if (TorqueImpulse != Vector3.Zero)
        {
            AngularVelocity += TorqueImpulse;
            TorqueImpulse = Vector3.Zero;
        }
        if (VelocityImpulse != Vector3.Zero)
        {
            Velocity += VelocityImpulse;
            VelocityImpulse = Vector3.Zero;
        }
    }

    public void SyncToSimulation(BoneData bone)
    {
        if (TransformType != RigidBodyTransformType.FromBoneToSimulation && Type != RigidBodyType.Kinematic)
            return;
        InitializeTransforms(bone);
        Position = IsBoneRelative
            ? bone.Translation + Vector3.Transform(_initialLocalPosition, bone.Rotation)
            : _initialLocalPosition;
        Orientation = IsBoneRelative
            ? Quaternion.Normalize(bone.Rotation * _initialLocalOrientation)
            : _initialLocalOrientation;
        Velocity = Vector3.Zero;
        AngularVelocity = Vector3.Zero;
        _isActive = true;
    }

    public void SyncFromSimulation(BoneData bone, bool followBone)
    {
        if (TransformType == RigidBodyTransformType.FromBoneToSimulation || Type == RigidBodyType.Kinematic)
            return;
        InitializeTransforms(bone);
        var deltaPos = Position - _initialWorldPosition;
        var deltaRot = Orientation * Quaternion.Inverse(_initialWorldOrientation);
        switch (TransformType)
        {
            case RigidBodyTransformType.FromSimulationToBone:
                bone.Translation = _initialBoneTranslation + deltaPos;
                bone.Rotation = Quaternion.Normalize(deltaRot * _initialBoneRotation);
                break;
            case RigidBodyTransformType.FromBoneOrientationAndSimulationToBone:
                bone.Translation = _initialBoneTranslation + deltaPos;
                if (followBone)
                {
                    Position = IsBoneRelative
                        ? bone.Translation + Vector3.Transform(_initialLocalPosition, bone.Rotation)
                        : _initialLocalPosition;
                    Orientation = IsBoneRelative
                        ? Quaternion.Normalize(bone.Rotation * _initialLocalOrientation)
                        : _initialLocalOrientation;
                    _initialWorldPosition = Position;
                    _initialWorldOrientation = Orientation;
                }
                else
                {
                    bone.Rotation = _initialBoneRotation;
                }
                break;
            case RigidBodyTransformType.FromBoneTranslationAndSimulationToBone:
                bone.Rotation = Quaternion.Normalize(deltaRot * _initialBoneRotation);
                if (followBone)
                {
                    Position = IsBoneRelative
                        ? bone.Translation + Vector3.Transform(_initialLocalPosition, bone.Rotation)
                        : _initialLocalPosition;
                    _initialWorldPosition = Position;
                    _initialWorldOrientation = Orientation = IsBoneRelative
                        ? Quaternion.Normalize(bone.Rotation * _initialLocalOrientation)
                        : _initialLocalOrientation;
                }
                else
                {
                    bone.Translation = _initialBoneTranslation;
                }
                break;
        }

        if (IsMorph)
            _isActive = true;
    }

    internal void ApplyGravity(Vector3 worldGravity, float dt)
    {
        if (!_isActive || Mass <= 0f || Type != Import.RigidBodyType.Dynamic)
            return;
        var g = Gravity ?? worldGravity;
        Velocity += g * dt;
    }

    internal void Integrate(float dt)
    {
        if (!_isActive || Type == Import.RigidBodyType.Static)
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

        if (!IsMorph && Velocity.LengthSquared() < 1e-6f && AngularVelocity.LengthSquared() < 1e-6f)
            _isActive = false;
        else
            _isActive = true;
    }
}
