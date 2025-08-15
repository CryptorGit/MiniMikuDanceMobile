using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using OpenTK.Mathematics;
using OpenTK.Graphics.ES30;
using GL = OpenTK.Graphics.ES30.GL;
using MiniMikuDance.Util;
using MiniMikuDance.Physics;
using MiniMikuDance.Import;
using Vector2 = OpenTK.Mathematics.Vector2;
using Vector3 = OpenTK.Mathematics.Vector3;
using Vector4 = OpenTK.Mathematics.Vector4;

namespace MiniMikuDanceMaui;

public partial class PmxRenderer
{
    private void EnsureBoneCapacity()
    {
        if (_boneCapacity == _bones.Count)
            return;

        _boneCapacity = _bones.Count;
        _worldMats = new System.Numerics.Matrix4x4[_boneCapacity];
        _skinMats = new System.Numerics.Matrix4x4[_boneCapacity];
        _boneLines = new float[_boneCapacity * 6];

        if (_boneVbo != 0) GL.DeleteBuffer(_boneVbo);
        if (_boneVao != 0) GL.DeleteVertexArray(_boneVao);
        _boneVbo = 0;
        _boneVao = 0;

        if (_boneCapacity > 0)
        {
            _boneVao = GL.GenVertexArray();
            _boneVbo = GL.GenBuffer();
            GL.BindVertexArray(_boneVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _boneVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, _boneLines.Length * sizeof(float), IntPtr.Zero, BufferUsageHint.DynamicDraw);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }
    }

    private void DrawIkBones()
    {
        lock (_ikBonesLock)
        {
            UpdateIkBoneWorldPositions();
            if (_ikBones.Count == 0)
                return;
            EnsureIkBoneMesh();

            GL.Disable(EnableCap.DepthTest);
            GL.Uniform1(_pointSizeLoc, 1f);

            GL.BindVertexArray(_ikBoneVao);
            for (int i = 0; i < _ikBones.Count; i++)
            {
                var ik = _ikBones[i];
                var worldPos = ik.Position.ToOpenTK();
                float scale = _ikBoneScale * _distance;
                if (ik.IsSelected)
                    scale *= 1.4f;
                var mat = Matrix4.CreateScale(scale) * Matrix4.CreateTranslation(worldPos);
                GL.UniformMatrix4(_modelLoc, false, ref mat);
                var color = ik.IsSelected ? new Vector4(1f, 0f, 0f, 1f) : new Vector4(0f, 1f, 0f, 1f);
                GL.Uniform4(_colorLoc, color);
                GL.DrawElements(PrimitiveType.Triangles, _ikBoneIndexCount, DrawElementsType.UnsignedShort, 0);
            }
            GL.BindVertexArray(0);
            GL.Enable(EnableCap.DepthTest);
        }
    }

    public void Render()
    {
        if (_physicsWorld != null)
        {
            _physicsWorld.Step(1f / 60f);
            foreach (var body in _physicsWorld.RigidBodies)
            {
                if (body.TransformType != RigidBodyTransformType.FromSimulationToBone)
                    continue;
                int index = body.BoneIndex;
                if (index < 0 || index >= _bones.Count)
                    continue;
                var bone = _bones[index];
                var delta = body.Position - bone.Translation;
                while (_boneTranslations.Count <= index)
                    _boneTranslations.Add(Vector3.Zero);
                _boneTranslations[index] = delta.ToOpenTK();
            }
            _bonesDirty = true;
        }

        UpdateViewProjection();

        bool needsUpdate = _bonesDirty || _morphDirty || _uvMorphDirty;
        if (needsUpdate)
        {
            if (_bones.Count > 0)
                CpuSkinning();
            UpdateVertexBuffers();
        }

        DrawScene();
    }

    private void UpdateViewProjection()
    {
        if (!_viewProjDirty)
            return;

        _cameraRot = Matrix4.CreateFromQuaternion(_externalRotation) *
                     Matrix4.CreateRotationX(_orbitX) *
                     Matrix4.CreateRotationY(_orbitY);
        _cameraPos = Vector3.TransformPosition(new Vector3(0, 0, _distance), _cameraRot) + _target;
        _viewMatrix = Matrix4.LookAt(_cameraPos, _target, Vector3.UnitY);
        float aspect = _width == 0 || _height == 0 ? 1f : _width / (float)_height;
        _projMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspect, 0.1f, 100f);
        _viewProjDirty = false;
    }

    private void CpuSkinning()
    {
        if (_bones.Count == 0)
            return;

        EnsureBoneCapacity();

        if (_worldMats.Length != _bones.Count)
            _worldMats = new System.Numerics.Matrix4x4[_bones.Count];
        else
            Array.Clear(_worldMats, 0, _worldMats.Length);

        if (_skinMats.Length != _bones.Count)
            _skinMats = new System.Numerics.Matrix4x4[_bones.Count];
        else
            Array.Clear(_skinMats, 0, _skinMats.Length);

        for (int i = 0; i < _bones.Count; i++)
        {
            var bone = _bones[i];
            System.Numerics.Vector3 euler = i < _boneRotations.Count ? _boneRotations[i].ToNumerics() : System.Numerics.Vector3.Zero;
            var delta = euler.FromEulerDegrees();
            System.Numerics.Quaternion morphRot = i < _boneMorphRotations.Length ? _boneMorphRotations[i] : System.Numerics.Quaternion.Identity;
            System.Numerics.Vector3 trans = bone.Translation;
            if (i < _boneMorphTranslations.Length)
                trans += _boneMorphTranslations[i];
            if (i < _boneTranslations.Count)
                trans += _boneTranslations[i].ToNumerics();
            var rot = bone.Rotation * morphRot * delta;
            if (bone.InheritParent >= 0 && bone.InheritParent < _worldMats.Length)
            {
                var src = _worldMats[bone.InheritParent];
                if (bone.InheritRotation)
                {
                    var srcRot = System.Numerics.Quaternion.CreateFromRotationMatrix(src);
                    rot = System.Numerics.Quaternion.Normalize(System.Numerics.Quaternion.Slerp(System.Numerics.Quaternion.Identity, srcRot, bone.InheritRatio) * rot);
                }
                if (bone.InheritTranslation)
                    trans += src.Translation * bone.InheritRatio;
            }
            var local = System.Numerics.Matrix4x4.CreateFromQuaternion(rot) * System.Numerics.Matrix4x4.CreateTranslation(trans);
            if (bone.Parent >= 0)
                _worldMats[i] = local * _worldMats[bone.Parent];
            else
                _worldMats[i] = local;
        }

        for (int i = 0; i < _bones.Count; i++)
            _skinMats[i] = _bones[i].InverseBindMatrix * _worldMats[i];

        UpdateIkBoneWorldPositions();

        if (ShowBoneOutline)
        {
            int lineIdx = 0;
            for (int i = 0; i < _bones.Count; i++)
            {
                var bone = _bones[i];
                if (bone.Parent >= 0)
                {
                    if (!_showAllBones && (_physicsBoneIndices.Contains(i) || _physicsBoneIndices.Contains(bone.Parent)))
                        continue;
                    var pp = _worldMats[bone.Parent].Translation;
                    var cp = _worldMats[i].Translation;
                    _boneLines[lineIdx++] = pp.X; _boneLines[lineIdx++] = pp.Y; _boneLines[lineIdx++] = pp.Z;
                    _boneLines[lineIdx++] = cp.X; _boneLines[lineIdx++] = cp.Y; _boneLines[lineIdx++] = cp.Z;
                }
            }
            _boneVertexCount = lineIdx / 3;
            if (_boneVertexCount > 0)
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, _boneVbo);
                var handle = System.Runtime.InteropServices.GCHandle.Alloc(_boneLines, System.Runtime.InteropServices.GCHandleType.Pinned);
                try
                {
                    GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, lineIdx * sizeof(float), handle.AddrOfPinnedObject());
                }
                finally
                {
                    handle.Free();
                }
            }
        }
        else
        {
            _boneVertexCount = 0;
        }
    }

    private void UpdateVertexBuffers()
    {
        bool needsUpdate = _bonesDirty || _morphDirty || _uvMorphDirty;
        if (!needsUpdate)
            return;

        List<int>? changedVerts = null;
        if (_morphDirty || _uvMorphDirty)
        {
            lock (_changedVerticesLock)
            {
                if (_changedOriginalVertices.Count > 0)
                {
                    changedVerts = _changedVerticesList;
                    changedVerts.Clear();
                    changedVerts.EnsureCapacity(_changedOriginalVertices.Count);
                    foreach (var idx in _changedOriginalVertices)
                        changedVerts.Add(idx);
                    _changedOriginalVertices.Clear();
                }
            }
        }

        if (_bones.Count > 0)
        {
            if (_bonesDirty)
            {
                float[]? tmpVertexBuffer = null;
                try
                {
                    foreach (var rm in _meshes)
                    {
                        if (rm.JointIndices.Length != rm.BaseVertices.Length)
                            continue;
                        int required = rm.BaseVertices.Length * 8;
                        if (tmpVertexBuffer == null || tmpVertexBuffer.Length < required)
                        {
                            if (tmpVertexBuffer != null)
                                ArrayPool<float>.Shared.Return(tmpVertexBuffer);
                            tmpVertexBuffer = ArrayPool<float>.Shared.Rent(required);
                        }
                        else
                        {
                            Array.Clear(tmpVertexBuffer, 0, required);
                        }
                        for (int vi = 0; vi < rm.BaseVertices.Length; vi++)
                        {
                            var pos = System.Numerics.Vector3.Zero;
                            var norm = System.Numerics.Vector3.Zero;
                            var jp = rm.JointIndices[vi];
                            var jw = rm.JointWeights[vi];
                            var basePos = (rm.BaseVertices[vi] + rm.VertexOffsets[vi]).ToNumerics();
                            bool useSdef = rm.SdefC.Length > vi && rm.SdefR0.Length > vi && rm.SdefR1.Length > vi &&
                                (rm.SdefC[vi] != Vector3.Zero || rm.SdefR0[vi] != Vector3.Zero || rm.SdefR1[vi] != Vector3.Zero);
                            int loop = useSdef ? 2 : 4;
                            for (int k = 0; k < loop; k++)
                            {
                                int bi = (int)jp[k];
                                float w = jw[k];
                                if (bi >= 0 && bi < _skinMats.Length && w > 0f)
                                {
                                    var m = _skinMats[bi];
                                    pos += System.Numerics.Vector3.Transform(basePos, m) * w;
                                    norm += System.Numerics.Vector3.TransformNormal(rm.Normals[vi].ToNumerics(), m) * w;
                                }
                            }
                            if (useSdef)
                            {
                                int b0 = (int)jp[0];
                                int b1 = (int)jp[1];
                                float w0 = jw[0];
                                float w1 = jw[1];
                                var c = rm.SdefC[vi].ToNumerics();
                                var r0 = rm.SdefR0[vi].ToNumerics();
                                var r1 = rm.SdefR1[vi].ToNumerics();
                                var m0 = _skinMats[b0];
                                var m1 = _skinMats[b1];
                                var c0 = System.Numerics.Vector3.Transform(c, m0);
                                var c1 = System.Numerics.Vector3.Transform(c, m1);
                                var r0t = System.Numerics.Vector3.Transform(r0, m0) - c0;
                                var r1t = System.Numerics.Vector3.Transform(r1, m1) - c1;
                                var cMix = c0 * w0 + c1 * w1;
                                var rMix = r0t * w0 + r1t * w1;
                                var q0 = System.Numerics.Quaternion.CreateFromRotationMatrix(m0);
                                var q1 = System.Numerics.Quaternion.CreateFromRotationMatrix(m1);
                                var q = System.Numerics.Quaternion.Slerp(q0, q1, w1);
                                var rotMat = System.Numerics.Matrix4x4.CreateFromQuaternion(q);
                                var local = basePos - c;
                                pos = System.Numerics.Vector3.Transform(local, rotMat) + cMix + rMix;
                                var nLocal = rm.Normals[vi].ToNumerics();
                                norm = System.Numerics.Vector3.TransformNormal(nLocal, rotMat);
                            }
                            if (norm.LengthSquared() > 0)
                                norm = System.Numerics.Vector3.Normalize(norm);

                            tmpVertexBuffer[vi * 8 + 0] = pos.X;
                            tmpVertexBuffer[vi * 8 + 1] = pos.Y;
                            tmpVertexBuffer[vi * 8 + 2] = pos.Z;
                            tmpVertexBuffer[vi * 8 + 3] = norm.X;
                            tmpVertexBuffer[vi * 8 + 4] = norm.Y;
                            tmpVertexBuffer[vi * 8 + 5] = norm.Z;
                            if (vi < rm.TexCoords.Length)
                            {
                                var uv = rm.TexCoords[vi];
                                if (vi < rm.UvOffsets.Length)
                                    uv += rm.UvOffsets[vi];
                                tmpVertexBuffer[vi * 8 + 6] = uv.X;
                                tmpVertexBuffer[vi * 8 + 7] = uv.Y;
                            }
                            else
                            {
                                tmpVertexBuffer[vi * 8 + 6] = 0f;
                                tmpVertexBuffer[vi * 8 + 7] = 0f;
                            }
                        }
                        GL.BindBuffer(BufferTarget.ArrayBuffer, rm.Vbo);
                        var handle = System.Runtime.InteropServices.GCHandle.Alloc(tmpVertexBuffer, System.Runtime.InteropServices.GCHandleType.Pinned);
                        try
                        {
                            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, required * sizeof(float), handle.AddrOfPinnedObject());
                        }
                        finally
                        {
                            handle.Free();
                        }
                    }
                }
                finally
                {
                    if (tmpVertexBuffer != null)
                        ArrayPool<float>.Shared.Return(tmpVertexBuffer);
                }
            }
            else if ((_morphDirty || _uvMorphDirty) && changedVerts != null)
            {
                var small = new float[8];
                var handleSmall = System.Runtime.InteropServices.GCHandle.Alloc(small, System.Runtime.InteropServices.GCHandleType.Pinned);
                try
                {
                    var span = CollectionsMarshal.AsSpan(changedVerts);
                    for (int ci = 0; ci < span.Length; ci++)
                    {
                        var origIdx = span[ci];
                        var mapped = _morphVertexMap[origIdx];
                        if (mapped == null) continue;
                        foreach (var (rm, vi) in mapped)
                        {
                            var pos = System.Numerics.Vector3.Zero;
                            var norm = System.Numerics.Vector3.Zero;
                            var jp = rm.JointIndices[vi];
                            var jw = rm.JointWeights[vi];
                            var basePos = (rm.BaseVertices[vi] + rm.VertexOffsets[vi]).ToNumerics();
                            bool useSdef = rm.SdefC.Length > vi && rm.SdefR0.Length > vi && rm.SdefR1.Length > vi &&
                                (rm.SdefC[vi] != Vector3.Zero || rm.SdefR0[vi] != Vector3.Zero || rm.SdefR1[vi] != Vector3.Zero);
                            int loop = useSdef ? 2 : 4;
                            for (int k = 0; k < loop; k++)
                            {
                                int bi = (int)jp[k];
                                float w = jw[k];
                                if (bi >= 0 && bi < _skinMats.Length && w > 0f)
                                {
                                    var m = _skinMats[bi];
                                    pos += System.Numerics.Vector3.Transform(basePos, m) * w;
                                    norm += System.Numerics.Vector3.TransformNormal(rm.Normals[vi].ToNumerics(), m) * w;
                                }
                            }
                            if (useSdef)
                            {
                                int b0 = (int)jp[0];
                                int b1 = (int)jp[1];
                                float w0 = jw[0];
                                float w1 = jw[1];
                                var c = rm.SdefC[vi].ToNumerics();
                                var r0 = rm.SdefR0[vi].ToNumerics();
                                var r1 = rm.SdefR1[vi].ToNumerics();
                                var m0 = _skinMats[b0];
                                var m1 = _skinMats[b1];
                                var c0 = System.Numerics.Vector3.Transform(c, m0);
                                var c1 = System.Numerics.Vector3.Transform(c, m1);
                                var r0t = System.Numerics.Vector3.Transform(r0, m0) - c0;
                                var r1t = System.Numerics.Vector3.Transform(r1, m1) - c1;
                                var cMix = c0 * w0 + c1 * w1;
                                var rMix = r0t * w0 + r1t * w1;
                                var q0 = System.Numerics.Quaternion.CreateFromRotationMatrix(m0);
                                var q1 = System.Numerics.Quaternion.CreateFromRotationMatrix(m1);
                                var q = System.Numerics.Quaternion.Slerp(q0, q1, w1);
                                var rotMat = System.Numerics.Matrix4x4.CreateFromQuaternion(q);
                                var local = basePos - c;
                                pos = System.Numerics.Vector3.Transform(local, rotMat) + cMix + rMix;
                                var nLocal = rm.Normals[vi].ToNumerics();
                                norm = System.Numerics.Vector3.TransformNormal(nLocal, rotMat);
                            }
                            if (norm.LengthSquared() > 0)
                                norm = System.Numerics.Vector3.Normalize(norm);

                            small[0] = pos.X; small[1] = pos.Y; small[2] = pos.Z;
                            small[3] = norm.X; small[4] = norm.Y; small[5] = norm.Z;
                            if (vi < rm.TexCoords.Length)
                            {
                                var uv = rm.TexCoords[vi];
                                if (vi < rm.UvOffsets.Length)
                                    uv += rm.UvOffsets[vi];
                                small[6] = uv.X; small[7] = uv.Y;
                            }
                            else { small[6] = 0f; small[7] = 0f; }

                            GL.BindBuffer(BufferTarget.ArrayBuffer, rm.Vbo);
                            IntPtr offset = new IntPtr(vi * 8 * sizeof(float));
                            GL.BufferSubData(BufferTarget.ArrayBuffer, offset, 8 * sizeof(float), handleSmall.AddrOfPinnedObject());
                        }
                    }
                }
                finally
                {
                    handleSmall.Free();
                }
            }
        }
        else
        {
            float[]? tmpVertexBuffer = null;
            try
            {
                foreach (var rm in _meshes)
                {
                    int required = rm.BaseVertices.Length * 8;
                    if (tmpVertexBuffer == null || tmpVertexBuffer.Length < required)
                    {
                        if (tmpVertexBuffer != null)
                            ArrayPool<float>.Shared.Return(tmpVertexBuffer);
                        tmpVertexBuffer = ArrayPool<float>.Shared.Rent(required);
                    }
                    else
                    {
                        Array.Clear(tmpVertexBuffer, 0, required);
                    }
                    for (int vi = 0; vi < rm.BaseVertices.Length; vi++)
                    {
                        var pos = rm.BaseVertices[vi] + rm.VertexOffsets[vi];
                        var nor = vi < rm.Normals.Length ? rm.Normals[vi] : new Vector3(0, 0, 1);
                        tmpVertexBuffer[vi * 8 + 0] = pos.X;
                        tmpVertexBuffer[vi * 8 + 1] = pos.Y;
                        tmpVertexBuffer[vi * 8 + 2] = pos.Z;
                        tmpVertexBuffer[vi * 8 + 3] = nor.X;
                        tmpVertexBuffer[vi * 8 + 4] = nor.Y;
                        tmpVertexBuffer[vi * 8 + 5] = nor.Z;
                        if (vi < rm.TexCoords.Length)
                        {
                            var uv = rm.TexCoords[vi];
                            if (vi < rm.UvOffsets.Length)
                                uv += rm.UvOffsets[vi];
                            tmpVertexBuffer[vi * 8 + 6] = uv.X;
                            tmpVertexBuffer[vi * 8 + 7] = uv.Y;
                        }
                        else
                        {
                            tmpVertexBuffer[vi * 8 + 6] = 0f;
                            tmpVertexBuffer[vi * 8 + 7] = 0f;
                        }
                    }
                    GL.BindBuffer(BufferTarget.ArrayBuffer, rm.Vbo);
                    var handle = System.Runtime.InteropServices.GCHandle.Alloc(tmpVertexBuffer, System.Runtime.InteropServices.GCHandleType.Pinned);
                    try
                    {
                        GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, required * sizeof(float), handle.AddrOfPinnedObject());
                    }
                    finally
                    {
                        handle.Free();
                    }
                }
            }
            finally
            {
                if (tmpVertexBuffer != null)
                    ArrayPool<float>.Shared.Return(tmpVertexBuffer);
            }
        }

        _bonesDirty = false;
        _morphDirty = false;
        _uvMorphDirty = false;
    }

    private void DrawScene()
    {
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
        GL.Enable(EnableCap.Blend); // 半透明描画のためブレンドを有効化
        GL.FrontFace(FrontFaceDirection.Ccw);
        GL.ClearColor(1f, 1f, 1f, 1f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        var modelMat = ModelTransform;

        GL.UseProgram(_modelProgram);
        GL.UniformMatrix4(_modelViewLoc, false, ref _viewMatrix);
        GL.UniformMatrix4(_modelProjLoc, false, ref _projMatrix);
        Vector3 light = Vector3.Normalize(new Vector3(0.3f, 0.6f, -0.7f));
        light = Vector3.TransformNormal(light, _cameraRot);
        GL.Uniform3(_modelLightDirLoc, ref light);
        Vector3 viewDir = Vector3.Normalize(_target - _cameraPos);
        GL.Uniform3(_modelViewDirLoc, ref viewDir);
        GL.Uniform1(_modelShadeShiftLoc, ShadeShift);
        GL.Uniform1(_modelShadeToonyLoc, ShadeToony);
        GL.Uniform1(_modelRimIntensityLoc, RimIntensity);
        GL.Uniform1(_modelAmbientLoc, Ambient);
        GL.UniformMatrix4(_modelMatrixLoc, false, ref modelMat);
        foreach (var rm in _meshes)
        {
            GL.Uniform4(_modelColorLoc, rm.Color);
            GL.Uniform3(_modelSpecularLoc, rm.Specular);
            GL.Uniform1(_modelSpecularPowerLoc, rm.SpecularPower);
            GL.Uniform4(_modelEdgeColorLoc, rm.EdgeColor);
            GL.Uniform1(_modelEdgeSizeLoc, rm.EdgeSize);
            GL.Uniform3(_modelToonColorLoc, rm.ToonColor);
            GL.Uniform4(_modelTexTintLoc, rm.TextureTint);
            if (rm.HasTexture)
            {
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, rm.Texture);
                GL.Uniform1(_modelTexLoc, 0);
                GL.Uniform1(_modelUseTexLoc, 1);
            }
            else
            {
                GL.Uniform1(_modelUseTexLoc, 0);
            }
            GL.BindVertexArray(rm.Vao);
            GL.DrawElements(PrimitiveType.Triangles, rm.IndexCount, DrawElementsType.UnsignedInt, IntPtr.Zero);
            GL.BindVertexArray(0);
            if (rm.HasTexture)
            {
                GL.BindTexture(TextureTarget.Texture2D, 0);
            }
        }
        GL.UseProgram(_program);
        GL.UniformMatrix4(_viewLoc, false, ref _viewMatrix);
        GL.UniformMatrix4(_projLoc, false, ref _projMatrix);

        Matrix4 gridModel = Matrix4.Identity;
        GL.DepthMask(false);
        GL.UniformMatrix4(_modelLoc, false, ref gridModel);
        GL.Uniform4(_colorLoc, new Vector4(1f, 1f, 1f, 0.3f));
        GL.BindVertexArray(_groundVao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, _groundVertexCount);
        GL.BindVertexArray(0);

        GL.UniformMatrix4(_modelLoc, false, ref gridModel);
        GL.Uniform4(_colorLoc, new Vector4(0.8f, 0.8f, 0.8f, 0.5f));
        GL.BindVertexArray(_gridVao);
        GL.DrawArrays(PrimitiveType.Lines, 0, _gridVertexCount);
        GL.BindVertexArray(0);
        GL.DepthMask(true);

        if (ShowBoneOutline && _boneVertexCount > 0)
        {
            GL.Disable(EnableCap.DepthTest);
            GL.UniformMatrix4(_modelLoc, false, ref modelMat);
            GL.Uniform4(_colorLoc, new Vector4(1f, 0f, 0f, 1f));
            GL.BindVertexArray(_boneVao);
            GL.DrawArrays(PrimitiveType.Lines, 0, _boneVertexCount);
            GL.BindVertexArray(0);
            GL.Enable(EnableCap.DepthTest);
        }

        if (ShowIkBones)
        {
            DrawIkBones();
        }
    }

}
