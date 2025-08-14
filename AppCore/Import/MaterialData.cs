using System.Numerics;

namespace MiniMikuDance.Import;

public class MaterialData
{
    public string Name { get; set; } = string.Empty;
    public string EnglishName { get; set; } = string.Empty;
    public Vector4 Diffuse { get; set; }
    public Vector4 Specular { get; set; }
    public Vector4 Ambient { get; set; }
    public int TextureIndex { get; set; } = -1;
}
