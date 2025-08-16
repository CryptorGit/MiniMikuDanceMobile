namespace MiniMikuDance.Physics;

using System;
using System.Numerics;
using MiniMikuDance.Import;

/// <summary>
/// nanoem の剛体を保持する薄いラッパークラス。
/// </summary>
public sealed class RigidBody : IDisposable
{
    public string Name { get; }
    public int BoneIndex { get; }
    public RigidBodyTransformType TransformType { get; }
    public Vector3 Position { get; internal set; }
    public Quaternion Rotation { get; internal set; }
    internal nint Handle { get; }
    public Vector3 Position { get; internal set; }
    public Quaternion Orientation { get; internal set; }

    internal RigidBody(string name, int boneIndex, RigidBodyTransformType transformType, nint handle)
    {
        Name = name;
        BoneIndex = boneIndex;
        TransformType = transformType;
        Handle = handle;
        Position = Vector3.Zero;
        Orientation = Quaternion.Identity;
    }

    public void Dispose()
    {
        if (Handle != nint.Zero)
            NanoemPhysicsNative.PhysicsRigidBodyDestroy(Handle);
    }
}

