using Xunit;
using MiniMikuDanceMaui;
using System.Reflection;
using MiniMikuDance.Camera;
using OpenTK.Mathematics;
using System.Collections.Generic;

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
    public void AddKeyPanel_Initialize()
    {
        var panel = new AddKeyPanel();
        Assert.NotNull(panel);
    }

    [Fact]
    public void EditKeyPanel_Initialize()
    {
        var panel = new EditKeyPanel();
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
    public void TimelineView_CurrentFrameChanged_Event()
    {
        var view = new TimelineView();
        int changed = -1;
        view.CurrentFrameChanged += f => changed = f;
        view.CurrentFrame = 5;
        Assert.Equal(5, changed);
    }

    [Fact]
    public void MainPage_Initialize()
    {
        var page = new MainPage();
        Assert.NotNull(page);
    }

    [Fact]
    public void VrmRenderer_ClampsBoneRotation()
    {
        var renderer = new VrmRenderer();
        renderer.SetRotationLimits(new Dictionary<int, (Vector3 Min, Vector3 Max)>
        {
            [0] = (new Vector3(-10, -10, -10), new Vector3(10, 10, 10))
        });
        renderer.SetBoneRotation(0, new Vector3(20, -20, 5));
        var r = renderer.GetBoneRotation(0);
        Assert.Equal(10, r.X, 3);
        Assert.Equal(-10, r.Y, 3);
        Assert.Equal(5, r.Z, 3);
    }
}
