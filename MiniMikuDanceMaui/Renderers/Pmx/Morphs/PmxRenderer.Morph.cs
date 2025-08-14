using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using MiniMikuDance.Morph;
using OpenTK.Graphics.ES30;
using Vector2 = OpenTK.Mathematics.Vector2;
using Vector3 = OpenTK.Mathematics.Vector3;
using Vector4 = OpenTK.Mathematics.Vector4;
using NumericsVector3 = System.Numerics.Vector3;
using NumericsQuaternion = System.Numerics.Quaternion;
using GL = OpenTK.Graphics.ES30.GL;

namespace MiniMikuDanceMaui.Renderers.Pmx;

public partial class PmxRenderer
{

    public void SetMorph(string name, float value)
    {
        if (!_morphs.TryGetValue(name, out var morph))
            return;

        value = Math.Clamp(value, 0f, 1f);
        if (MathF.Abs(value) < 1e-5f) value = 0f;

        _morphValues.TryGetValue(name, out var current);
        if (MathF.Abs(current - value) < 1e-5f)
            return;

        if (value == 0f)
            _morphValues.Remove(name);
        else
            _morphValues[name] = value;

        if (_modelHandle != IntPtr.Zero)
        {
            var pairs = new (int, float)[_morphValues.Count];
            int i = 0;
            foreach (var (mName, mv) in _morphValues)
            {
                pairs[i++] = (_morphs[mName].Index, mv);
            }
            MorphController.SetWeight(_modelHandle, pairs);
            UpdateMorphResults();
        }

        _morphDirty = true;
        _uvMorphDirty = true;
        _bonesDirty = true;
        Viewer?.InvalidateSurface();
    }

    public IReadOnlyList<MorphData> GetMorphs(MorphCategory category)
    {
        return _morphsByCategory.TryGetValue(category, out var list) ? list : Array.Empty<MorphData>();
    }

    public void SetMorph(MorphCategory category, string name, float value)
    {
        if (_morphs.TryGetValue(name, out var morph) && morph.Category == category)
        {
            SetMorph(name, value);
        }
    }

    partial void InitializeMorphModule()
    {
        RegisterModule(new MorphModule());
    }

    private class MorphModule : PmxRendererModuleBase
    {
    }

    private void UpdateMorphResults()
    {
        if (_modelHandle == IntPtr.Zero)
            return;

        foreach (var rm in _meshes)
        {
            Array.Clear(rm.VertexOffsets, 0, rm.VertexOffsets.Length);
            Array.Clear(rm.UvOffsets, 0, rm.UvOffsets.Length);
        }

        lock (_changedVerticesLock)
            _changedOriginalVertices.Clear();

        foreach (var (mName, mv) in _morphValues)
        {
            int idx = _morphs[mName].Index;
            var vo = NanoemMorph.GetVertexOffsets(_modelHandle, idx);
            foreach (var off in vo)
            {
                var mapped = _morphVertexMap[off.VertexIndex];
                if (mapped == null) continue;
                var vec = new Vector3(off.X, off.Y, off.Z) * mv;
                foreach (var (rm, vi) in mapped)
                {
                    rm.VertexOffsets[vi] += vec;
                }
                lock (_changedVerticesLock)
                    _changedOriginalVertices.Add(off.VertexIndex);
            }

            var uo = NanoemMorph.GetUvOffsets(_modelHandle, idx);
            foreach (var off in uo)
            {
                var mapped = _morphVertexMap[off.VertexIndex];
                if (mapped == null) continue;
                var vec = new Vector2(off.X, off.Y) * mv;
                foreach (var (rm, vi) in mapped)
                {
                    if (vi < rm.UvOffsets.Length)
                        rm.UvOffsets[vi] += vec;
                }
                lock (_changedVerticesLock)
                    _changedOriginalVertices.Add(off.VertexIndex);
            }
        }

        RecalculateMaterialMorphs();
        UploadMaterialMorphUniforms();
        RecalculateBoneMorphs();
        UpdateBoneMatricesFromModel();
        UploadChangedVertices();
    }

    private void RecalculateMaterialMorphs()
    {
        foreach (var rm in _meshes)
        {
            rm.Color = rm.BaseColor;
            rm.Specular = rm.BaseSpecular;
            rm.SpecularPower = rm.BaseSpecularPower;
            rm.EdgeColor = rm.BaseEdgeColor;
            rm.EdgeSize = rm.BaseEdgeSize;
            rm.ToonColor = rm.BaseToonColor;
            rm.TextureTint = rm.BaseTextureTint;
        }

        if (_modelHandle == IntPtr.Zero)
            return;

        foreach (var (mName, mv) in _morphValues)
        {
            int idx = _morphs[mName].Index;
            var offs = NanoemMorph.GetMaterialOffsets(_modelHandle, idx);
            foreach (var off in offs)
            {
                if (off.MaterialIndex < 0 || off.MaterialIndex >= _meshes.Count)
                    continue;
                var rm = _meshes[off.MaterialIndex];
                if (off.OperationType == 0)
                {
                    rm.Color *= new Vector4(1f + off.DiffuseR * mv, 1f + off.DiffuseG * mv, 1f + off.DiffuseB * mv, 1f + off.DiffuseA * mv);
                    rm.Specular *= new Vector3(1f + off.SpecularR * mv, 1f + off.SpecularG * mv, 1f + off.SpecularB * mv);
                    rm.EdgeColor *= new Vector4(1f + off.EdgeColorR * mv, 1f + off.EdgeColorG * mv, 1f + off.EdgeColorB * mv, 1f + off.EdgeColorA * mv);
                    rm.SpecularPower *= 1f + off.SpecularPower * mv;
                    rm.EdgeSize *= 1f + off.EdgeSize * mv;
                }
                else
                {
                    rm.Color += new Vector4(off.DiffuseR, off.DiffuseG, off.DiffuseB, off.DiffuseA) * mv;
                    rm.Specular += new Vector3(off.SpecularR, off.SpecularG, off.SpecularB) * mv;
                    rm.EdgeColor += new Vector4(off.EdgeColorR, off.EdgeColorG, off.EdgeColorB, off.EdgeColorA) * mv;
                    rm.SpecularPower += off.SpecularPower * mv;
                    rm.EdgeSize += off.EdgeSize * mv;
                }
                rm.TextureTint += new Vector4(off.TextureBlendR, off.TextureBlendG, off.TextureBlendB, off.TextureBlendA) * mv;
                rm.ToonColor += new Vector3(off.ToonTextureBlendR, off.ToonTextureBlendG, off.ToonTextureBlendB) * mv;
            }
        }

    }

    private void UploadMaterialMorphUniforms()
    {
        if (_modelProgram == 0)
            return;

        GL.UseProgram(_modelProgram);
        foreach (var rm in _meshes)
        {
            GL.Uniform4(_modelColorLoc, rm.Color);
            GL.Uniform3(_modelSpecularLoc, rm.Specular);
            GL.Uniform1(_modelSpecularPowerLoc, rm.SpecularPower);
            GL.Uniform4(_modelEdgeColorLoc, rm.EdgeColor);
            GL.Uniform1(_modelEdgeSizeLoc, rm.EdgeSize);
            GL.Uniform3(_modelToonColorLoc, rm.ToonColor);
            GL.Uniform4(_modelTexTintLoc, rm.TextureTint);
        }
    }

    private void UploadChangedVertices()
    {
        List<int>? changedVerts;
        lock (_changedVerticesLock)
        {
            if (_changedOriginalVertices.Count == 0)
                return;
            changedVerts = _changedVerticesList;
            changedVerts.Clear();
            changedVerts.EnsureCapacity(_changedOriginalVertices.Count);
            foreach (var idx in _changedOriginalVertices)
                changedVerts.Add(idx);
            _changedOriginalVertices.Clear();
        }

        var small = new float[8];
        var handleSmall = GCHandle.Alloc(small, GCHandleType.Pinned);
        try
        {
            foreach (var origIdx in changedVerts)
            {
                var mapped = _morphVertexMap[origIdx];
                if (mapped == null) continue;
                foreach (var (rm, vi) in mapped)
                {
                    var pos = rm.BaseVertices[vi] + rm.VertexOffsets[vi];
                    var nor = vi < rm.Normals.Length ? rm.Normals[vi] : new Vector3(0, 0, 1);
                    Vector2 uv = vi < rm.TexCoords.Length ? rm.TexCoords[vi] : Vector2.Zero;
                    if (vi < rm.UvOffsets.Length)
                        uv += rm.UvOffsets[vi];

                    small[0] = pos.X; small[1] = pos.Y; small[2] = pos.Z;
                    small[3] = nor.X; small[4] = nor.Y; small[5] = nor.Z;
                    small[6] = uv.X; small[7] = uv.Y;

                    GL.BindBuffer(BufferTarget.ArrayBuffer, rm.Vbo);
                    IntPtr offset = new IntPtr(vi * 8 * sizeof(float));
                    GL.BufferSubData(BufferTarget.ArrayBuffer, offset, 8 * sizeof(float), handleSmall.AddrOfPinnedObject());
                }
            }
        }
        finally
        {
            handleSmall.Free();
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }
    }

    private void RecalculateBoneMorphs()
    {
        if (_boneMorphTranslations.Length < _bones.Count)
            Array.Resize(ref _boneMorphTranslations, _bones.Count);
        if (_boneMorphRotations.Length < _bones.Count)
            Array.Resize(ref _boneMorphRotations, _bones.Count);

        Array.Clear(_boneMorphTranslations, 0, _boneMorphTranslations.Length);
        for (int i = 0; i < _boneMorphRotations.Length; i++)
            _boneMorphRotations[i] = NumericsQuaternion.Identity;

        if (_modelHandle == IntPtr.Zero)
            return;

        foreach (var (mName, mv) in _morphValues)
        {
            int idx = _morphs[mName].Index;
            var offs = NanoemMorph.GetBoneOffsets(_modelHandle, idx);
            foreach (var off in offs)
            {
                int bi = off.BoneIndex;
                if (bi < 0 || bi >= _boneMorphTranslations.Length)
                    continue;
                _boneMorphTranslations[bi] += new NumericsVector3(off.TranslationX, off.TranslationY, off.TranslationZ) * mv;
                var q = new NumericsQuaternion(off.OrientationX, off.OrientationY, off.OrientationZ, off.OrientationW);
                q = NumericsQuaternion.Slerp(NumericsQuaternion.Identity, q, mv);
                _boneMorphRotations[bi] = NumericsQuaternion.Normalize(_boneMorphRotations[bi] * q);
            }
        }
    }
}
