using Assimp;

namespace MiniMikuDance.Import;

public class SubMeshData
{
    public Mesh Mesh { get; set; } = null!;
    public byte[]? TextureData { get; set; }
    public int TextureWidth { get; set; }
    public int TextureHeight { get; set; }
    public System.Numerics.Vector4 ColorFactor { get; set; } = System.Numerics.Vector4.One;
}
