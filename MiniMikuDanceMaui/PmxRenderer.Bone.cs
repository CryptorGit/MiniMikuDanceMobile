using System;
using System.Collections.Generic;
using System.Numerics;
using MiniMikuDance;
using MiniMikuDance.Import;

namespace MiniMikuDanceMaui;

public partial class PmxRenderer
{
    private int _selectedBone = -1;

    public IReadOnlyList<BoneData> GetBones() => _bones;

    public void SelectBone(int index)
    {
        if (index < 0 || index >= _bones.Count)
            return;
        _selectedBone = index;
    }

    public Matrix4x4 GetBoneMatrix(int index)
    {
        if (index < 0 || index >= _bones.Count)
            return Matrix4x4.Identity;
        var bone = NanoemBone.nanoemModelGetBoneObject(IntPtr.Zero, index);
        if (bone != IntPtr.Zero)
        {
            NanoemBone.nanoemModelBoneGetTransformMatrix(bone, out var m);
            return m;
        }
        return _bones[index].Transform;
    }

    public void SetBoneMatrix(int index, Matrix4x4 value)
    {
        if (index < 0 || index >= _bones.Count)
            return;
        var bonePtr = NanoemBone.nanoemModelGetBoneObject(IntPtr.Zero, index);
        if (bonePtr != IntPtr.Zero)
        {
            NanoemBone.nanoemModelBoneSetTransformMatrix(bonePtr, value);
        }
        _bones[index].Transform = value;
        _bonesDirty = true;
        Viewer?.InvalidateSurface();
    }

    public void TranslateSelectedBone(Vector3 delta)
    {
        if (_selectedBone < 0 || _selectedBone >= _bones.Count)
            return;
        var current = GetBoneMatrix(_selectedBone);
        var m = Matrix4x4.CreateTranslation(delta) * current;
        SetBoneMatrix(_selectedBone, m);
    }
}

