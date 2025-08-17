using System;
using System.Collections.Generic;
using System.Numerics;
using SharpBgfx;
using Matrix4 = System.Numerics.Matrix4x4;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

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

    private void UpdateVertexBuffers()
    {
        var changed = new Dictionary<RenderMesh, List<int>>();
        CollectChangedVertices(changed);
        foreach (var kv in changed)
        {
            var mesh = kv.Key;
            var indices = kv.Value;
            if (indices.Count == 0 || mesh.VertexBuffer == null)
                continue;

            int i = 0;
            while (i < indices.Count)
            {
                int start = indices[i];
                int end = start;
                i++;
                while (i < indices.Count && indices[i] == end + 1)
                {
                    end = indices[i];
                    i++;
                }
                int count = end - start + 1;
                var verts = new PmxVertex[count];
                for (int j = 0; j < count; j++)
                {
                    int idx = start + j;
                    verts[j] = new PmxVertex
                    {
                        Px = mesh.BaseVertices[idx].X + mesh.VertexOffsets[idx].X,
                        Py = mesh.BaseVertices[idx].Y + mesh.VertexOffsets[idx].Y,
                        Pz = mesh.BaseVertices[idx].Z + mesh.VertexOffsets[idx].Z,
                        Nx = mesh.Normals.Length > idx ? mesh.Normals[idx].X : 0f,
                        Ny = mesh.Normals.Length > idx ? mesh.Normals[idx].Y : 0f,
                        Nz = mesh.Normals.Length > idx ? mesh.Normals[idx].Z : 0f,
                        U = mesh.TexCoords.Length > idx ? mesh.TexCoords[idx].X + mesh.UvOffsets[idx].X : 0f,
                        V = mesh.TexCoords.Length > idx ? mesh.TexCoords[idx].Y + mesh.UvOffsets[idx].Y : 0f
                    };
                }
                Bgfx.UpdateVertexBuffer(mesh.VertexBuffer, start, MemoryBlock.FromArray(verts));
            }
            indices.Clear();
        }
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

        UpdateVertexBuffers();

        Bgfx.SetViewTransform(0, _viewMatrix, _projMatrix);

        foreach (var rm in _meshes)
        {
            if (rm.VertexBuffer == null || rm.IndexBuffer == null)
                continue;

            Bgfx.SetVertexBuffer(0, rm.VertexBuffer);
            Bgfx.SetIndexBuffer(rm.IndexBuffer);
            if (rm.Texture != null && rm.HasTexture)
                Bgfx.SetTexture(0, rm.TextureUniform, rm.Texture);
            Bgfx.SetUniform(rm.ColorUniform, rm.Color);
            Bgfx.SetUniform(rm.SpecularUniform, new Vector4(rm.Specular, rm.SpecularPower));
            Bgfx.SetUniform(rm.EdgeUniform, new Vector4(rm.EdgeColor.X, rm.EdgeColor.Y, rm.EdgeColor.Z, rm.EdgeSize));
            Bgfx.SetUniform(rm.ToonColorUniform, new Vector4(rm.ToonColor, 1f));
            Bgfx.SetUniform(rm.TextureTintUniform, rm.TextureTint);
            Bgfx.SetTransform(_modelTransform);
            Bgfx.Submit(0, _modelProgram ?? _program);
        }

        Bgfx.Touch(0);
        Bgfx.Frame();
    }
}
