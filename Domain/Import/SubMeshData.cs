using Assimp;

namespace MiniMikuDance.Import;

public enum SphereMode
{
    None,
    Multiply,
    Add,
    SubTexture
}

public class SubMeshData
{
    public Mesh Mesh { get; set; } = null!;
    public System.Numerics.Vector4 ColorFactor { get; set; } = System.Numerics.Vector4.One;
    public System.Numerics.Vector3 Specular { get; set; }
    public float SpecularPower { get; set; }
    public System.Numerics.Vector4 EdgeColor { get; set; }
    public float EdgeSize { get; set; }
    public System.Numerics.Vector3 ToonColor { get; set; } = System.Numerics.Vector3.One;
    public System.Numerics.Vector4 TextureTint { get; set; } = System.Numerics.Vector4.One;
    public List<System.Numerics.Vector2> TexCoords { get; set; } = new();
    public List<System.Numerics.Vector4> AdditionalUV1 { get; set; } = new();
    public List<System.Numerics.Vector4> AdditionalUV2 { get; set; } = new();
    public List<System.Numerics.Vector4> AdditionalUV3 { get; set; } = new();
    public List<System.Numerics.Vector4> AdditionalUV4 { get; set; } = new();
    public byte[]? TextureBytes { get; set; }
    public int TextureWidth { get; set; }
    public int TextureHeight { get; set; }
    public string? TextureFilePath { get; set; }
    public byte[]? SphereTextureBytes { get; set; }
    public int SphereTextureWidth { get; set; }
    public int SphereTextureHeight { get; set; }
    public string? SphereTextureFilePath { get; set; }
    public byte[]? ToonTextureBytes { get; set; }
    public int ToonTextureWidth { get; set; }
    public int ToonTextureHeight { get; set; }
    public string? ToonTextureFilePath { get; set; }
    public SphereMode SphereMode { get; set; } = SphereMode.None;
    public List<System.Numerics.Vector4> JointIndices { get; set; } = new();
    public List<System.Numerics.Vector4> JointWeights { get; set; } = new();
    public List<System.Numerics.Vector3> SdefC { get; set; } = new();
    public List<System.Numerics.Vector3> SdefR0 { get; set; } = new();
    public List<System.Numerics.Vector3> SdefR1 { get; set; } = new();
}
