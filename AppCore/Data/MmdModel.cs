using System;
using System.Collections.Generic;
using MiniMikuDance.Import;

namespace MiniMikuDance.Data;

public class MmdModel
{
    public List<BoneData> Bones { get; } = new();

    public List<IkChain> IkChains { get; } = new();

    public List<FootIkChain> FootIkChains { get; } = new();

    public List<RigidBodyData> RigidBodies { get; } = new();

    public List<JointData> Joints { get; } = new();

    public static int NormalizeIndex(int index, int count, Action<string>? logger = null)
    {
        if (index < 0 || index >= count)
        {
            logger?.Invoke($"Index {index} out of range (0..{count - 1}).");
            return -1;
        }
        return index;
    }
}

