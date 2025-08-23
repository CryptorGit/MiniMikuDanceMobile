using System;
using MiniMikuDance.App;

namespace MiniMikuDance.Physics;

public interface IPhysicsWorld : IDisposable
{
    void Initialize(PhysicsConfig config, float modelScale, bool useScaledGravity);
    void Step(float dt);
    void SyncFromBones(Scene scene);
    void SyncToBones(Scene scene);
}
