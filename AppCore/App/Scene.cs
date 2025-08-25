using System.Collections.Generic;
using MiniMikuDance.Import;
using MiniMikuDance.Physics;

namespace MiniMikuDance.App;

public class Scene
{
    public List<BoneData> Bones { get; } = new();
    public List<RigidBody> RigidBodies { get; } = new();
    public List<Joint> Joints { get; } = new();
}

