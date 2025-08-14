using System;
using System.Runtime.InteropServices;

namespace MiniMikuDance;

internal static class NanoemPhysics
{
    private const string NativeLibName = "nanoem";
    private static IntPtr _world;

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr nanoemPhysicsRigidBodyCreate(IntPtr value, IntPtr opaque, out int status);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemPhysicsRigidBodyDestroy(IntPtr rigidBody);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemPhysicsWorldAddRigidBody(IntPtr world, IntPtr rigidBody);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemPhysicsWorldRemoveRigidBody(IntPtr world, IntPtr rigidBody);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr nanoemPhysicsJointCreate(IntPtr value, IntPtr opaque, out int status);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemPhysicsJointDestroy(IntPtr joint);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemPhysicsWorldAddJoint(IntPtr world, IntPtr joint);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemPhysicsWorldRemoveJoint(IntPtr world, IntPtr joint);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemPhysicsWorldReset(IntPtr world);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemPhysicsWorldSetPreferredFPS(IntPtr world, int value);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemPhysicsWorldSetActive(IntPtr world, bool value);

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

    public static IntPtr CreateRigidBody(IntPtr value)
    {
        return nanoemPhysicsRigidBodyCreate(value, IntPtr.Zero, out _);
    }

    public static void DestroyRigidBody(IntPtr rigidBody)
    {
        if (rigidBody != IntPtr.Zero)
        {
            nanoemPhysicsRigidBodyDestroy(rigidBody);
        }
    }

    public static void AddRigidBody(IntPtr rigidBody)
    {
        if (_world != IntPtr.Zero && rigidBody != IntPtr.Zero)
        {
            nanoemPhysicsWorldAddRigidBody(_world, rigidBody);
        }
    }

    public static void RemoveRigidBody(IntPtr rigidBody)
    {
        if (_world != IntPtr.Zero && rigidBody != IntPtr.Zero)
        {
            nanoemPhysicsWorldRemoveRigidBody(_world, rigidBody);
        }
    }

    public static IntPtr CreateJoint(IntPtr value)
    {
        return nanoemPhysicsJointCreate(value, IntPtr.Zero, out _);
    }

    public static void DestroyJoint(IntPtr joint)
    {
        if (joint != IntPtr.Zero)
        {
            nanoemPhysicsJointDestroy(joint);
        }
    }

    public static void AddJoint(IntPtr joint)
    {
        if (_world != IntPtr.Zero && joint != IntPtr.Zero)
        {
            nanoemPhysicsWorldAddJoint(_world, joint);
        }
    }

    public static void RemoveJoint(IntPtr joint)
    {
        if (_world != IntPtr.Zero && joint != IntPtr.Zero)
        {
            nanoemPhysicsWorldRemoveJoint(_world, joint);
        }
    }

    public static void Reset()
    {
        if (_world != IntPtr.Zero)
        {
            nanoemPhysicsWorldReset(_world);
        }
    }

    public static void SetPreferredFPS(int value)
    {
        if (_world != IntPtr.Zero)
        {
            nanoemPhysicsWorldSetPreferredFPS(_world, value);
        }
    }

    public static void SetActive(bool value)
    {
        if (_world != IntPtr.Zero)
        {
            nanoemPhysicsWorldSetActive(_world, value);
        }
    }
}
