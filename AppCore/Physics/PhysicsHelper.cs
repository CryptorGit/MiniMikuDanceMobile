using System;
using System.Numerics;
using MiniMikuDance.Import;

namespace MiniMikuDance.Physics;

public static class PhysicsHelper
{
    /// <summary>
    /// Compute mass and inertia tensor from density.
    /// Use this only when the material density is known.
    /// If the mass is already given, call <see cref="ComputeInertiaFromMass"/> instead.
    /// </summary>
    public static (float mass, Vector3 inertia) Compute(RigidBodyShape shape, Vector3 size, float density)
    {
        var mass = shape switch
        {
            RigidBodyShape.Sphere => ComputeSphereMass(size, density),
            RigidBodyShape.Box => ComputeBoxMass(size, density),
            RigidBodyShape.Capsule => ComputeCapsuleMass(size, density),
            _ => density
        };
        var inertia = ComputeInertia(shape, size, mass);
        return (mass, inertia);
    }

    /// <summary>
    /// Compute only the inertia tensor from mass.
    /// </summary>
    public static Vector3 ComputeInertiaFromMass(RigidBodyShape shape, Vector3 size, float mass)
        => ComputeInertia(shape, size, mass);

    public static Vector3 ComputeInertia(RigidBodyShape shape, Vector3 size, float mass)
        => shape switch
        {
            RigidBodyShape.Sphere => ComputeSphereInertia(size, mass),
            RigidBodyShape.Box => ComputeBoxInertia(size, mass),
            RigidBodyShape.Capsule => ComputeCapsuleInertia(size, mass),
            _ => Vector3.One * mass
        };

    private static float ComputeSphereMass(Vector3 size, float density)
    {
        var r = size.X * 0.5f;
        return 4f / 3f * MathF.PI * r * r * r * density;
    }

    private static Vector3 ComputeSphereInertia(Vector3 size, float mass)
    {
        var r = size.X * 0.5f;
        var i = 0.4f * mass * r * r;
        return new Vector3(i);
    }

    private static float ComputeBoxMass(Vector3 size, float density)
        => size.X * size.Y * size.Z * density;

    private static Vector3 ComputeBoxInertia(Vector3 size, float mass)
    {
        var x2 = size.X * size.X;
        var y2 = size.Y * size.Y;
        var z2 = size.Z * size.Z;
        var coeff = mass / 12f;
        return new Vector3(coeff * (y2 + z2), coeff * (x2 + z2), coeff * (x2 + y2));
    }

    private static float ComputeCapsuleMass(Vector3 size, float density)
    {
        var r = size.X * 0.5f;
        var h = size.Y;
        var cyl = MathF.PI * r * r * h;
        return cyl * density;
    }

    private static Vector3 ComputeCapsuleInertia(Vector3 size, float mass)
    {
        var r = size.X * 0.5f;
        var h = size.Y;
        var ixz = mass / 12f * (3f * r * r + h * h);
        var iy = 0.5f * mass * r * r;
        return new Vector3(ixz, iy, ixz);
    }
}
