using System.Collections.Generic;

namespace MiniMikuDance.Data;

public class MmdModel
{
    public List<Import.RigidBodyData> RigidBodies { get; } = new();
    public List<Import.JointData> Joints { get; } = new();
}
