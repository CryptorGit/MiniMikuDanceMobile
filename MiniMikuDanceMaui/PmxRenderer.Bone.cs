using System;
using System.Collections.Generic;
using System.Numerics;
using MiniMikuDance;
using MiniMikuDance.Import;
using MiniMikuDance.UI;

namespace MiniMikuDanceMaui;

public partial class PmxRenderer
{
    private int _selectedBone = -1;
    private IntPtr _modelHandle = IntPtr.Zero;

    public IntPtr ModelHandle
    {
        get => _modelHandle;
        set
        {
            _modelHandle = value;
            UpdateBonesFromNanoem();
        }
    }

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
        return BoneController.GetTransform(_modelHandle, index, _bones[index].Transform);
    }

    public void SetBoneMatrix(int index, Matrix4x4 value)
    {
        if (index < 0 || index >= _bones.Count)
            return;
        BoneController.SetTransform(_modelHandle, index, value);
        _bones[index].Transform = value;
        _bonesDirty = true;
        Viewer?.InvalidateSurface();
    }

    public void TranslateSelectedBone(Vector3 delta)
    {
        if (_selectedBone < 0 || _selectedBone >= _bones.Count)
            return;
        BoneController.Translate(_modelHandle, _selectedBone, delta);
        _bones[_selectedBone].Transform = BoneController.GetTransform(_modelHandle, _selectedBone, _bones[_selectedBone].Transform);
        _bonesDirty = true;
        Viewer?.InvalidateSurface();
    }

    private void UpdateBonesFromNanoem()
    {
        if (_modelHandle == IntPtr.Zero || _bones.Count == 0)
            return;
        NanoemBone.nanoemModelBoneUpdateAll(_modelHandle);
        for (int i = 0; i < _bones.Count; i++)
        {
            var bonePtr = NanoemBone.nanoemModelGetBoneObject(_modelHandle, i);
            if (bonePtr != IntPtr.Zero)
            {
                NanoemBone.nanoemModelBoneGetTransformMatrix(bonePtr, out var m);
                _bones[i].Transform = m;
            }
        }
        _bonesDirty = true;
    }
}

