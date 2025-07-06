namespace MiniMikuDance.Import;

public class ModelData
{
    public System.Numerics.Matrix4x4 Transform { get; set; } = System.Numerics.Matrix4x4.Identity;
    public float ShadeShift { get; set; } = -0.1f;
    public float ShadeToony { get; set; } = 0.9f;
    public float RimIntensity { get; set; } = 0.5f;
    public VrmInfo Info { get; set; } = new();
}

public class ModelImporter
{
    public ModelData ImportModel(string path)
    {
        // 元の実装では VRM 解析とメッシュ生成を行っていましたが、
        // ボーンとメッシュ処理を一旦削除したため、
        // ここでは空の ModelData を返します。
        return new ModelData();
    }

    public ModelData ImportModel(System.IO.Stream stream)
    {
        return new ModelData();
    }
}
