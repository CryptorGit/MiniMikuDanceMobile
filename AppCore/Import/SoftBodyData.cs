using System.Collections.Generic;

namespace MiniMikuDance.Import;

public enum SoftBodyShape
{
    TriMesh,
    Rope
}

public class SoftBodyAnchorData
{
    public int RigidBodyIndex { get; set; } = -1;
    public int VertexIndex { get; set; } = -1;
    public bool Near { get; set; }
}

public class SoftBodyData
{
    public string Name { get; set; } = string.Empty;
    public string NameEnglish { get; set; } = string.Empty;
    public int MaterialIndex { get; set; } = -1;
    public SoftBodyShape Shape { get; set; }
    public byte Group { get; set; }
    public ushort Mask { get; set; }
    public float Mass { get; set; }
    public float Friction { get; set; }
    public float SpringConstant { get; set; }
    public List<SoftBodyAnchorData> Anchors { get; set; } = new();
}
