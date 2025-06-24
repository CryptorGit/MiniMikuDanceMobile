using MMDTools;
using SharpGLTF.Scenes;
using System.Numerics;
using SharpGLTF.Geometry.VertexTypes;

namespace MiniMikuDance.Import;

public static class PmxToGltfService
{
    public static byte[] Convert(Stream pmx)
    {
        var pmxModel = PMXParser.Parse(pmx);
        var scene = new SceneBuilder();
        // TODO: convert vertices and skins properly
        // Note: minimal placeholder implementation without geometry
        var model = scene.ToSchema2();
        return model.WriteGLB().ToArray();
    }
}
