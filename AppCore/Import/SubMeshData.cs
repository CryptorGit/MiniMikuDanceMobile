using Assimp;

namespace MiniMikuDance.Import;

public class SubMeshData
{
    public Mesh Mesh { get; set; } = null!;
    public System.Numerics.Vector4 ColorFactor { get; set; } = System.Numerics.Vector4.One;
}
