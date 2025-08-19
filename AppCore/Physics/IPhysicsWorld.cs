using System;
using MiniMikuDance.App;

namespace MiniMikuDance.Physics;

public interface IPhysicsWorld : IDisposable
{
    void Initialize();
    void Step(float dt);
    void SyncToBones(Scene scene);
}
