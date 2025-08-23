using System.Numerics;
using System.Collections.Generic;
using System.Reflection;
using BepuPhysics;
using MiniMikuDance.Physics;
using MiniMikuDance.Import;
using Xunit;

public class BepuPhysicsWorldTests
{
    private static BepuPhysicsWorld CreateWorld()
    {
        var world = new BepuPhysicsWorld();
        var config = new PhysicsConfig(new Vector3(0, -9.8f, 0), 1, 1);
        world.Initialize(config, 1f);
        return world;
    }

    private static int GetRigidBodyCount(BepuPhysicsWorld world)
    {
        var field = typeof(BepuPhysicsWorld).GetField("_rigidBodyHandles", BindingFlags.NonPublic | BindingFlags.Instance);
        var list = (List<BodyHandle>)field!.GetValue(world)!;
        return list.Count;
    }

    [Fact]
    public void InvalidMass_IsSkipped()
    {
        var model = new ModelData();
        model.RigidBodies.Add(new RigidBodyData
        {
            Name = "invalid",
            Mass = -1f,
            Shape = RigidBodyShape.Sphere,
            Size = new Vector3(1f),
            Mode = 1
        });

        var world = CreateWorld();
        world.LoadRigidBodies(model);

        Assert.Equal(0, GetRigidBodyCount(world));
    }

    [Fact]
    public void InvalidSize_IsSkipped()
    {
        var model = new ModelData();
        model.RigidBodies.Add(new RigidBodyData
        {
            Name = "invalid",
            Mass = 1f,
            Shape = RigidBodyShape.Box,
            Size = new Vector3(1f, -1f, 1f),
            Mode = 1
        });

        var world = CreateWorld();
        world.LoadRigidBodies(model);

        Assert.Equal(0, GetRigidBodyCount(world));
    }
}
