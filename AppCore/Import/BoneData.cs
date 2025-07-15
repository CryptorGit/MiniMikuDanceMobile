using System.Numerics;

namespace MiniMikuDance.Import;

public class BoneData
{
    public string Name { get; set; } = string.Empty;
    public int Parent { get; set; } = -1;
    public Quaternion Rotation { get; set; } = Quaternion.Identity;
    public Vector3 Translation { get; set; } = Vector3.Zero;
    public (float Min, float Max) RotationXRange { get; set; } = (0f, 0f);
    public (float Min, float Max) RotationYRange { get; set; } = (0f, 0f);
    public (float Min, float Max) RotationZRange { get; set; } = (0f, 0f);
    public Matrix4x4 BindMatrix { get; set; } = Matrix4x4.Identity;
    public Matrix4x4 InverseBindMatrix { get; set; } = Matrix4x4.Identity;
}
