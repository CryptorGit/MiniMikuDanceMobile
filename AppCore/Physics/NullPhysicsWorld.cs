using System;
using System.Numerics;
using MiniMikuDance.App;

namespace MiniMikuDance.Physics;

public sealed class NullPhysicsWorld : IPhysicsWorld
{
    public void Initialize(PhysicsConfig config, float modelScale, bool useScaledGravity) { }
    public void Step(float dt) { }
    public void SyncFromBones(Scene scene) { }
    public void SyncToBones(Scene scene) { }
    public Vector3 GetGravity() => Vector3.Zero;
    public void Dispose() { }
}
