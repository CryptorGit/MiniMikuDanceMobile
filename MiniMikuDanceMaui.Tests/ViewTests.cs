using Xunit;
using MiniMikuDanceMaui;
using System.Reflection;
using MiniMikuDance.Camera;

public class ViewTests
{
    [Fact]
    public void SettingView_Initialize()
    {
        var view = new SettingView();
        Assert.NotNull(view);
        // property set/get round-trip
        view.HeightRatio = 0.5;
        Assert.Equal(0.5, view.HeightRatio, 3);
    }

    [Fact]
    public void KeyInputPanel_Initialize()
    {
        var panel = new KeyInputPanel();
        Assert.NotNull(panel);
    }

    [Fact]
    public void KeyDeletePanel_Initialize()
    {
        var panel = new KeyDeletePanel();
        Assert.NotNull(panel);
    }

    [Fact]
    public void GyroView_ButtonToggle()
    {
        var camera = new CameraController();
        var renderer = new VrmRenderer();
        var view = new GyroView(camera, renderer);
        var method = typeof(GyroView).GetMethod("OnGyroClicked", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        method.Invoke(view, new object[]{view});
    }

    [Fact]
    public void TimelineView_Initialize()
    {
        var view = new TimelineView();
        Assert.NotNull(view);
        view.CurrentFrame = 1;
        Assert.Equal(1, view.CurrentFrame);
    }

    [Fact]
    public void MainPage_Initialize()
    {
        var page = new MainPage();
        Assert.NotNull(page);
    }
}
