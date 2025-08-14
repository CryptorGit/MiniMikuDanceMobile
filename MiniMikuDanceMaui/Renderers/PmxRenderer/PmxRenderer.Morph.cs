using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using MiniMikuDance.Util;
using MiniMikuDance.Import;
using MiniMikuDance;
using MiniMikuDance.Morph;
using MMDTools;
using Vector2 = OpenTK.Mathematics.Vector2;
using Vector3 = OpenTK.Mathematics.Vector3;
using Vector4 = OpenTK.Mathematics.Vector4;

namespace MiniMikuDanceMaui.Renderers;

public partial class PmxRenderer
{
    private void RecalculateBoneMorphs()
    {
        int count = _bones.Count;

        if (_boneMorphTranslations.Length != count)
        {
            _boneMorphTranslations = new System.Numerics.Vector3[count];
        }
        else
        {
            Array.Clear(_boneMorphTranslations, 0, count);
        }

        if (_boneMorphRotations.Length != count)
        {
            _boneMorphRotations = new System.Numerics.Quaternion[count];
        }

        Array.Fill(_boneMorphRotations, System.Numerics.Quaternion.Identity);

        foreach (var (mName, mv) in _morphValues)
        {
            var m = _morphs[mName];
            if (m.Type != MorphType.Bone || MathF.Abs(mv) < 1e-5f)
                continue;
            foreach (var off in m.Offsets)
            {
                int idx = off.Index;
                if (idx < 0 || idx >= count) continue;
                _boneMorphTranslations[idx] += off.Bone.Translation * mv;
                var q = System.Numerics.Quaternion.Slerp(System.Numerics.Quaternion.Identity, off.Bone.Rotation, mv);
                _boneMorphRotations[idx] = System.Numerics.Quaternion.Normalize(_boneMorphRotations[idx] * q);
            }
        }
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

        foreach (var (mName, mv) in _morphValues)
        {
            var m = _morphs[mName];
            if (m.Type != MorphType.Material || MathF.Abs(mv) < 1e-5f)
                continue;
            foreach (var off in m.Offsets)
            {
                void Apply(RenderMesh mesh)
                {
                    var diff = new Vector4(off.Material.Diffuse.X, off.Material.Diffuse.Y, off.Material.Diffuse.Z, off.Material.Diffuse.W);
                    var spec = new Vector3(off.Material.Specular.X, off.Material.Specular.Y, off.Material.Specular.Z);
                    var edge = new Vector4(off.Material.EdgeColor.X, off.Material.EdgeColor.Y, off.Material.EdgeColor.Z, off.Material.EdgeColor.W);
                    var toon = new Vector3(off.Material.ToonColor.X, off.Material.ToonColor.Y, off.Material.ToonColor.Z);
                    var tex = new Vector4(off.Material.TextureTint.X, off.Material.TextureTint.Y, off.Material.TextureTint.Z, off.Material.TextureTint.W);
                    if (off.Material.CalcMode == MaterialCalcMode.Mul)
                    {
                        mesh.Color *= Vector4.One + diff * mv;
                        mesh.Specular *= Vector3.One + spec * mv;
                        mesh.SpecularPower *= 1f + off.Material.SpecularPower * mv;
                        mesh.EdgeColor *= Vector4.One + edge * mv;
                        mesh.EdgeSize *= 1f + off.Material.EdgeSize * mv;
                        mesh.ToonColor *= Vector3.One + toon * mv;
                        mesh.TextureTint *= Vector4.One + tex * mv;
                    }
                    else
                    {
                        mesh.Color += diff * mv;
                        mesh.Specular += spec * mv;
                        mesh.SpecularPower += off.Material.SpecularPower * mv;
                        mesh.EdgeColor += edge * mv;
                        mesh.EdgeSize += off.Material.EdgeSize * mv;
                        mesh.ToonColor += toon * mv;
                        mesh.TextureTint += tex * mv;
                    }
                }

                if (off.Material.IsAll)
                {
                    foreach (var mesh in _meshes)
                        Apply(mesh);
                }
                else if (off.Index >= 0 && off.Index < _meshes.Count)
                {
                    Apply(_meshes[off.Index]);
                }
            }
        }
    }

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
            MorphController.SetWeight(_modelHandle, morph.Index, value);
        }

        switch (morph.Type)
        {
            case MorphType.Vertex:
                _morphDirty = true;
                foreach (var off in morph.Offsets)
                {
                    int vid = off.Index;
                    Vector3 total = Vector3.Zero;
                    var contribs = _vertexMorphOffsets[vid];
                    if (contribs != null)
                    {
                        foreach (var (mName, vec) in contribs)
                        {
                            if (_morphValues.TryGetValue(mName, out var mv) && MathF.Abs(mv) >= 1e-5f)
                                total += vec * mv;
                        }
                    }
                    _vertexTotalOffsets[vid] = total;
                    lock (_changedVerticesLock)
                        _changedOriginalVertices.Add(vid);
                    var list = _morphVertexMap[vid];
                    if (list != null)
                    {
                        foreach (var (mesh, idx) in list)
                            mesh.VertexOffsets[idx] = total;
                    }
                }
                break;
            case MorphType.Group:
                foreach (var off in morph.Offsets)
                {
                    var target = _morphIndexToName.Length > off.Group.MorphIndex ? _morphIndexToName[off.Group.MorphIndex] : null;
                    if (!string.IsNullOrEmpty(target))
                        SetMorph(target, value * off.Group.Rate);
                }
                break;
            case MorphType.Bone:
                RecalculateBoneMorphs();
                _bonesDirty = true;
                break;
            case MorphType.Material:
                RecalculateMaterialMorphs();
                _morphDirty = true;
                break;
            case MorphType.UV:
                _uvMorphDirty = true;
                foreach (var off in morph.Offsets)
                {
                    int vid = off.Index;
                    System.Numerics.Vector2 total = System.Numerics.Vector2.Zero;
                    var contribs = _uvMorphOffsets[vid];
                    if (contribs != null)
                    {
                        foreach (var (mName, vec) in contribs)
                        {
                            if (_morphValues.TryGetValue(mName, out var mv) && MathF.Abs(mv) >= 1e-5f)
                                total += new System.Numerics.Vector2(vec.X, vec.Y) * mv;
                        }
                    }
                    lock (_changedVerticesLock)
                        _changedOriginalVertices.Add(vid);
                    var list = _morphVertexMap[vid];
                    if (list != null)
                    {
                        foreach (var (mesh, idx) in list)
                            mesh.UvOffsets[idx] = new Vector2(total.X, total.Y);
                    }
                }
                break;
            default:
                break;
        }

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
}
