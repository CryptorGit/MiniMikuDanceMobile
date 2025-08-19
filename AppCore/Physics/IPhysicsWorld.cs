using System;

namespace MiniMikuDance.Physics;

public interface IPhysicsWorld : IDisposable
{
    void Initialize();
    void Step(float dt);
}
