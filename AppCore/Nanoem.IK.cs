using System.Collections.Generic;
using System.Runtime.InteropServices;
using MiniMikuDance.IK;
using MiniMikuDance.Import;

namespace MiniMikuDance;

internal static partial class Nanoem
{
    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoem_emapp_initialize_ik([In] IKConstraintInfo[] infos, int count);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoem_emapp_solve_ik(int constraintIndex, [In, Out] float[] position);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoem_emapp_reset_ik();

    public static void InitializeIk(IReadOnlyList<IkBone> bones)
    {
        var infos = new IKConstraintInfo[bones.Count];
        var allocations = new IntPtr[bones.Count];
        int linkSize = Marshal.SizeOf<IKConstraintLink>();
        for (int i = 0; i < bones.Count; i++)
        {
            var constraint = bones[i].Constraint;
            int linkCount = constraint.Links.Count;
            IntPtr linkPtr = IntPtr.Zero;
            if (linkCount > 0)
            {
                linkPtr = Marshal.AllocHGlobal(linkSize * linkCount);
                for (int j = 0; j < linkCount; j++)
                {
                    var link = constraint.Links[j];
                    var nativeLink = new IKConstraintLink
                    {
                        BoneIndex = link.BoneIndex,
                        HasLimit = link.HasLimit ? 1 : 0,
                        LowerLimitX = link.LowerLimit.X,
                        LowerLimitY = link.LowerLimit.Y,
                        LowerLimitZ = link.LowerLimit.Z,
                        UpperLimitX = link.UpperLimit.X,
                        UpperLimitY = link.UpperLimit.Y,
                        UpperLimitZ = link.UpperLimit.Z
                    };
                    Marshal.StructureToPtr(nativeLink, IntPtr.Add(linkPtr, j * linkSize), false);
                }
            }
            allocations[i] = linkPtr;
            infos[i] = new IKConstraintInfo
            {
                TargetBoneIndex = constraint.TargetBoneIndex,
                AngleLimit = constraint.AngleLimit,
                Iterations = constraint.Iterations,
                LinkCount = linkCount,
                Links = linkPtr
            };
        }
        nanoem_emapp_initialize_ik(infos, infos.Length);
        for (int i = 0; i < allocations.Length; i++)
        {
            if (allocations[i] != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(allocations[i]);
            }
        }
    }

    public static void InitializeIk()
    {
        nanoem_emapp_initialize_ik(System.Array.Empty<IKConstraintInfo>(), 0);
    }

    public static void SolveIk(int constraintIndex, float[] position)
    {
        nanoem_emapp_solve_ik(constraintIndex, position);
    }

    public static void ResetIk()
    {
        nanoem_emapp_reset_ik();
    }
}
