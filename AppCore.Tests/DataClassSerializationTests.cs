using System;
using System.IO;
using System.Numerics;
using MiniMikuDance.Import;
using MiniMikuDance.Util;
using Assimp;
using Xunit;

public class DataClassSerializationTests
{
    [Fact]
    public void ModelData_RoundTrip()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        var model = new ModelData();
        model.Mesh = new Mesh("mesh", PrimitiveType.Triangle);
        model.ShadeShift = 0.2f;
        model.ShadeToony = 0.8f;
        model.RimIntensity = 0.5f;
        model.Info = new VrmInfo { Title = "t" };
        model.Bones.Add(new BoneData { Name = "b1", Parent = 0 });
        model.HumanoidBones["hips"] = 1;
        model.HumanoidBoneList.Add(("hips", 1));
        var sub = new SubMeshData { Mesh = new Mesh("sub", PrimitiveType.Triangle) };
        model.SubMeshes.Add(sub);

        JSONUtil.Save(tmp, model);
        var loaded = JSONUtil.Load<ModelData>(tmp);

        Assert.Equal(model.ShadeShift, loaded.ShadeShift);
        Assert.Equal(model.ShadeToony, loaded.ShadeToony);
        Assert.Equal(model.RimIntensity, loaded.RimIntensity);
        Assert.Equal("t", loaded.Info.Title);
        Assert.Single(loaded.SubMeshes);
        Assert.Single(loaded.Bones);
        Assert.Equal("b1", loaded.Bones[0].Name);
        Assert.True(loaded.HumanoidBones.ContainsKey("hips"));
        Assert.Equal(1, loaded.HumanoidBones["hips"]);
        File.Delete(tmp);
    }

    [Fact]
    public void BoneData_RoundTrip()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        var data = new BoneData { Name = "bone", Parent = 5, Translation = new Vector3(1,2,3) };
        JSONUtil.Save(tmp, data);
        var loaded = JSONUtil.Load<BoneData>(tmp);
        Assert.Equal(data.Name, loaded.Name);
        Assert.Equal(data.Parent, loaded.Parent);
        Assert.Equal(data.Translation, loaded.Translation);
        File.Delete(tmp);
    }

    [Fact]
    public void SubMeshData_RoundTrip()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        var data = new SubMeshData
        {
            Mesh = new Mesh("m", PrimitiveType.Triangle),
            TextureBytes = new byte[]{1,2,3},
            TextureWidth = 64,
            TextureHeight = 32
        };
        JSONUtil.Save(tmp, data);
        var loaded = JSONUtil.Load<SubMeshData>(tmp);
        Assert.Equal(data.TextureWidth, loaded.TextureWidth);
        Assert.Equal(data.TextureHeight, loaded.TextureHeight);
        Assert.Equal(data.TextureBytes, loaded.TextureBytes);
        File.Delete(tmp);
    }

    [Fact]
    public void VrmInfo_RoundTrip()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        var data = new VrmInfo { Title = "t", Author = "a", VertexCount = 10 };
        JSONUtil.Save(tmp, data);
        var loaded = JSONUtil.Load<VrmInfo>(tmp);
        Assert.Equal(data.Title, loaded.Title);
        Assert.Equal(data.Author, loaded.Author);
        Assert.Equal(10, loaded.VertexCount);
        File.Delete(tmp);
    }
}
