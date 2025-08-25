using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using OpenTK.Mathematics;
using OpenTK.Graphics.ES30;
using GL = OpenTK.Graphics.ES30.GL;
using ErrorCode = OpenTK.Graphics.ES30.ErrorCode;
using MiniMikuDance.Util;
using MiniMikuDance.IK;
using Vector2 = OpenTK.Mathematics.Vector2;
using Vector3 = OpenTK.Mathematics.Vector3;
using Vector4 = OpenTK.Mathematics.Vector4;

namespace MiniMikuDanceMaui;

public partial class PmxRenderer
{
    #if DEBUG
    private static bool CheckGLError(string api, string details = "")
    {
        var error = GL.GetError();
        if (error == ErrorCode.NoError)
            return true;
        Console.Error.WriteLine($"GL error {error} after {api}. {details}");
        return false;
    }
    #else
    private static bool CheckGLError(string api, string details = "") => true;
    #endif

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
        UpdateIkBoneWorldPositions();

        IkBone[] iks;
        lock (_ikBonesLock)
        {
            if (_ikBones.Count == 0)
                return;
            iks = _ikBones.ToArray();
        }

        EnsureIkBoneMesh();

        GL.Disable(EnableCap.DepthTest);
#if DEBUG
        if (!CheckGLError("GL.Disable", $"cap={EnableCap.DepthTest}")) return;
#endif
        GL.Uniform1(_pointSizeLoc, 1f);
#if DEBUG
        if (!CheckGLError("GL.Uniform1", $"loc={_pointSizeLoc}")) return;
#endif

        GL.BindVertexArray(_ikBoneVao);
#if DEBUG
        if (!CheckGLError("GL.BindVertexArray", $"vao={_ikBoneVao}")) return;
#endif
        for (int i = 0; i < iks.Length; i++)
        {
            var ik = iks[i];
            var worldPos = ik.Position.ToOpenTK();
            float scale = _ikBoneScale * _distance;
            if (ik.IsSelected)
                scale *= 1.4f;
            var mat = Matrix4.CreateScale(scale) * Matrix4.CreateTranslation(worldPos);
            GL.UniformMatrix4(_modelLoc, false, ref mat);
#if DEBUG
            if (!CheckGLError("GL.UniformMatrix4", $"loc={_modelLoc}")) return;
#endif
            var color = ik.IsSelected ? new Vector4(1f, 0f, 0f, 1f) : new Vector4(0f, 1f, 0f, 1f);
            GL.Uniform4(_colorLoc, color);
#if DEBUG
            if (!CheckGLError("GL.Uniform4", $"loc={_colorLoc}")) return;
#endif
            GL.DrawElements(PrimitiveType.Triangles, _ikBoneIndexCount, DrawElementsType.UnsignedShort, 0);
#if DEBUG
            if (!CheckGLError("GL.DrawElements", $"count={_ikBoneIndexCount}")) return;
#endif
        }
        GL.BindVertexArray(0);
#if DEBUG
        if (!CheckGLError("GL.BindVertexArray", "vao=0")) return;
#endif
        GL.Enable(EnableCap.DepthTest);
#if DEBUG
        if (!CheckGLError("GL.Enable", $"cap={EnableCap.DepthTest}")) return;
#endif
    }

    private void DrawBoneMarkers()
    {
        if (_bones.Count == 0)
            return;

        EnsureIkBoneMesh();
        if (DistinguishBoneTypes)
            EnsureCubeMesh();

        GL.Disable(EnableCap.DepthTest);
#if DEBUG
        if (!CheckGLError("GL.Disable", $"cap={EnableCap.DepthTest}")) return;
#endif
        GL.Uniform1(_pointSizeLoc, 1f);
#if DEBUG
        if (!CheckGLError("GL.Uniform1", $"loc={_pointSizeLoc}")) return;
#endif

        HashSet<int> ikIndices;
        lock (_ikBonesLock)
            ikIndices = new HashSet<int>(_ikBoneIndices);

        for (int i = 0; i < _bones.Count; i++)
        {
            bool isIk = ikIndices.Contains(i);
            if (isIk && ShowIkBones)
                continue;

            var pos = _worldMats[i].Translation.ToOpenTK();
            float scale = _ikBoneScale * _distance;
            if (i == SelectedBoneIndex)
                scale *= 1.4f;
            var mat = Matrix4.CreateScale(scale) * Matrix4.CreateTranslation(pos);
            GL.UniformMatrix4(_modelLoc, false, ref mat);
#if DEBUG
            if (!CheckGLError("GL.UniformMatrix4", $"loc={_modelLoc}")) return;
#endif
            bool isPhysics = DistinguishBoneTypes && _physicsBones.Contains(i);
            bool isIkBone = DistinguishBoneTypes && isIk;
            Vector4 color = new Vector4(1f, 1f, 0f, 1f);
            if (i == SelectedBoneIndex)
                color = new Vector4(1f, 0f, 0f, 1f);
            else if (isPhysics)
                color = new Vector4(0f, 0f, 1f, 1f);
            else if (isIkBone)
                color = new Vector4(0f, 1f, 0f, 1f);
            GL.Uniform4(_colorLoc, color);
#if DEBUG
            if (!CheckGLError("GL.Uniform4", $"loc={_colorLoc}")) return;
#endif

            if (isPhysics)
            {
                GL.BindVertexArray(_cubeVao);
#if DEBUG
                if (!CheckGLError("GL.BindVertexArray", $"vao={_cubeVao}")) return;
#endif
                GL.DrawElements(PrimitiveType.Triangles, _cubeIndexCount, DrawElementsType.UnsignedShort, 0);
#if DEBUG
                if (!CheckGLError("GL.DrawElements", $"count={_cubeIndexCount}")) return;
#endif
            }
            else
            {
                GL.BindVertexArray(_ikBoneVao);
#if DEBUG
                if (!CheckGLError("GL.BindVertexArray", $"vao={_ikBoneVao}")) return;
#endif
                GL.DrawElements(PrimitiveType.Triangles, _ikBoneIndexCount, DrawElementsType.UnsignedShort, 0);
#if DEBUG
                if (!CheckGLError("GL.DrawElements", $"count={_ikBoneIndexCount}")) return;
#endif
            }
        }
        GL.BindVertexArray(0);
#if DEBUG
        if (!CheckGLError("GL.BindVertexArray", "vao=0")) return;
#endif
        GL.Enable(EnableCap.DepthTest);
#if DEBUG
        if (!CheckGLError("GL.Enable", $"cap={EnableCap.DepthTest}")) return;
#endif
    }

    public void Render()
    {
        UpdateViewProjection();

        bool needsUpdate = _bonesDirty || _morphDirty || _uvMorphDirty;
        if (needsUpdate)
        {
            if (_bones.Count > 0)
                CpuSkinning();
            UpdateVertexBuffers();
            if (_bufferUpdatePending)
            {
                GL.Flush();
                CheckGLError("GL.Flush");
                _bufferUpdatePending = false;
            }
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
#if DEBUG
        CheckGLError(nameof(UpdateViewProjection));
#endif
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
                unsafe
                {
                    fixed (float* p = _boneLines)
                    {
                        GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, lineIdx * sizeof(float), (IntPtr)p);
                        _bufferUpdatePending = true;
                    }
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

        RenderMesh[] meshes;
        lock (_meshesLock)
            meshes = _meshes.ToArray();

        if (_bones.Count > 0)
        {
            if (_bonesDirty)
            {
                foreach (var rm in meshes)
                {
                    if (rm.JointIndices.Length != rm.BaseVertices.Length)
                        continue;
                    GL.BindBuffer(BufferTarget.ArrayBuffer, rm.Vbo);
                    if (!CheckGLError("GL.BindBuffer", $"target={BufferTarget.ArrayBuffer}, buffer={rm.Vbo}"))
                        return;
                    int byteSize = rm.BaseVertices.Length * 8 * sizeof(float);
                    IntPtr mapped = GL.MapBufferRange(BufferTarget.ArrayBuffer, IntPtr.Zero, byteSize, BufferAccessMask.MapWriteBit | BufferAccessMask.MapInvalidateBufferBit);
                    if (mapped == IntPtr.Zero)
                    {
                        CheckGLError("GL.MapBufferRange");
                        continue;
                    }
                    unsafe
                    {
                        float* dst = (float*)mapped;
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

                            dst[vi * 8 + 0] = pos.X;
                            dst[vi * 8 + 1] = pos.Y;
                            dst[vi * 8 + 2] = pos.Z;
                            dst[vi * 8 + 3] = norm.X;
                            dst[vi * 8 + 4] = norm.Y;
                            dst[vi * 8 + 5] = norm.Z;
                            if (vi < rm.TexCoords.Length)
                            {
                                var uv = rm.TexCoords[vi];
                                if (vi < rm.UvOffsets.Length)
                                    uv += rm.UvOffsets[vi];
                                dst[vi * 8 + 6] = uv.X;
                                dst[vi * 8 + 7] = uv.Y;
                            }
                            else
                            {
                                dst[vi * 8 + 6] = 0f;
                                dst[vi * 8 + 7] = 0f;
                            }
                        }
                      
                        GL.BindBuffer(BufferTarget.ArrayBuffer, rm.Vbo);
#if DEBUG
                        if (!CheckGLError("GL.BindBuffer", $"target={BufferTarget.ArrayBuffer}, buffer={rm.Vbo}"))
                            return;
#endif
                        int byteSize = rm.BaseVertices.Length * 8 * sizeof(float);
                        int poolSize = tmpVertexBuffer.Length * sizeof(float);
                        if (poolSize < byteSize)
                        {
                            Console.Error.WriteLine($"Vertex buffer overflow: required={byteSize}, available={poolSize}");
                            byteSize = poolSize;
                        }
                        else if (poolSize > byteSize)
                        {
                            Console.Error.WriteLine($"Vertex buffer size mismatch: required={byteSize}, available={poolSize}");
                        }
                        var handle = GCHandle.Alloc(tmpVertexBuffer, GCHandleType.Pinned);
                        try
                        {
                            IntPtr ptr = handle.AddrOfPinnedObject();
                            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, byteSize, ptr);
#if DEBUG
                            if (!CheckGLError("GL.BufferSubData", $"size={byteSize}, ptr={ptr}"))
                                return;
#endif
                            GL.Finish();
#if DEBUG
                            if (!CheckGLError("GL.Finish"))
                                return;
#endif
                            _bufferUpdatePending = true;
                        }
                        finally
                        {
                            handle.Free();
                        }
                    }
                    GL.UnmapBuffer(BufferTarget.ArrayBuffer);
                    if (!CheckGLError("GL.UnmapBuffer"))
                        return;
                    GL.Finish();
                    if (!CheckGLError("GL.Finish"))
                        return;
                }
            }
            else if ((_morphDirty || _uvMorphDirty) && changedVerts != null)
            {
                unsafe
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

                            GL.BindBuffer(BufferTarget.ArrayBuffer, rm.Vbo);
#if DEBUG
                            if (!CheckGLError("GL.BindBuffer", $"target={BufferTarget.ArrayBuffer}, buffer={rm.Vbo}"))
                                return;
#endif
                            IntPtr offset = new IntPtr(vi * 8 * sizeof(float));
                            IntPtr ptr = GL.MapBufferRange(BufferTarget.ArrayBuffer, offset, 8 * sizeof(float), BufferAccessMask.MapWriteBit | BufferAccessMask.MapInvalidateRangeBit);
                            if (ptr == IntPtr.Zero)
                            {
                                CheckGLError("GL.MapBufferRange");
                                continue;
                            }
                            float* buf = stackalloc float[8];
                            buf[0] = pos.X; buf[1] = pos.Y; buf[2] = pos.Z;
                            buf[3] = norm.X; buf[4] = norm.Y; buf[5] = norm.Z;
                            if (vi < rm.TexCoords.Length)
                            {
                                var uv = rm.TexCoords[vi];
                                if (vi < rm.UvOffsets.Length)
                                    uv += rm.UvOffsets[vi];
                
                                buf[6] = uv.X; buf[7] = uv.Y;
                            }
                            else { buf[6] = 0f; buf[7] = 0f; }

                            Buffer.MemoryCopy(buf, (void*)ptr, 8 * sizeof(float), 8 * sizeof(float));
                            GL.UnmapBuffer(BufferTarget.ArrayBuffer);
                            if (!CheckGLError("GL.UnmapBuffer"))
                            GL.BufferSubData(BufferTarget.ArrayBuffer, offset, smallByteSize, smallPtr);
#if DEBUG
                            if (!CheckGLError("GL.BufferSubData", $"offset={offset}, size={smallByteSize}, ptr={smallPtr}"))
                                return;
#endif
                            GL.Finish();
#if DEBUG
                            if (!CheckGLError("GL.Finish"))
                                return;
#endif
                            _bufferUpdatePending = true;
                        }
                    }
                }
            }
            else
            {
                foreach (var rm in meshes)
                {
                    GL.BindBuffer(BufferTarget.ArrayBuffer, rm.Vbo);
                    if (!CheckGLError("GL.BindBuffer", $"target={BufferTarget.ArrayBuffer}, buffer={rm.Vbo}"))
                        return;
                    int byteSize2 = rm.BaseVertices.Length * 8 * sizeof(float);
                    IntPtr mapped2 = GL.MapBufferRange(BufferTarget.ArrayBuffer, IntPtr.Zero, byteSize2, BufferAccessMask.MapWriteBit | BufferAccessMask.MapInvalidateBufferBit);
                    if (mapped2 == IntPtr.Zero)
                    {
                        CheckGLError("GL.MapBufferRange");
                        continue;
                    }
                    unsafe
                    {
                        float* dst = (float*)mapped2;
                        for (int vi = 0; vi < rm.BaseVertices.Length; vi++)
                        {
                            var pos = rm.BaseVertices[vi] + rm.VertexOffsets[vi];
                            var nor = vi < rm.Normals.Length ? rm.Normals[vi] : new Vector3(0, 0, 1);
                            dst[vi * 8 + 0] = pos.X;
                            dst[vi * 8 + 1] = pos.Y;
                            dst[vi * 8 + 2] = pos.Z;
                            dst[vi * 8 + 3] = nor.X;
                            dst[vi * 8 + 4] = nor.Y;
                            dst[vi * 8 + 5] = nor.Z;
                            if (vi < rm.TexCoords.Length)
                            {
                                var uv = rm.TexCoords[vi];
                                if (vi < rm.UvOffsets.Length)
                                    uv += rm.UvOffsets[vi];
                                dst[vi * 8 + 6] = uv.X;
                                dst[vi * 8 + 7] = uv.Y;
                            }
                            else
                            {
                                dst[vi * 8 + 6] = 0f;
                                dst[vi * 8 + 7] = 0f;
                            }
                        }

                        GL.BindBuffer(BufferTarget.ArrayBuffer, rm.Vbo);
#if DEBUG
                        if (!CheckGLError("GL.BindBuffer", $"target={BufferTarget.ArrayBuffer}, buffer={rm.Vbo}"))
                            return;
#endif
                        int byteSize2 = rm.BaseVertices.Length * 8 * sizeof(float);
                        int poolSize2 = tmpVertexBuffer.Length * sizeof(float);
                        if (poolSize2 < byteSize2)
                        {
                            Console.Error.WriteLine($"Vertex buffer overflow: required={byteSize2}, available={poolSize2}");
                            byteSize2 = poolSize2;
                        }
                        else if (poolSize2 > byteSize2)
                        {
                            Console.Error.WriteLine($"Vertex buffer size mismatch: required={byteSize2}, available={poolSize2}");
                        }
                        var handle2 = GCHandle.Alloc(tmpVertexBuffer, GCHandleType.Pinned);
                        try
                        {
                            IntPtr ptr2 = handle2.AddrOfPinnedObject();
                            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, byteSize2, ptr2);
#if DEBUG
                            if (!CheckGLError("GL.BufferSubData", $"size={byteSize2}, ptr={ptr2}"))
                                return;
#endif
                            GL.Finish();
#if DEBUG
                            if (!CheckGLError("GL.Finish"))
                                return;
#endif
                            _bufferUpdatePending = true;
                        }
                        finally
                        {
                            handle2.Free();
                        }
                    }
                    GL.UnmapBuffer(BufferTarget.ArrayBuffer);
                    if (!CheckGLError("GL.UnmapBuffer"))
                        return;
                    GL.Finish();
                    if (!CheckGLError("GL.Finish"))
                        return;
                }
            }

            _bonesDirty = false;
            _morphDirty = false;
            _uvMorphDirty = false;
        }
    }

    private void DrawScene()
    {
        GL.Enable(EnableCap.DepthTest);
#if DEBUG
        if (!CheckGLError("GL.Enable", $"cap={EnableCap.DepthTest}")) return;
#endif
        GL.Enable(EnableCap.CullFace);
#if DEBUG
        if (!CheckGLError("GL.Enable", $"cap={EnableCap.CullFace}")) return;
#endif
        GL.Enable(EnableCap.Blend); // 半透明描画のためブレンドを有効化
#if DEBUG
        if (!CheckGLError("GL.Enable", $"cap={EnableCap.Blend}")) return;
#endif
        GL.FrontFace(FrontFaceDirection.Cw);
#if DEBUG
        if (!CheckGLError("GL.FrontFace", $"dir={FrontFaceDirection.Cw}")) return;
#endif
        GL.ClearColor(1f, 1f, 1f, 1f);
#if DEBUG
        if (!CheckGLError("GL.ClearColor", "1,1,1,1")) return;
#endif
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
#if DEBUG
        if (!CheckGLError("GL.Clear", $"mask={ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit}")) return;
#endif

        var modelMat = ModelTransform;

        GL.UseProgram(_modelProgram);
#if DEBUG
        if (!CheckGLError("GL.UseProgram", $"program={_modelProgram}")) return;
#endif
        GL.UniformMatrix4(_modelViewLoc, false, ref _viewMatrix);
#if DEBUG
        if (!CheckGLError("GL.UniformMatrix4", $"loc={_modelViewLoc}")) return;
#endif
        GL.UniformMatrix4(_modelProjLoc, false, ref _projMatrix);
#if DEBUG
        if (!CheckGLError("GL.UniformMatrix4", $"loc={_modelProjLoc}")) return;
#endif
        Vector3 light = Vector3.Normalize(new Vector3(-0.3f, 0.6f, -0.7f));
        light = Vector3.TransformNormal(light, _cameraRot);
        GL.Uniform3(_modelLightDirLoc, ref light);
#if DEBUG
        if (!CheckGLError("GL.Uniform3", $"loc={_modelLightDirLoc}")) return;
#endif
        Vector3 viewDir = Vector3.Normalize(_target - _cameraPos);
        GL.Uniform3(_modelViewDirLoc, ref viewDir);
#if DEBUG
        if (!CheckGLError("GL.Uniform3", $"loc={_modelViewDirLoc}")) return;
#endif
        GL.Uniform1(_modelShadeShiftLoc, ShadeShift);
#if DEBUG
        if (!CheckGLError("GL.Uniform1", $"loc={_modelShadeShiftLoc}")) return;
#endif
        GL.Uniform1(_modelShadeToonyLoc, ShadeToony);
#if DEBUG
        if (!CheckGLError("GL.Uniform1", $"loc={_modelShadeToonyLoc}")) return;
#endif
        GL.Uniform1(_modelRimIntensityLoc, RimIntensity);
#if DEBUG
        if (!CheckGLError("GL.Uniform1", $"loc={_modelRimIntensityLoc}")) return;
#endif
        GL.Uniform1(_modelAmbientLoc, Ambient);
#if DEBUG
        if (!CheckGLError("GL.Uniform1", $"loc={_modelAmbientLoc}")) return;
#endif
        GL.UniformMatrix4(_modelMatrixLoc, false, ref modelMat);
#if DEBUG
        if (!CheckGLError("GL.UniformMatrix4", $"loc={_modelMatrixLoc}")) return;
#endif
        RenderMesh[] meshes;
        lock (_meshesLock)
            meshes = _meshes.ToArray();
        foreach (var rm in meshes)
        {
            GL.Uniform4(_modelColorLoc, rm.Color);
#if DEBUG
            if (!CheckGLError("GL.Uniform4", $"loc={_modelColorLoc}")) return;
#endif
            GL.Uniform3(_modelSpecularLoc, rm.Specular);
#if DEBUG
            if (!CheckGLError("GL.Uniform3", $"loc={_modelSpecularLoc}")) return;
#endif
            GL.Uniform1(_modelSpecularPowerLoc, rm.SpecularPower);
#if DEBUG
            if (!CheckGLError("GL.Uniform1", $"loc={_modelSpecularPowerLoc}")) return;
#endif
            GL.Uniform4(_modelEdgeColorLoc, rm.EdgeColor);
#if DEBUG
            if (!CheckGLError("GL.Uniform4", $"loc={_modelEdgeColorLoc}")) return;
#endif
            GL.Uniform1(_modelEdgeSizeLoc, rm.EdgeSize);
#if DEBUG
            if (!CheckGLError("GL.Uniform1", $"loc={_modelEdgeSizeLoc}")) return;
#endif
            GL.Uniform3(_modelToonColorLoc, rm.ToonColor);
#if DEBUG
            if (!CheckGLError("GL.Uniform3", $"loc={_modelToonColorLoc}")) return;
#endif
            GL.Uniform4(_modelTexTintLoc, rm.TextureTint);
#if DEBUG
            if (!CheckGLError("GL.Uniform4", $"loc={_modelTexTintLoc}")) return;
#endif
            GL.Uniform1(_modelSphereModeLoc, (int)rm.SphereMode);
#if DEBUG
            if (!CheckGLError("GL.Uniform1", $"loc={_modelSphereModeLoc}")) return;
#endif
            GL.Uniform1(_modelSphereStrengthLoc, SphereStrength);
#if DEBUG
            if (!CheckGLError("GL.Uniform1", $"loc={_modelSphereStrengthLoc}")) return;
#endif
            GL.Uniform1(_modelToonStrengthLoc, ToonStrength);
#if DEBUG
            if (!CheckGLError("GL.Uniform1", $"loc={_modelToonStrengthLoc}")) return;
#endif
            if (rm.HasTexture)
            {
                GL.ActiveTexture(TextureUnit.Texture0);
#if DEBUG
                if (!CheckGLError("GL.ActiveTexture", $"unit={TextureUnit.Texture0}")) return;
#endif
                GL.BindTexture(TextureTarget.Texture2D, rm.Texture);
#if DEBUG
                if (!CheckGLError("GL.BindTexture", $"target={TextureTarget.Texture2D}, tex={rm.Texture}")) return;
#endif
                GL.Uniform1(_modelTexLoc, 0);
#if DEBUG
                if (!CheckGLError("GL.Uniform1", $"loc={_modelTexLoc}")) return;
#endif
                GL.Uniform1(_modelUseTexLoc, 1);
#if DEBUG
                if (!CheckGLError("GL.Uniform1", $"loc={_modelUseTexLoc}")) return;
#endif
            }
            else
            {
                GL.Uniform1(_modelUseTexLoc, 0);
#if DEBUG
                if (!CheckGLError("GL.Uniform1", $"loc={_modelUseTexLoc}")) return;
#endif
            }
            if (rm.HasSphereTexture)
            {
                GL.ActiveTexture(TextureUnit.Texture1);
#if DEBUG
                if (!CheckGLError("GL.ActiveTexture", $"unit={TextureUnit.Texture1}")) return;
#endif
                GL.BindTexture(TextureTarget.Texture2D, rm.SphereTexture);
#if DEBUG
                if (!CheckGLError("GL.BindTexture", $"target={TextureTarget.Texture2D}, tex={rm.SphereTexture}")) return;
#endif
                GL.Uniform1(_modelSphereTexLoc, 1);
#if DEBUG
                if (!CheckGLError("GL.Uniform1", $"loc={_modelSphereTexLoc}")) return;
#endif
                GL.Uniform1(_modelUseSphereTexLoc, 1);
#if DEBUG
                if (!CheckGLError("GL.Uniform1", $"loc={_modelUseSphereTexLoc}")) return;
#endif
            }
            else
            {
                GL.Uniform1(_modelUseSphereTexLoc, 0);
#if DEBUG
                if (!CheckGLError("GL.Uniform1", $"loc={_modelUseSphereTexLoc}")) return;
#endif
            }
            if (rm.HasToonTexture)
            {
                GL.ActiveTexture(TextureUnit.Texture2);
#if DEBUG
                if (!CheckGLError("GL.ActiveTexture", $"unit={TextureUnit.Texture2}")) return;
#endif
                GL.BindTexture(TextureTarget.Texture2D, rm.ToonTexture);
#if DEBUG
                if (!CheckGLError("GL.BindTexture", $"target={TextureTarget.Texture2D}, tex={rm.ToonTexture}")) return;
#endif
                GL.Uniform1(_modelToonTexLoc, 2);
#if DEBUG
                if (!CheckGLError("GL.Uniform1", $"loc={_modelToonTexLoc}")) return;
#endif
                GL.Uniform1(_modelUseToonTexLoc, 1);
#if DEBUG
                if (!CheckGLError("GL.Uniform1", $"loc={_modelUseToonTexLoc}")) return;
#endif
            }
            else
            {
                GL.Uniform1(_modelUseToonTexLoc, 0);
#if DEBUG
                if (!CheckGLError("GL.Uniform1", $"loc={_modelUseToonTexLoc}")) return;
#endif
            }
            GL.BindVertexArray(rm.Vao);
#if DEBUG
            if (!CheckGLError("GL.BindVertexArray", $"vao={rm.Vao}")) return;
#endif
            GL.DrawElements(PrimitiveType.Triangles, rm.IndexCount, DrawElementsType.UnsignedInt, IntPtr.Zero);
#if DEBUG
            if (!CheckGLError("GL.DrawElements", $"count={rm.IndexCount}")) return;
#endif
            GL.BindVertexArray(0);
#if DEBUG
            if (!CheckGLError("GL.BindVertexArray", "vao=0")) return;
#endif
            if (rm.HasTexture)
            {
                GL.ActiveTexture(TextureUnit.Texture0);
#if DEBUG
                if (!CheckGLError("GL.ActiveTexture", $"unit={TextureUnit.Texture0}")) return;
#endif
                GL.BindTexture(TextureTarget.Texture2D, 0);
#if DEBUG
                if (!CheckGLError("GL.BindTexture", $"target={TextureTarget.Texture2D}, tex=0")) return;
#endif
            }
            if (rm.HasSphereTexture)
            {
                GL.ActiveTexture(TextureUnit.Texture1);
#if DEBUG
                if (!CheckGLError("GL.ActiveTexture", $"unit={TextureUnit.Texture1}")) return;
#endif
                GL.BindTexture(TextureTarget.Texture2D, 0);
#if DEBUG
                if (!CheckGLError("GL.BindTexture", $"target={TextureTarget.Texture2D}, tex=0")) return;
#endif
            }
            if (rm.HasToonTexture)
            {
                GL.ActiveTexture(TextureUnit.Texture2);
#if DEBUG
                if (!CheckGLError("GL.ActiveTexture", $"unit={TextureUnit.Texture2}")) return;
#endif
                GL.BindTexture(TextureTarget.Texture2D, 0);
#if DEBUG
                if (!CheckGLError("GL.BindTexture", $"target={TextureTarget.Texture2D}, tex=0")) return;
#endif
            }
            GL.ActiveTexture(TextureUnit.Texture0);
#if DEBUG
            if (!CheckGLError("GL.ActiveTexture", $"unit={TextureUnit.Texture0}")) return;
#endif
        }
        GL.UseProgram(_program);
#if DEBUG
        if (!CheckGLError("GL.UseProgram", $"program={_program}")) return;
#endif
        GL.UniformMatrix4(_viewLoc, false, ref _viewMatrix);
#if DEBUG
        if (!CheckGLError("GL.UniformMatrix4", $"loc={_viewLoc}")) return;
#endif
        GL.UniformMatrix4(_projLoc, false, ref _projMatrix);
#if DEBUG
        if (!CheckGLError("GL.UniformMatrix4", $"loc={_projLoc}")) return;
#endif

        Matrix4 gridModel = Matrix4.Identity;
        GL.DepthMask(false);
#if DEBUG
        if (!CheckGLError("GL.DepthMask", "false")) return;
#endif
        GL.UniformMatrix4(_modelLoc, false, ref gridModel);
#if DEBUG
        if (!CheckGLError("GL.UniformMatrix4", $"loc={_modelLoc}")) return;
#endif
        GL.Uniform4(_colorLoc, new Vector4(1f, 1f, 1f, 0.3f));
#if DEBUG
        if (!CheckGLError("GL.Uniform4", $"loc={_colorLoc}")) return;
#endif
        GL.BindVertexArray(_groundVao);
#if DEBUG
        if (!CheckGLError("GL.BindVertexArray", $"vao={_groundVao}")) return;
#endif
        GL.DrawArrays(PrimitiveType.Triangles, 0, _groundVertexCount);
#if DEBUG
        if (!CheckGLError("GL.DrawArrays", $"mode={PrimitiveType.Triangles}, count={_groundVertexCount}")) return;
#endif
        GL.BindVertexArray(0);
#if DEBUG
        if (!CheckGLError("GL.BindVertexArray", "vao=0")) return;
#endif

        GL.UniformMatrix4(_modelLoc, false, ref gridModel);
#if DEBUG
        if (!CheckGLError("GL.UniformMatrix4", $"loc={_modelLoc}")) return;
#endif
        GL.Uniform4(_colorLoc, new Vector4(0.8f, 0.8f, 0.8f, 0.5f));
#if DEBUG
        if (!CheckGLError("GL.Uniform4", $"loc={_colorLoc}")) return;
#endif
        GL.BindVertexArray(_gridVao);
#if DEBUG
        if (!CheckGLError("GL.BindVertexArray", $"vao={_gridVao}")) return;
#endif
        GL.DrawArrays(PrimitiveType.Lines, 0, _gridVertexCount);
#if DEBUG
        if (!CheckGLError("GL.DrawArrays", $"mode={PrimitiveType.Lines}, count={_gridVertexCount}")) return;
#endif
        GL.BindVertexArray(0);
#if DEBUG
        if (!CheckGLError("GL.BindVertexArray", "vao=0")) return;
#endif
        if (_axesVao != 0)
        {
            GL.UniformMatrix4(_modelLoc, false, ref gridModel);
#if DEBUG
            if (!CheckGLError("GL.UniformMatrix4", $"loc={_modelLoc}")) return;
#endif
            GL.Disable(EnableCap.DepthTest);
#if DEBUG
            if (!CheckGLError("GL.Disable", $"cap={EnableCap.DepthTest}")) return;
#endif
            GL.BindVertexArray(_axesVao);
#if DEBUG
            if (!CheckGLError("GL.BindVertexArray", $"vao={_axesVao}")) return;
#endif
            int cnt = _axesVertexCount;
            GL.Uniform4(_colorLoc, new Vector4(1f, 0f, 0f, 1f));
#if DEBUG
            if (!CheckGLError("GL.Uniform4", $"loc={_colorLoc}")) return;
#endif
            GL.DrawArrays(PrimitiveType.Lines, 0, cnt);
#if DEBUG
            if (!CheckGLError("GL.DrawArrays", $"mode={PrimitiveType.Lines}, count={cnt}")) return;
#endif
            GL.Uniform4(_colorLoc, new Vector4(0f, 1f, 0f, 1f));
#if DEBUG
            if (!CheckGLError("GL.Uniform4", $"loc={_colorLoc}")) return;
#endif
            GL.DrawArrays(PrimitiveType.Lines, cnt, cnt);
#if DEBUG
            if (!CheckGLError("GL.DrawArrays", $"mode={PrimitiveType.Lines}, first={cnt}, count={cnt}")) return;
#endif
            GL.Uniform4(_colorLoc, new Vector4(0f, 0f, 1f, 1f));
#if DEBUG
            if (!CheckGLError("GL.Uniform4", $"loc={_colorLoc}")) return;
#endif
            GL.DrawArrays(PrimitiveType.Lines, cnt * 2, cnt);
#if DEBUG
            if (!CheckGLError("GL.DrawArrays", $"mode={PrimitiveType.Lines}, first={cnt * 2}, count={cnt}")) return;
#endif
            GL.BindVertexArray(0);
#if DEBUG
            if (!CheckGLError("GL.BindVertexArray", "vao=0")) return;
#endif
            GL.Enable(EnableCap.DepthTest);
#if DEBUG
            if (!CheckGLError("GL.Enable", $"cap={EnableCap.DepthTest}")) return;
#endif
        }
        GL.DepthMask(true);
#if DEBUG
        if (!CheckGLError("GL.DepthMask", "true")) return;
#endif

        if (ShowBoneOutline)
        {
            DrawBoneMarkers();
            if (_boneVertexCount > 0)
            {
                GL.Disable(EnableCap.DepthTest);
#if DEBUG
                if (!CheckGLError("GL.Disable", $"cap={EnableCap.DepthTest}")) return;
#endif
                GL.UniformMatrix4(_modelLoc, false, ref modelMat);
#if DEBUG
                if (!CheckGLError("GL.UniformMatrix4", $"loc={_modelLoc}")) return;
#endif
                GL.Uniform4(_colorLoc, new Vector4(1f, 0f, 0f, 1f));
#if DEBUG
                if (!CheckGLError("GL.Uniform4", $"loc={_colorLoc}")) return;
#endif
                GL.BindVertexArray(_boneVao);
#if DEBUG
                if (!CheckGLError("GL.BindVertexArray", $"vao={_boneVao}")) return;
#endif
                GL.DrawArrays(PrimitiveType.Lines, 0, _boneVertexCount);
#if DEBUG
                if (!CheckGLError("GL.DrawArrays", $"mode={PrimitiveType.Lines}, count={_boneVertexCount}")) return;
#endif
                GL.BindVertexArray(0);
#if DEBUG
                if (!CheckGLError("GL.BindVertexArray", "vao=0")) return;
#endif
                GL.Enable(EnableCap.DepthTest);
#if DEBUG
                if (!CheckGLError("GL.Enable", $"cap={EnableCap.DepthTest}")) return;
#endif
            }
        }

        if (ShowIkBones)
        {
            DrawIkBones();
        }
    }

}
