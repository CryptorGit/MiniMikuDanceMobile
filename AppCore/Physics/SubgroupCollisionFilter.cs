namespace MiniMikuDance.AppCore.Physics;

public struct SubgroupCollisionFilter
{
    public uint Group;
    public uint Mask;

    public SubgroupCollisionFilter(uint group, uint mask)
    {
        Group = group;
        Mask = mask;
    }

    public static bool AllowCollision(in SubgroupCollisionFilter a, in SubgroupCollisionFilter b)
    {
        return (a.Group & b.Mask) != 0 && (b.Group & a.Mask) != 0;
    }
}
