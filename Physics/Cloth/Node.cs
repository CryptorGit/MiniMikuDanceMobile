using System.Numerics;

namespace MiniMikuDance.Physics.Cloth;

public struct Node
{
    public Vector3 Position;
    public Vector3 Velocity;
    public float InverseMass;
}

