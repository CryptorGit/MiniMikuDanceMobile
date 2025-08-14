using System.Numerics;
using System.Runtime.InteropServices;

namespace MiniMikuDance.IK;

internal static class NativeMethods
{
    [DllImport("nanoem_emapp", EntryPoint = "nanoem_emapp_solve_ik", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void SolveIk(int boneIndex, ref Vector3 position);
}

