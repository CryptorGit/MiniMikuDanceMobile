using System.Numerics;
using System.Runtime.InteropServices;

namespace MiniMikuDance.IK;

internal static class NativeMethods
{
    [DllImport("nanoem_emapp", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void nanoem_emapp_solve_ik(int boneIndex, ref Vector3 position);
}

