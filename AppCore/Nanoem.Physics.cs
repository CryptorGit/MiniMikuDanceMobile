using System;
using System.Runtime.InteropServices;

namespace MiniMikuDance;

internal static class NanoemPhysics
{
    private const string NativeLibName = "nanoem";
    private static IntPtr _world;

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr nanoemPhysicsWorldCreate(IntPtr opaque, out int status);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemPhysicsWorldDestroy(IntPtr world);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemPhysicsWorldStepSimulation(IntPtr world, float delta);

    public static void Start()
    {
        _world = nanoemPhysicsWorldCreate(IntPtr.Zero, out _);
    }

    public static void Stop()
    {
        if (_world != IntPtr.Zero)
        {
            nanoemPhysicsWorldDestroy(_world);
            _world = IntPtr.Zero;
        }
    }

    public static void Step(float delta)
    {
        if (_world != IntPtr.Zero)
        {
            nanoemPhysicsWorldStepSimulation(_world, delta);
        }
    }
}
