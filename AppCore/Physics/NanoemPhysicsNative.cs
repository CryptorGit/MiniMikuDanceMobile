namespace MiniMikuDance.Physics;

using System;
using System.Runtime.InteropServices;

/// <summary>
/// nanoem の物理演算ラッパー。
/// </summary>
internal static class NanoemPhysicsNative
{
    private const string LibraryName = "nanoem";

    [DllImport(LibraryName, EntryPoint = "nanoemPhysicsWorldIsAvailable", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool PhysicsWorldIsAvailable(nint opaque);

    [DllImport(LibraryName, EntryPoint = "nanoemPhysicsWorldCreate", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint PhysicsWorldCreate(nint opaque, out NanoemStatus status);

    [DllImport(LibraryName, EntryPoint = "nanoemPhysicsWorldAddRigidBody", CallingConvention = CallingConvention.Cdecl)]
    public static extern void PhysicsWorldAddRigidBody(nint world, nint rigidBody);

    [DllImport(LibraryName, EntryPoint = "nanoemPhysicsWorldAddSoftBody", CallingConvention = CallingConvention.Cdecl)]
    public static extern void PhysicsWorldAddSoftBody(nint world, nint softBody);

    [DllImport(LibraryName, EntryPoint = "nanoemPhysicsWorldAddJoint", CallingConvention = CallingConvention.Cdecl)]
    public static extern void PhysicsWorldAddJoint(nint world, nint joint);

    [DllImport(LibraryName, EntryPoint = "nanoemPhysicsWorldRemoveRigidBody", CallingConvention = CallingConvention.Cdecl)]
    public static extern void PhysicsWorldRemoveRigidBody(nint world, nint rigidBody);

    [DllImport(LibraryName, EntryPoint = "nanoemPhysicsWorldRemoveSoftBody", CallingConvention = CallingConvention.Cdecl)]
    public static extern void PhysicsWorldRemoveSoftBody(nint world, nint softBody);

    [DllImport(LibraryName, EntryPoint = "nanoemPhysicsWorldRemoveJoint", CallingConvention = CallingConvention.Cdecl)]
    public static extern void PhysicsWorldRemoveJoint(nint world, nint joint);

    [DllImport(LibraryName, EntryPoint = "nanoemPhysicsWorldSetPreferredFPS", CallingConvention = CallingConvention.Cdecl)]
    public static extern void PhysicsWorldSetPreferredFPS(nint world, int value);

    [DllImport(LibraryName, EntryPoint = "nanoemPhysicsWorldStepSimulation", CallingConvention = CallingConvention.Cdecl)]
    public static extern int PhysicsWorldStepSimulation(nint world, float delta);

    [DllImport(LibraryName, EntryPoint = "nanoemPhysicsWorldReset", CallingConvention = CallingConvention.Cdecl)]
    public static extern void PhysicsWorldReset(nint world);

    [DllImport(LibraryName, EntryPoint = "nanoemPhysicsWorldGetGravity", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint PhysicsWorldGetGravity(nint world);

    [DllImport(LibraryName, EntryPoint = "nanoemPhysicsWorldSetGravity", CallingConvention = CallingConvention.Cdecl)]
    public static extern void PhysicsWorldSetGravity(nint world, float[] value);

    [DllImport(LibraryName, EntryPoint = "nanoemPhysicsWorldIsActive", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool PhysicsWorldIsActive(nint world);

    [DllImport(LibraryName, EntryPoint = "nanoemPhysicsWorldSetActive", CallingConvention = CallingConvention.Cdecl)]
    public static extern void PhysicsWorldSetActive(nint world, [MarshalAs(UnmanagedType.I1)] bool value);

    [DllImport(LibraryName, EntryPoint = "nanoemPhysicsWorldDestroy", CallingConvention = CallingConvention.Cdecl)]
    public static extern void PhysicsWorldDestroy(nint world);

    [DllImport(LibraryName, EntryPoint = "nanoemPhysicsRigidBodyCreate", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint PhysicsRigidBodyCreate(nint rigidBody, nint opaque, out NanoemStatus status);

    [DllImport(LibraryName, EntryPoint = "nanoemPhysicsRigidBodyDestroy", CallingConvention = CallingConvention.Cdecl)]
    public static extern void PhysicsRigidBodyDestroy(nint rigidBody);

    [DllImport(LibraryName, EntryPoint = "nanoemPhysicsRigidBodyGetWorldTransform", CallingConvention = CallingConvention.Cdecl)]
    public static extern void PhysicsRigidBodyGetWorldTransform(nint rigidBody, [Out] float[] value);

    [DllImport(LibraryName, EntryPoint = "nanoemPhysicsJointCreate", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint PhysicsJointCreate(nint joint, nint opaque, out NanoemStatus status);

    [DllImport(LibraryName, EntryPoint = "nanoemPhysicsJointDestroy", CallingConvention = CallingConvention.Cdecl)]
    public static extern void PhysicsJointDestroy(nint joint);

    /// <summary>
    /// ポインタが null の場合に <see cref="InvalidOperationException"/> を投げる。
    /// </summary>
    public static nint ThrowIfNull(nint ptr, string? message = null)
    {
        if (ptr == nint.Zero)
            throw new InvalidOperationException(message ?? "native 関数が null を返しました");
        return ptr;
    }

    /// <summary>
    /// ステータスが成功以外の場合に <see cref="InvalidOperationException"/> を投げる。
    /// </summary>
    public static void ThrowIfError(NanoemStatus status, string? message = null)
    {
        if (status != NanoemStatus.Success)
            throw new InvalidOperationException(message ?? $"native 関数が失敗しました: {status}");
    }

    internal enum NanoemStatus
    {
        Unknown = -1,
        Success = 0,
    }
}

