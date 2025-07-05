using System.Numerics;

namespace MiniMikuDance.Import;

public class BoneData
{
    public string Name { get; set; } = string.Empty;
    public Quaternion Rotation { get; set; } = Quaternion.Identity;
}
