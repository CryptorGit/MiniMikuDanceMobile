using System;
using System.IO;
using MiniMikuDance.Data;
using Xunit;

public class DataManagerTests
{
    private class DummyCfg
    {
        public int Value { get; set; }
    }

    [Fact]
    public void SaveLoadConfig_RoundTrip()
    {
        var key = Guid.NewGuid().ToString();
        var cfg = new DummyCfg { Value = 10 };
        DataManager.Instance.SaveConfig(key, cfg);
        var loaded = DataManager.Instance.LoadConfig<DummyCfg>(key);
        Assert.Equal(10, loaded.Value);
        File.Delete($"Configs/{key}.json");
    }

    [Fact]
    public void CleanupTemp_CreatesEmptyDir()
    {
        var dm = DataManager.Instance;
        var field = typeof(DataManager).GetField("_tempDir", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var path = (string)field.GetValue(dm)!;
        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, "tmp.txt"), "x");
        dm.CleanupTemp();
        Assert.True(Directory.Exists(path));
        Assert.Empty(Directory.GetFiles(path));
    }
}
