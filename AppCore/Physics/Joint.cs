namespace MiniMikuDance.Physics;

using System;

/// <summary>
/// nanoem のジョイントを保持する薄いラッパークラス。
/// </summary>
public sealed class Joint : IDisposable
{
    public string Name { get; }
    public RigidBody? BodyA { get; }
    public RigidBody? BodyB { get; }
    internal nint Handle { get; }

    internal Joint(string name, RigidBody? bodyA, RigidBody? bodyB, nint handle)
    {
        Name = name;
        BodyA = bodyA;
        BodyB = bodyB;
        Handle = handle;
    }

    public void Dispose()
    {
        if (Handle != nint.Zero)
            NanoemPhysicsNative.PhysicsJointDestroy(Handle);
    }
}

