using System.IO;
using MiniMikuDance.Util;

namespace AppCore.Tests;

public class JSONUtilTests
{
    private class TestData
    {
        public int Value { get; set; }
    }

    [Fact]
    public void Load_CreatesFileAndReturnsDefault_WhenFileDoesNotExist()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var path = Path.Combine(tempDir, "data.json");

        var data = JSONUtil.Load<TestData>(path);

        Assert.True(File.Exists(path));
        Assert.Equal(0, data.Value);

        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void SaveAndLoad_RoundTripPreservesData()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var path = Path.Combine(tempDir, "data.json");
        var original = new TestData { Value = 123 };

        JSONUtil.Save(path, original);
        var loaded = JSONUtil.Load<TestData>(path);

        Assert.Equal(123, loaded.Value);

        Directory.Delete(tempDir, true);
    }
}
