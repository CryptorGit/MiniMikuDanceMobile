using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using MiniMikuDance.Util;
using MiniMikuDance.Import;
using MiniMikuDance.Data;
using MMDTools;
using Vector2 = OpenTK.Mathematics.Vector2;
using Vector3 = OpenTK.Mathematics.Vector3;
using Vector4 = OpenTK.Mathematics.Vector4;

namespace MiniMikuDanceMaui;

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

        foreach (var (mName, state) in _morphStates)
        {
            var m = _morphs[mName];
            var mv = state.Weight;
            if (m.Type != MorphType.Bone || MathF.Abs(mv) < 1e-5f)
                continue;
            foreach (var off in m.Objects.OfType<BoneMorphObject>())
            {
                int idx = off.Index;
                if (idx < 0 || idx >= count) continue;
                _boneMorphTranslations[idx] += off.Translation * mv;
                var q = System.Numerics.Quaternion.Slerp(System.Numerics.Quaternion.Identity, off.Rotation, mv);
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

        foreach (var (mName, state) in _morphStates)
        {
            var m = _morphs[mName];
            var mv = state.Weight;
            if (m.Type != MorphType.Material || MathF.Abs(mv) < 1e-5f)
                continue;
            foreach (var off in m.Objects.OfType<MaterialMorphObject>())
            {
                void Apply(RenderMesh mesh)
                {
                    var diff = new Vector4(off.Diffuse.X, off.Diffuse.Y, off.Diffuse.Z, off.Diffuse.W);
                    var spec = new Vector3(off.Specular.X, off.Specular.Y, off.Specular.Z);
                    var edge = new Vector4(off.EdgeColor.X, off.EdgeColor.Y, off.EdgeColor.Z, off.EdgeColor.W);
                    var toon = new Vector3(off.ToonColor.X, off.ToonColor.Y, off.ToonColor.Z);
                    var tex = new Vector4(off.TextureTint.X, off.TextureTint.Y, off.TextureTint.Z, off.TextureTint.W);
                    if (off.CalcMode == MaterialCalcMode.Mul)
                    {
                        mesh.Color *= Vector4.One + diff * mv;
                        mesh.Specular *= Vector3.One + spec * mv;
                        mesh.SpecularPower *= 1f + off.SpecularPower * mv;
                        mesh.EdgeColor *= Vector4.One + edge * mv;
                        mesh.EdgeSize *= 1f + off.EdgeSize * mv;
                        mesh.ToonColor *= Vector3.One + toon * mv;
                        mesh.TextureTint *= Vector4.One + tex * mv;
                    }
                    else
                    {
                        mesh.Color += diff * mv;
                        mesh.Specular += spec * mv;
                        mesh.SpecularPower += off.SpecularPower * mv;
                        mesh.EdgeColor += edge * mv;
                        mesh.EdgeSize += off.EdgeSize * mv;
                        mesh.ToonColor += toon * mv;
                        mesh.TextureTint += tex * mv;
                    }
                }

                if (off.IsAll)
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

        if (!_morphStates.TryGetValue(name, out var state))
            return;

        if (MathF.Abs(state.Weight - value) < 1e-5f)
            return;

        state.Weight = value;

        switch (morph.Type)
        {
            case MorphType.Vertex:
                _morphDirty = true;
                foreach (var off in morph.Objects.OfType<VertexMorphObject>())
                {
                    int vid = off.Index;
                    Vector3 total = Vector3.Zero;
                    var contribs = _vertexMorphOffsets[vid];
                    if (contribs != null)
                    {
                        foreach (var (mName, vec) in contribs)
                        {
                            if (_morphStates.TryGetValue(mName, out var st) && MathF.Abs(st.Weight) >= 1e-5f)
                                total += vec * st.Weight;
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
                foreach (var off in morph.Objects.OfType<GroupMorphObject>())
                {
                    var target = _morphIndexToName.Length > off.MorphIndex ? _morphIndexToName[off.MorphIndex] : null;
                    if (!string.IsNullOrEmpty(target))
                        SetMorph(target, value * off.Rate);
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
                foreach (var off in morph.Objects.OfType<UvMorphObject>())
                {
                    int vid = off.Index;
                    System.Numerics.Vector2 total = System.Numerics.Vector2.Zero;
                    var contribs = _uvMorphOffsets[vid];
                    if (contribs != null)
                    {
                        foreach (var (mName, vec) in contribs)
                        {
                            if (_morphStates.TryGetValue(mName, out var st) && MathF.Abs(st.Weight) >= 1e-5f)
                                total += new System.Numerics.Vector2(vec.X, vec.Y) * st.Weight;
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

    public IReadOnlyList<(Morph Morph, MorphState State)> GetMorphs(MorphCategory category)
    {
        if (_morphsByCategory.TryGetValue(category, out var list))
        {
            var result = new List<(Morph, MorphState)>(list.Count);
            foreach (var m in list)
            {
                if (_morphStates.TryGetValue(m.NameJa, out var state))
                    result.Add((m, state));
            }
            return result;
        }
        return Array.Empty<(Morph, MorphState)>();
    }

    public IEnumerable<(Morph Morph, MorphState State)> GetAllMorphStates()
    {
        foreach (var (name, morph) in _morphs)
        {
            if (_morphStates.TryGetValue(name, out var state))
                yield return (morph, state);
        }
    }

    public void SetMorph(MorphCategory category, string name, float value)
    {
        if (_morphs.TryGetValue(name, out var morph) && morph.Category == category)
        {
            SetMorph(name, value);
        }
    }
}
