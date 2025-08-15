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
    public Vector3 Size { get; }
    public Vector3 Origin { get; }
    public Quaternion Orientation { get; internal set; }
    public float LinearDamping { get; }
    public float AngularDamping { get; }
    public float Restitution { get; }
    public float Friction { get; }
    public Import.RigidBodyTransformType TransformType { get; }
    public bool IsBoneRelative { get; }
    public Vector3 Position { get; internal set; }
    public Vector3 Velocity { get; internal set; }

    public RigidBody(string name, int boneIndex, float mass, Import.RigidBodyShape shape,
        Vector3 size, Vector3 origin, Quaternion orientation,
        float linearDamping, float angularDamping, float restitution, float friction,
        Import.RigidBodyTransformType transformType, bool isBoneRelative)
    {
        Name = name;
        BoneIndex = boneIndex;
        Mass = mass;
        Shape = shape;
        Size = size;
        Origin = origin;
        Orientation = orientation;
        LinearDamping = linearDamping;
        AngularDamping = angularDamping;
        Restitution = restitution;
        Friction = friction;
        TransformType = transformType;
        IsBoneRelative = isBoneRelative;
        Position = origin;
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
