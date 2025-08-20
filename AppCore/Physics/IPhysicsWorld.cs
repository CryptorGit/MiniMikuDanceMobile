using System;
using MiniMikuDance.App;

namespace MiniMikuDance.Physics;

public interface IPhysicsWorld : IDisposable
{
    void Initialize(PhysicsConfig config, float modelScale);
    void Step(float dt);
    void SyncToBones(Scene scene);
}
