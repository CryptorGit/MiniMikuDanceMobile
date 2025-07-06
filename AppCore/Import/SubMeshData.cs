using Assimp;

namespace MiniMikuDance.Import;

public class SubMeshData
{
    public Mesh Mesh { get; set; } = null!;
    public System.Numerics.Vector4 ColorFactor { get; set; } = System.Numerics.Vector4.One;
    public List<System.Numerics.Vector2> TexCoords { get; } = new();
    public byte[]? TextureBytes { get; set; }
    public int TextureWidth { get; set; }
    public int TextureHeight { get; set; }
    public List<System.Numerics.Vector4> JointIndices { get; } = new();
    public List<System.Numerics.Vector4> JointWeights { get; } = new();
}
