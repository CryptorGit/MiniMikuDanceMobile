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
        _bones[index].Rotation = BoneController.GetRotation(_modelHandle, index, _bones[index].Rotation);
        return BoneController.GetTransform(_modelHandle, index, _bones[index].Transform);
    }

    public void SetBoneMatrix(int index, Matrix4x4 value)
    {
        if (index < 0 || index >= _bones.Count)
            return;
        BoneController.SetTransform(_modelHandle, index, value);
        _bones[index].Transform = value;
        _bones[index].Rotation = Quaternion.CreateFromRotationMatrix(value);
        _bonesDirty = true;
        Viewer?.InvalidateSurface();
    }

    public void TranslateSelectedBone(Vector3 delta)
    {
        if (_selectedBone < 0 || _selectedBone >= _bones.Count)
            return;
        BoneController.Translate(_modelHandle, _selectedBone, delta);
        _bones[_selectedBone].Transform = BoneController.GetTransform(_modelHandle, _selectedBone, _bones[_selectedBone].Transform);
        _bones[_selectedBone].Rotation = BoneController.GetRotation(_modelHandle, _selectedBone, _bones[_selectedBone].Rotation);
        _bonesDirty = true;
        Viewer?.InvalidateSurface();
    }

    public Quaternion GetBoneRotation(int index)
    {
        if (index < 0 || index >= _bones.Count)
            return Quaternion.Identity;
        return BoneController.GetRotation(_modelHandle, index, _bones[index].Rotation);
    }

    public void SetBoneRotation(int index, Quaternion value)
    {
        if (index < 0 || index >= _bones.Count)
            return;
        BoneController.SetRotation(_modelHandle, index, value);
        _bones[index].Rotation = value;
        _bones[index].Transform = BoneController.GetTransform(_modelHandle, index, _bones[index].Transform);
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

    partial void InitializeBoneModule()
    {
        RegisterModule(new BoneModule());
    }

    private class BoneModule : PmxRendererModuleBase
    {
    }
}

