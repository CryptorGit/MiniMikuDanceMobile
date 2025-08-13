namespace MiniMikuDance.Physix;

using System.Numerics;
using MiniMikuDance.Data;

public interface IPhysicsEngine
{
    void Setup(MmdModel model);
    void Step(float deltaTime);
    bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out RayHit hit);
}

public struct RayHit
{
    public bool HasHit;
    public Vector3 Position;
    public Vector3 Normal;
    public float Distance;
}
