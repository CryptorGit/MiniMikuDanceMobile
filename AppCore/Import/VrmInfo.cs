namespace MiniMikuDance.Import;

public class VrmInfo
{
    public string SpecVersion { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string License { get; set; } = string.Empty;
    public int NodeCount { get; set; }
    public int MeshCount { get; set; }
    public int SkinCount { get; set; }
    public int VertexCount { get; set; }
    public int TriangleCount { get; set; }
    public int MaterialCount { get; set; }
    public int TextureCount { get; set; }
    public int HumanoidBoneCount { get; set; }
}
