using System;
using System.Collections.Generic;
using System.Numerics;
using MiniMikuDance;
using MiniMikuDance.Import;
using MiniMikuDance.UI;

namespace MiniMikuDanceMaui.Renderers.Pmx;

public partial class PmxRenderer
{
    private int _selectedBone = -1;
    private IntPtr _modelHandle = IntPtr.Zero;

    public IntPtr ModelHandle
    {
        get => _modelHandle;
        set => _modelHandle = value;
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
        UpdateBoneCache(index);
        return _bones[index].Transform;
    }

    public void SetBoneMatrix(int index, Matrix4x4 value)
    {
        if (index < 0 || index >= _bones.Count)
            return;
        BoneController.SetTransform(_modelHandle, index, value);
        UpdateBoneCache(index);
        _bonesDirty = true;
        Viewer?.InvalidateSurface();
    }

    public void TranslateSelectedBone(Vector3 delta)
    {
        if (_selectedBone < 0 || _selectedBone >= _bones.Count)
            return;
        BoneController.Translate(_modelHandle, _selectedBone, delta);
        UpdateBoneCache(_selectedBone);
        _bonesDirty = true;
        Viewer?.InvalidateSurface();
    }

    public Quaternion GetBoneRotation(int index)
    {
        if (index < 0 || index >= _bones.Count)
            return Quaternion.Identity;
        return BoneController.GetRotation(_modelHandle, index, _bones[index].Rotation);
    }

    private void UpdateBoneCache(int index)
    {
        if (index < 0 || index >= _bones.Count)
            return;
        _bones[index].Rotation = BoneController.GetRotation(_modelHandle, index, _bones[index].Rotation);
        _bones[index].Transform = BoneController.GetWorldTransform(_modelHandle, index, _bones[index].Transform);
        if (index >= 0 && index < _worldMats.Length)
            _worldMats[index] = _bones[index].Transform;
    }

    public void SetBoneRotation(int index, Quaternion value)
    {
        if (index < 0 || index >= _bones.Count)
            return;
        BoneController.SetRotation(_modelHandle, index, value);
        UpdateBoneCache(index);
        _bonesDirty = true;
        Viewer?.InvalidateSurface();
    }

    public void RotateSelectedBone(Quaternion delta)
    {
        if (_selectedBone < 0 || _selectedBone >= _bones.Count)
            return;
        var current = GetBoneRotation(_selectedBone);
        var next = Quaternion.Normalize(delta * current);
        SetBoneRotation(_selectedBone, next);
    }

    private void UpdateBoneMatricesFromModel()
    {
        if (_modelHandle == IntPtr.Zero || _worldMats.Length == 0)
            return;
        for (int i = 0; i < _bones.Count && i < _worldMats.Length; i++)
        {
            var bone = NanoemBone.nanoemModelGetBoneObject(_modelHandle, i);
            if (bone != IntPtr.Zero)
            {
                NanoemBone.nanoemModelBoneGetTransform(bone, out var m);
                _worldMats[i] = m;
                _bones[i].Transform = m;
                NanoemBone.nanoemModelBoneGetOrientation(bone, out var q);
                _bones[i].Rotation = q;
            }
        }
    }

    partial void InitializeBoneModule()
    {
        RegisterModule(new BoneModule());
    }

    private class BoneModule : PmxRendererModuleBase
    {
    }
}

