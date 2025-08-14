using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace MiniMikuDance.Import;

public class IkConstraintData
{
    public int TargetBoneIndex { get; set; }
    public float AngleLimit { get; set; }
    public int Iterations { get; set; }
    public List<Link> Links { get; } = new();

    public class Link
    {
        public int BoneIndex { get; set; }
        public bool HasLimit { get; set; }
        public Vector3 LowerLimit { get; set; }
        public Vector3 UpperLimit { get; set; }
    }
}

public partial class ModelImporter
{
    private static IkConstraintData? ReadIkConstraint(IntPtr model, int boneIndex)
    {
        var info = Nanoem.ModelGetIKConstraintInfo(model, (uint) boneIndex);
        try
        {
            if (info.TargetBoneIndex < 0 || info.LinkCount == 0)
            {
                return null;
            }
            var result = new IkConstraintData
            {
                TargetBoneIndex = info.TargetBoneIndex,
                AngleLimit = info.AngleLimit,
                Iterations = info.Iterations
            };
            int linkSize = Marshal.SizeOf<Nanoem.IKConstraintLink>();
            for (int i = 0; i < info.LinkCount; i++)
            {
                var ptr = IntPtr.Add(info.Links, i * linkSize);
                var link = Marshal.PtrToStructure<Nanoem.IKConstraintLink>(ptr);
                result.Links.Add(new IkConstraintData.Link
                {
                    BoneIndex = link.BoneIndex,
                    HasLimit = link.HasLimit != 0,
                    LowerLimit = new Vector3(link.LowerLimitX, link.LowerLimitY, link.LowerLimitZ),
                    UpperLimit = new Vector3(link.UpperLimitX, link.UpperLimitY, link.UpperLimitZ)
                });
            }
            return result;
        }
        finally
        {
            if (info.Links != IntPtr.Zero)
            {
                Nanoem.ModelIOFree(info.Links);
            }
        }
    }

    private void LoadIkConstraints(IntPtr model, ModelData data)
    {
        for (int i = 0; i < data.Bones.Count; i++)
        {
            var constraint = ReadIkConstraint(model, i);
            if (constraint != null)
            {
                data.Bones[i].IkConstraint = constraint;
            }
        }
    }
}
