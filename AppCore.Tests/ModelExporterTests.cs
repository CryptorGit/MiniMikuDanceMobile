using System;
using MiniMikuDance.Import;
using Xunit;

public class ModelExporterTests
{
    [Fact]
    public void ExportModel_NullMesh_ThrowsArgumentException()
    {
        var exporter = new ModelExporter();
        var model = new ModelData();
        Assert.Throws<ArgumentException>(() => exporter.ExportModel(model, "dummy.obj"));
    }
}
