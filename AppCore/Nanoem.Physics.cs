using System;
using System.Runtime.InteropServices;

namespace MiniMikuDance;

internal static class NanoemPhysics
{
    private const string NativeLibName = "nanoem";
    private static IntPtr _world;

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr nanoemEmappPhysicsRigidBodyCreate(IntPtr value, IntPtr world, out int status);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemEmappPhysicsRigidBodyDestroy(IntPtr rigidBody);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemEmappPhysicsWorldAddRigidBody(IntPtr world, IntPtr rigidBody);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemEmappPhysicsWorldRemoveRigidBody(IntPtr world, IntPtr rigidBody);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr nanoemEmappPhysicsJointCreate(IntPtr value, IntPtr world, out int status);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemEmappPhysicsJointDestroy(IntPtr joint);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemEmappPhysicsWorldAddJoint(IntPtr world, IntPtr joint);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemEmappPhysicsWorldRemoveJoint(IntPtr world, IntPtr joint);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemEmappPhysicsWorldReset(IntPtr world);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemEmappPhysicsWorldSetPreferredFPS(IntPtr world, int value);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemEmappPhysicsWorldSetActive(IntPtr world, bool value);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr nanoemEmappPhysicsWorldCreate(IntPtr opaque, out int status);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemEmappPhysicsWorldDestroy(IntPtr world);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemEmappPhysicsWorldStepSimulation(IntPtr world, float delta);

    public static void Start()
    {
        _world = nanoemEmappPhysicsWorldCreate(IntPtr.Zero, out _);
    }

    public static void Stop()
    {
        if (_world != IntPtr.Zero)
        {
            nanoemEmappPhysicsWorldDestroy(_world);
            _world = IntPtr.Zero;
        }
    }

    public static void Step(float delta)
    {
        if (_world != IntPtr.Zero)
        {
            nanoemEmappPhysicsWorldStepSimulation(_world, delta);
        }
    }

    public static IntPtr CreateRigidBody(IntPtr value)
    {
        return nanoemEmappPhysicsRigidBodyCreate(value, _world, out _);
    }

    public static void DestroyRigidBody(IntPtr rigidBody)
    {
        if (rigidBody != IntPtr.Zero)
        {
            nanoemEmappPhysicsRigidBodyDestroy(rigidBody);
        }
    }

    public static void AddRigidBody(IntPtr rigidBody)
    {
        if (_world != IntPtr.Zero && rigidBody != IntPtr.Zero)
        {
            nanoemEmappPhysicsWorldAddRigidBody(_world, rigidBody);
        }
    }

    public static void RemoveRigidBody(IntPtr rigidBody)
    {
        if (_world != IntPtr.Zero && rigidBody != IntPtr.Zero)
        {
            nanoemEmappPhysicsWorldRemoveRigidBody(_world, rigidBody);
        }
    }

    public static IntPtr CreateJoint(IntPtr value)
    {
        return nanoemEmappPhysicsJointCreate(value, _world, out _);
    }

    public static void DestroyJoint(IntPtr joint)
    {
        if (joint != IntPtr.Zero)
        {
            nanoemEmappPhysicsJointDestroy(joint);
        }
    }

    public static void AddJoint(IntPtr joint)
    {
        if (_world != IntPtr.Zero && joint != IntPtr.Zero)
        {
            nanoemEmappPhysicsWorldAddJoint(_world, joint);
        }
    }

    public static void RemoveJoint(IntPtr joint)
    {
        if (_world != IntPtr.Zero && joint != IntPtr.Zero)
        {
            nanoemEmappPhysicsWorldRemoveJoint(_world, joint);
        }
    }

    public static void Reset()
    {
        if (_world != IntPtr.Zero)
        {
            nanoemEmappPhysicsWorldReset(_world);
        }
    }

    public static void SetPreferredFPS(int value)
    {
        if (_world != IntPtr.Zero)
        {
            nanoemEmappPhysicsWorldSetPreferredFPS(_world, value);
        }
    }

    public static void SetActive(bool value)
    {
        if (_world != IntPtr.Zero)
        {
            nanoemEmappPhysicsWorldSetActive(_world, value);
        }
    }
}
