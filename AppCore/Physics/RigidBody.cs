namespace MiniMikuDance.Physics;

using System;
using MiniMikuDance.Import;

/// <summary>
/// nanoem の剛体を保持する薄いラッパークラス。
/// </summary>
public sealed class RigidBody : IDisposable
{
    public string Name { get; }
    public int BoneIndex { get; }
    public RigidBodyTransformType TransformType { get; }
    internal nint Handle { get; }

    internal RigidBody(string name, int boneIndex, RigidBodyTransformType transformType, nint handle)
    {
        Name = name;
        BoneIndex = boneIndex;
        TransformType = transformType;
        Handle = handle;
    }

    public void Dispose()
    {
        if (Handle != nint.Zero)
            NanoemPhysicsNative.PhysicsRigidBodyDestroy(Handle);
    }
}

