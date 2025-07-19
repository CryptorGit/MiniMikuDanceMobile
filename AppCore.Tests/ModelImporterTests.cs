using System.IO;
using MiniMikuDance.Import;
using Xunit;

public class ModelImporterTests
{
    [Fact]
    public void ImportModel_FileNotFound_ThrowsFileNotFoundException()
    {
        var importer = new ModelImporter();
        Assert.Throws<FileNotFoundException>(() => importer.ImportModel("nonexistent.vrm"));
    }
}
