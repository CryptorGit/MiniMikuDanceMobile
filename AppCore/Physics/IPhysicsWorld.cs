using System;
using System.Numerics;
using MiniMikuDance.App;

namespace MiniMikuDance.Physics;

public interface IPhysicsWorld : IDisposable
{
    void Initialize(PhysicsConfig config, float modelScale);
    void Step(float dt);
    void SyncFromBones(Scene scene);
    void SyncToBones(Scene scene);
    Vector3 GetGravity();
}
