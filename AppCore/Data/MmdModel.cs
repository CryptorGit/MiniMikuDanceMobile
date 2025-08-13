namespace MiniMikuDance.Data;

using System.Collections.Generic;
using System.Numerics;
using Assimp;
using MiniMikuDance.Import;

public class MmdModel
{
    public List<SubMeshData> SubMeshes { get; set; } = new();
    public Mesh Mesh { get; set; } = null!;
    public Matrix4x4 Transform { get; set; } = Matrix4x4.Identity;
    public List<BoneData> Bones { get; set; } = new();
    public Dictionary<string, int> HumanoidBones { get; set; } = new(System.StringComparer.OrdinalIgnoreCase);
    public List<(string Name, int Index)> HumanoidBoneList { get; set; } = new();
    public List<MorphData> Morphs { get; set; } = new();
    public List<RigidBodyData> RigidBodies { get; set; } = new();
    public List<JointData> Joints { get; set; } = new();
    public List<DisplayFrameData> DisplayFrames { get; set; } = new();
    public float ShadeShift { get; set; } = -0.1f;
    public float ShadeToony { get; set; } = 0.9f;
    public float RimIntensity { get; set; } = 0.5f;
}
