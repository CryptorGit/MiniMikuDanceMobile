using System;
using MiniMikuDance.App;

namespace MiniMikuDance.AppCore.Physics;

public interface IPhysicsWorld : IDisposable
{
    void Initialize(PhysicsConfig config, float modelScale);
    void Step(float dt);
    void SyncFromBones(Scene scene);
    void SyncToBones(Scene scene);
}
