using System;
using System.Numerics;
using MiniMikuDance.Import;

namespace MiniMikuDance.Physics;

public static class PhysicsHelper
{
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
        // V = πr^2h + (4/3)πr^3
        var cyl = MathF.PI * r * r * h;
        var sph = 4f / 3f * MathF.PI * r * r * r;
        return (cyl + sph) * density;
    }

    private static Vector3 ComputeCapsuleInertia(Vector3 size, float mass)
    {
        var r = size.X * 0.5f;
        var h = size.Y;
        // 質量は円柱と半球2個の体積 V_cyl=πr^2h, V_sph=4/3πr^3 に基づき分配し、
        // 半球の慣性モーメントは I_parallel=2/5 m r^2, I_perp=83/320 m r^2 を使用
        var cylV = MathF.PI * r * r * h;
        var sphV = 4f / 3f * MathF.PI * r * r * r;
        var totalV = cylV + sphV;
        var massCyl = mass * (cylV / totalV);
        var massHemi = (mass - massCyl) * 0.5f;

        var cylIxz = massCyl / 12f * (3f * r * r + h * h);
        var cylIy = 0.5f * massCyl * r * r;

        const float hemiIxzCoeff = 83f / 320f;
        const float hemiIyCoeff = 2f / 5f;
        var offset = h * 0.5f + 3f * r / 8f;
        var hemiIxz = hemiIxzCoeff * massHemi * r * r + massHemi * offset * offset;
        var hemiIy = hemiIyCoeff * massHemi * r * r;

        var ixz = cylIxz + 2f * hemiIxz;
        var iy = cylIy + 2f * hemiIy;
        return new Vector3(ixz, iy, ixz);
    }
}
