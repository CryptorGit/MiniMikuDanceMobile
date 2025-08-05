using Assimp;

namespace MiniMikuDance.Import;

public class SubMeshData
{
    public Mesh Mesh { get; set; } = null!;
    public System.Numerics.Vector4 ColorFactor { get; set; } = System.Numerics.Vector4.One;
    public List<System.Numerics.Vector2> TexCoords { get; set; } = new();
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
    public SphereMapMode SphereMode { get; set; } = SphereMapMode.None;
    public List<System.Numerics.Vector4> JointIndices { get; set; } = new();
    public List<System.Numerics.Vector4> JointWeights { get; set; } = new();
    public List<int> BaseVertexIndices { get; set; } = new();
}
