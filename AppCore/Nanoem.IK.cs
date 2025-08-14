using System.Runtime.InteropServices;

namespace MiniMikuDance;

internal static partial class Nanoem
{
    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoem_emapp_initialize_ik(int constraintCount);

    // position: input target and output effector position
    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoem_emapp_solve_ik(int constraintIndex, int boneIndex, [In, Out] float[] position);

    public static void InitializeIk(int constraintCount)
    {
        nanoem_emapp_initialize_ik(constraintCount);
    }

    public static void SolveIk(int constraintIndex, int boneIndex, float[] position)
    {
        nanoem_emapp_solve_ik(constraintIndex, boneIndex, position);
    }
}
