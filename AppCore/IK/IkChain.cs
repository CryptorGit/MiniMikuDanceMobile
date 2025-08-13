using System.Numerics;

namespace MiniMikuDance.IK;

public class IkChain
{
    public IkBone[] Bones { get; }
    public Vector3 Target { get; set; }
    public IkBone? ChestBone { get; set; }
    public int MaxIterations { get; set; } = 10;
    public float Threshold { get; set; } = 1e-3f;

    public IkChain(IkBone[] bones, Vector3 target)
    {
        Bones = bones;
        Target = target;
    }
}
