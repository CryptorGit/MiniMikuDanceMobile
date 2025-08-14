using System;

namespace MiniMikuDance.Import;

public partial class ModelImporter
{
    partial void LoadIK(IntPtr model, ModelData data)
    {
        uint count = Nanoem.ModelGetIKConstraintCount(model);
        for (uint i = 0; i < count; i++)
        {
            var info = Nanoem.ModelGetIKConstraintInfo(model, i);
            data.IkBoneIndices.Add(info.EffectorBoneIndex);
        }
    }
}
