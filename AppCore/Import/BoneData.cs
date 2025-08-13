using System.Numerics;

namespace MiniMikuDance.Import;

public class BoneData
{
    public string Name { get; set; } = string.Empty;
    public int Parent { get; set; } = -1;
    public Quaternion Rotation { get; set; } = Quaternion.Identity;
    public Vector3 Translation { get; set; } = Vector3.Zero;
    public Vector3 BaseForward { get; set; } = Vector3.UnitY;
    public Vector3 BaseUp { get; set; } = Vector3.UnitY;
    public bool InheritRotation { get; set; }
    public bool InheritTranslation { get; set; }
    public int InheritParent { get; set; } = -1;
    public float InheritRatio { get; set; } = 1f;
    public bool HasFixedAxis { get; set; }
    public Vector3 FixedAxis { get; set; } = Vector3.Zero;
    public bool HasLocalAxis { get; set; }
    public Vector3 LocalAxisX { get; set; } = Vector3.UnitX;
    public Vector3 LocalAxisZ { get; set; } = Vector3.UnitZ;
    public int ExternalParent { get; set; } = -1;
    public Matrix4x4 BindMatrix { get; set; } = Matrix4x4.Identity;
    public Matrix4x4 InverseBindMatrix { get; set; } = Matrix4x4.Identity;
}
