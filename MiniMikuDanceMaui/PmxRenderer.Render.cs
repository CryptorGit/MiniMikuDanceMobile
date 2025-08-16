using System;
using System.Numerics;
using SharpBgfx;
using Matrix4 = System.Numerics.Matrix4x4;
using Vector3 = System.Numerics.Vector3;

namespace MiniMikuDanceMaui;

public partial class PmxRenderer
{
    private void EnsureBoneCapacity()
    {
        if (_boneCapacity == _bones.Count)
            return;

        _boneCapacity = _bones.Count;
        _worldMats = new Matrix4[_boneCapacity];
        _skinMats = new Matrix4[_boneCapacity];
        _boneLines = new float[_boneCapacity * 6];
    }

    private void UpdateViewProjection()
    {
        if (!_viewProjDirty)
            return;

        _cameraRot = Matrix4.CreateFromQuaternion(_externalRotation) *
                     Matrix4.CreateRotationX(_orbitX) *
                     Matrix4.CreateRotationY(_orbitY);
        _cameraPos = Vector3.Transform(new Vector3(0, 0, _distance), _cameraRot) + _target;
        _viewMatrix = Matrix4.CreateLookAt(_cameraPos, _target, Vector3.UnitY);
        float aspect = _width == 0 || _height == 0 ? 1f : _width / (float)_height;
        _projMatrix = Matrix4.CreatePerspectiveFieldOfView(MathF.PI / 4f, aspect, 0.1f, 100f);
        _viewProjDirty = false;
    }

    private void CpuSkinning()
    {
        if (_bones.Count == 0)
            return;

        EnsureBoneCapacity();

        Array.Clear(_worldMats, 0, _worldMats.Length);
        Array.Clear(_skinMats, 0, _skinMats.Length);

        for (int i = 0; i < _bones.Count; i++)
        {
            var bone = _bones[i];
            Vector3 euler = i < _boneRotations.Count ? _boneRotations[i] : Vector3.Zero;
            var delta = euler.FromEulerDegrees();
            Quaternion morphRot = i < _boneMorphRotations.Length ? _boneMorphRotations[i] : Quaternion.Identity;
            Vector3 trans = bone.Translation;
            if (i < _boneMorphTranslations.Length)
                trans += _boneMorphTranslations[i];
            if (i < _boneTranslations.Count)
                trans += _boneTranslations[i];
            var rot = bone.Rotation * morphRot * delta;
            Matrix4 local = Matrix4.CreateFromQuaternion(rot) * Matrix4.CreateTranslation(trans);
            if (bone.Parent >= 0)
                _worldMats[i] = local * _worldMats[bone.Parent];
            else
                _worldMats[i] = local;
            _skinMats[i] = bone.InverseBindMatrix * _worldMats[i];
        }

        UpdateIkBoneWorldPositions();
    }

    public void Render()
    {
        UpdateViewProjection();

        bool needsUpdate = _bonesDirty || _morphDirty || _uvMorphDirty;
        if (needsUpdate)
        {
            if (_bones.Count > 0)
                CpuSkinning();
            _bonesDirty = false;
            _morphDirty = false;
            _uvMorphDirty = false;
        }

        Bgfx.Touch(0);
        Bgfx.Frame();
    }
}
