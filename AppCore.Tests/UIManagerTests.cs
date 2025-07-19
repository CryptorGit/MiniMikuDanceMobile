using System;
using System.IO;
using MiniMikuDance.UI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

public class UIManagerTests
{
    [Fact]
    public void LoadConfigAndToggle()
    {
        var cfg = new UIConfig();
        cfg.Toggles.Add(new UIToggle { Id = "t1", DefaultValue = true });
        UIManager.Instance.LoadConfig(cfg);
        Assert.True(UIManager.Instance.GetToggle("t1"));
        UIManager.Instance.SetToggleState("t1", false);
        Assert.False(UIManager.Instance.GetToggle("t1"));
    }

    [Fact]
    public void RegisterLoaderAndSetThumbnail()
    {
        int called = 0;
        UIManager.Instance.RegisterTextureLoader((data,w,h) => { called = w*h; return 1; });
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".png");
        using (var img = new Image<Rgba32>(1,1))
            img.Save(path);
        UIManager.Instance.SetThumbnail(path);
        Assert.Equal(1, called);
        File.Delete(path);
    }

    [Fact]
    public void SetMessage_Works()
    {
        UIManager.Instance.SetMessage("hello");
        Assert.Equal("hello", UIManager.Instance.Message);
    }

    [Fact]
    public void SaveConfig_WritesFile()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid()+".json");
        UIManager.Instance.LoadConfig(new UIConfig());
        UIManager.Instance.SaveConfig(path);
        Assert.True(File.Exists(path));
        File.Delete(path);
    }
}
