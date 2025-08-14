using System.Runtime.InteropServices;

namespace MiniMikuDance;

internal static partial class Nanoem
{
    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoem_emapp_initialize_ik();

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoem_emapp_solve_ik(int boneIndex, float[] position);

    public static void InitializeIk()
    {
        nanoem_emapp_initialize_ik();
    }

    public static void SolveIk(int boneIndex, float[] position)
    {
        nanoem_emapp_solve_ik(boneIndex, position);
    }
}
