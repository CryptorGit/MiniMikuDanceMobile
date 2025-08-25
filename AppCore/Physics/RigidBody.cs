using System.Collections.Generic;
using System.Numerics;
using MiniMikuDance.Import;

namespace MiniMikuDance.Physics;

public class RigidBody
{
    public string Name { get; set; } = string.Empty;
    public string NameEnglish { get; set; } = string.Empty;
    public int BoneIndex { get; set; } = -1;
    public float Mass { get; set; }
    public Vector3 Inertia { get; set; } = Vector3.Zero;
    public RigidBodyShape Shape { get; set; }
    public float TranslationAttenuation { get; set; }
    public float RotationAttenuation { get; set; }
    public float Recoil { get; set; }
    public float Friction { get; set; }
    public Vector3 Position { get; set; } = Vector3.Zero;
    public Vector3 Rotation { get; set; } = Vector3.Zero;
    public Vector3 BoneOffsetPosition { get; set; } = Vector3.Zero;
    public Vector3 BoneOffsetRotation { get; set; } = Vector3.Zero;
    public Vector3 Velocity { get; set; } = Vector3.Zero;
    public Vector3 AngularVelocity { get; set; } = Vector3.Zero;
    public Vector3 Size { get; set; } = Vector3.Zero;
    public byte Group { get; set; }
    public ushort GroupTarget { get; set; }
    public RigidBodyPhysicsType PhysicsType { get; set; }
    public List<Joint> Joints { get; } = new();
}

public class Joint
{
    public string Name { get; set; } = string.Empty;
    public int RigidBodyA { get; set; } = -1;
    public int RigidBodyB { get; set; } = -1;
    public Vector3 Rotation { get; set; } = Vector3.Zero;
    public Vector3 PositionMin { get; set; } = Vector3.Zero;
    public Vector3 PositionMax { get; set; } = Vector3.Zero;
    public Vector3 RotationMin { get; set; } = Vector3.Zero;
    public Vector3 RotationMax { get; set; } = Vector3.Zero;
    public Vector3 SpringPosition { get; set; } = Vector3.Zero;
    public Vector3 SpringRotation { get; set; } = Vector3.Zero;
    public Vector3 AnchorA { get; set; } = Vector3.Zero;
    public Vector3 AnchorB { get; set; } = Vector3.Zero;
}
