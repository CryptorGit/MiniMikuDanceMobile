namespace MiniMikuDance.Physics;

using System.Numerics;

/// <summary>
/// 剛体を表すクラス。
/// </summary>
public class RigidBody
{
    public string Name { get; }
    public int BoneIndex { get; }
    public float Mass { get; }
    public Import.RigidBodyShape Shape { get; }
    public Vector3 Position { get; internal set; }
    public Vector3 Velocity { get; internal set; }

    public RigidBody(string name, int boneIndex, float mass, Import.RigidBodyShape shape)
    {
        Name = name;
        BoneIndex = boneIndex;
        Mass = mass;
        Shape = shape;
        Position = Vector3.Zero;
        Velocity = Vector3.Zero;
    }

    internal void ApplyGravity(Vector3 gravity, float dt)
    {
        if (Mass <= 0f)
            return;
        Velocity += gravity * dt;
    }

    internal void Integrate(float dt)
    {
        Position += Velocity * dt;
        if (Position.Z < 0f)
        {
            Position = new(Position.X, Position.Y, 0f);
            Velocity = new(Velocity.X, Velocity.Y, 0f);
        }
    }
}
