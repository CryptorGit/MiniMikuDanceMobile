namespace MiniMikuDance.Import;

public class ModelData
{
    public System.Collections.Generic.List<SubMeshData> SubMeshes { get; } = new();
    public Assimp.Mesh Mesh { get; set; } = null!;
    public System.Numerics.Matrix4x4 Transform { get; set; } = System.Numerics.Matrix4x4.Identity;
    public System.Collections.Generic.List<BoneData> Bones { get; } = new();
    public System.Collections.Generic.Dictionary<string, int> HumanoidBones { get; } = new();
    public System.Collections.Generic.List<(string Name, int Index)> HumanoidBoneList { get; } = new();
    public float ShadeShift { get; set; } = -0.1f;
    public float ShadeToony { get; set; } = 0.9f;
    public float RimIntensity { get; set; } = 0.5f;
    public VrmInfo Info { get; set; } = new();
}
