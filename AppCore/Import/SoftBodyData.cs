namespace MiniMikuDance.Import;

public enum SoftBodyShape
{
    TriMesh,
    Rope,
    Cloth
}

public class SoftBodyData
{
    public string Name { get; set; } = string.Empty;
    public string NameEnglish { get; set; } = string.Empty;
    public int MaterialIndex { get; set; } = -1;
    public SoftBodyShape Shape { get; set; }
    public byte Group { get; set; }
    public ushort Mask { get; set; }
    public float NodeMass { get; set; } = 1f;
    public float SpringStiffness { get; set; } = 100f;
    public float SpringDamping { get; set; } = 5f;
}
