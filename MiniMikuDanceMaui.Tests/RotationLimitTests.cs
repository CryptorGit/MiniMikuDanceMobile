using System.Reflection;
using OpenTK.Mathematics;
using MiniMikuDanceMaui;
using MiniMikuDance.App;
using MiniMikuDance.Import;
using Xunit;

public class RotationLimitTests
{
    private MainPage CreatePage(BonesConfig cfg)
    {
        App.Initializer.BonesConfig = cfg;
        return new MainPage();
    }

    [Fact]
    public void OnKeyParameterChanged_ClampsRotation()
    {
        var cfg = new BonesConfig();
        cfg.HumanoidBoneLimits.Add(new BoneLimit
        {
            Bone = "b",
            Min = new System.Numerics.Vector3(-10, -10, -10),
            Max = new System.Numerics.Vector3(10, 10, 10)
        });
        var page = CreatePage(cfg);

        var model = new ModelData();
        model.HumanoidBones["b"] = 0;
        var field = typeof(MainPage).GetField("_currentModel", BindingFlags.NonPublic | BindingFlags.Instance)!;
        field.SetValue(page, model);

        var method = typeof(MainPage).GetMethod("OnKeyParameterChanged", BindingFlags.NonPublic | BindingFlags.Instance)!;
        method.Invoke(page, new object?[] { "b", 0, new Vector3(), new Vector3(20, 0, 0) });

        var rField = typeof(MainPage).GetField("_renderer", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var renderer = (VrmRenderer)rField.GetValue(page)!;
        var rot = renderer.GetBoneRotation(0);
        Assert.InRange(rot.X, 9.9f, 10.1f);
    }

    [Fact]
    public void OnBoneAxisValueChanged_ClampsRotation()
    {
        var cfg = new BonesConfig();
        cfg.HumanoidBoneLimits.Add(new BoneLimit
        {
            Bone = "b",
            Min = new System.Numerics.Vector3(-10, -10, -10),
            Max = new System.Numerics.Vector3(10, 10, 10)
        });
        var page = CreatePage(cfg);

        var model = new ModelData();
        model.HumanoidBones["b"] = 0;
        typeof(MainPage).GetField("_currentModel", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(page, model);

        var panelField = typeof(MainPage).GetField("AddKeyPanel", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        var panel = (AddKeyPanel?)panelField?.GetValue(page);
        Assert.NotNull(panel);
        panel!.SetBones(new[] { "b" });
        panel.SelectedBoneIndex = 0;
        panel.IsVisible = true;
        panel.SetRotation(new Vector3(20, 0, 0));

        var method = typeof(MainPage).GetMethod("OnBoneAxisValueChanged", BindingFlags.NonPublic | BindingFlags.Instance)!;
        method.Invoke(page, new object?[] { 0.0 });

        var renderer = (VrmRenderer)typeof(MainPage).GetField("_renderer", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(page)!;
        var rot = renderer.GetBoneRotation(0);
        Assert.InRange(rot.X, 9.9f, 10.1f);
    }
}
