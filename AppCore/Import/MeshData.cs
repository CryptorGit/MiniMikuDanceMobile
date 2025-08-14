using System.Collections.Generic;

namespace MiniMikuDance.Import;

public class MeshData
{
    public List<System.Numerics.Vector3> Vertices { get; } = new();
    public List<System.Numerics.Vector3> Normals { get; } = new();
    public List<System.Numerics.Vector2> TexCoords { get; } = new();
    public List<int[]> Faces { get; } = new();
    public int VertexCount => Vertices.Count;
}
