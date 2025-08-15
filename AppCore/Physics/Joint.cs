namespace MiniMikuDance.Physics;

using System.Numerics;

/// <summary>
/// ジョイントを表すクラス。
/// </summary>
public class Joint
{
    public string Name { get; }
    public RigidBody? BodyA { get; }
    public RigidBody? BodyB { get; }

    internal Joint(string name, RigidBody? bodyA, RigidBody? bodyB)
    {
        Name = name;
        BodyA = bodyA;
        BodyB = bodyB;
    }

    internal void Solve()
    {
        if (BodyA is null || BodyB is null)
            return;
        var mid = (BodyA.Position + BodyB.Position) * 0.5f;
        BodyA.Position = mid;
        BodyB.Position = mid;
        BodyA.Velocity = Vector3.Zero;
        BodyB.Velocity = Vector3.Zero;
    }
}
