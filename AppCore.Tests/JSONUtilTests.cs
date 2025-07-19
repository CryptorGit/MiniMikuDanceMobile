using System;
using System.IO;
using System.Numerics;
using System.Text;
using MiniMikuDance.Util;
using Xunit;

public class JSONUtilTests
{
    private class Dummy
    {
        public Vector3 Vec { get; set; }
        public int Num { get; set; }
    }

    [Fact]
    public void SaveLoad_RoundTrip()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        var data = new Dummy { Vec = new Vector3(1, 2, 3), Num = 5 };
        JSONUtil.Save(tmp, data);
        var loaded = JSONUtil.Load<Dummy>(tmp);
        Assert.Equal(data.Vec, loaded.Vec);
        Assert.Equal(data.Num, loaded.Num);
        File.Delete(tmp);
    }

    [Fact]
    public void LoadFromStream_Success()
    {
        var json = "{\"Vec\":{\"X\":4,\"Y\":5,\"Z\":6},\"Num\":7}";
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var loaded = JSONUtil.LoadFromStream<Dummy>(ms);
        Assert.Equal(new Vector3(4,5,6), loaded.Vec);
        Assert.Equal(7, loaded.Num);
    }

    [Fact]
    public void Load_FileNotExists_CreatesDefault()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "missing.json");
        var loaded = JSONUtil.Load<Dummy>(path);
        Assert.True(File.Exists(path));
        Assert.Equal(default, loaded.Vec);
        Assert.Equal(0, loaded.Num);
        Directory.Delete(dir, true);
    }

    [Fact]
    public void LoadFromStream_Invalid_ReturnsDefault()
    {
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes("{invalid"));
        var loaded = JSONUtil.LoadFromStream<Dummy>(ms);
        Assert.Equal(default, loaded.Vec);
        Assert.Equal(0, loaded.Num);
    }
}
