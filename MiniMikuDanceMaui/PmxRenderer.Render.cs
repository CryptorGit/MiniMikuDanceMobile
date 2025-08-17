using MiniMikuDance.Util;
using SharpBgfx;
using VertexBuffer = SharpBgfx.DynamicVertexBuffer;
using IndexBuffer = SharpBgfx.DynamicIndexBuffer;
using Matrix4 = System.Numerics.Matrix4x4;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;
using Quaternion = System.Numerics.Quaternion;

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
                mesh.VertexBuffer?.Update(start, MemoryBlock.FromArray(verts));
            }
            indices.Clear();
        }

        foreach (var mesh in _meshes)
        {
            if (!mesh.IndicesDirty || mesh.IndexBuffer == null)
                continue;

            if (mesh.Indices32.Length > 0)
                mesh.IndexBuffer?.Update(0, MemoryBlock.FromArray(mesh.Indices32));
            else if (mesh.Indices16.Length > 0)
                mesh.IndexBuffer?.Update(0, MemoryBlock.FromArray(mesh.Indices16));

            mesh.IndicesDirty = false;
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

        SetViewTransform(0, _viewMatrix, _projMatrix);

        DrawScene();
        DrawIkBones();

        Bgfx.Touch(0);
        Bgfx.Frame();
    }

    private void DrawScene()
    {
        var lightDir = new Vector4(LightDirection, 0f);
        var lightColor = new Vector4(LightColor, 1f);
        var shadeParam = new Vector4(ShadeShift, ShadeToony, RimIntensity, Ambient);
        if (_lightDirUniform != null) SetUniform(_lightDirUniform.Value, lightDir);
        if (_lightColorUniform != null) SetUniform(_lightColorUniform.Value, lightColor);
        if (_shadeParamUniform != null) SetUniform(_shadeParamUniform.Value, shadeParam);

        foreach (var rm in _meshes)
        {
            if (rm.VertexBuffer == null || rm.IndexBuffer == null)
                continue;

            var vertexBuffer = rm.VertexBuffer.Value;
            var indexBuffer = rm.IndexBuffer.Value;
            SetTransform(_modelTransform);
            Bgfx.SetVertexBuffer(0, vertexBuffer);
            Bgfx.SetIndexBuffer(indexBuffer);
            if (rm.Texture != null && rm.TextureUniform != null && rm.HasTexture)
                Bgfx.SetTexture(0, rm.TextureUniform.Value, rm.Texture);
            if (rm.ColorUniform != null) SetUniform(rm.ColorUniform.Value, rm.Color);
            if (rm.SpecularUniform != null) SetUniform(rm.SpecularUniform.Value, new Vector4(rm.Specular, rm.SpecularPower));
            if (rm.EdgeUniform != null) SetUniform(rm.EdgeUniform.Value, new Vector4(rm.EdgeColor.X, rm.EdgeColor.Y, rm.EdgeColor.Z, rm.EdgeSize));
            if (rm.ToonColorUniform != null) SetUniform(rm.ToonColorUniform.Value, new Vector4(rm.ToonColor, 1f));
            if (rm.TextureTintUniform != null) SetUniform(rm.TextureTintUniform.Value, rm.TextureTint);
            var program = _modelProgram ?? _program;
            if (program != null)
                Bgfx.Submit(0, program.Value, 0);
        }
    }

    private void DrawIkBones()
    {
        var vertices = new List<PmxVertex>();
        var indices = new List<ushort>();
        ushort vi = 0;

        if (_showBoneOutline && _bones.Count > 0)
        {
            int count = 0;
            for (int i = 0; i < _bones.Count; i++)
            {
                int parent = _bones[i].Parent;
                if (parent < 0)
                    continue;
                var start = GetBoneWorldPosition(parent);
                var end = GetBoneWorldPosition(i);
                _boneLines[count++] = start.X;
                _boneLines[count++] = start.Y;
                _boneLines[count++] = start.Z;
                _boneLines[count++] = end.X;
                _boneLines[count++] = end.Y;
                _boneLines[count++] = end.Z;

                vertices.Add(new PmxVertex { Px = start.X, Py = start.Y, Pz = start.Z });
                vertices.Add(new PmxVertex { Px = end.X, Py = end.Y, Pz = end.Z });
                indices.Add(vi++);
                indices.Add(vi++);
            }
            _boneVertexCount = count / 3;
        }

        lock (_ikBonesLock)
        {
            foreach (var ik in _ikBones)
            {
                var p = ik.Position;
                const float s = 0.1f;
                vertices.Add(new PmxVertex { Px = p.X - s, Py = p.Y, Pz = p.Z });
                vertices.Add(new PmxVertex { Px = p.X + s, Py = p.Y, Pz = p.Z });
                indices.Add(vi++);
                indices.Add(vi++);
                vertices.Add(new PmxVertex { Px = p.X, Py = p.Y - s, Pz = p.Z });
                vertices.Add(new PmxVertex { Px = p.X, Py = p.Y + s, Pz = p.Z });
                indices.Add(vi++);
                indices.Add(vi++);
                vertices.Add(new PmxVertex { Px = p.X, Py = p.Y, Pz = p.Z - s });
                vertices.Add(new PmxVertex { Px = p.X, Py = p.Y, Pz = p.Z + s });
                indices.Add(vi++);
                indices.Add(vi++);
            }
        }

        if (vertices.Count == 0)
            return;

        var vb = new VertexBuffer(MemoryBlock.FromArray(vertices.ToArray()), PmxVertex.Layout);
        var ib = new IndexBuffer(MemoryBlock.FromArray(indices.ToArray()));
        SetTransform(_modelTransform);
        Bgfx.SetVertexBuffer(0, vb);
        Bgfx.SetIndexBuffer(ib);
        var program = _modelProgram ?? _program;
        if (program != null)
            Bgfx.Submit(0, program.Value, 0);
        vb.Dispose();
        ib.Dispose();
    }
}
