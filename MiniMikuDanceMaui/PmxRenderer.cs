using System;
using OpenTK.Mathematics;
using OpenTK.Graphics.ES30;
using GL = OpenTK.Graphics.ES30.GL;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using MiniMikuDance.Util;
using MiniMikuDance.Import;
using MiniMikuDance.App;
using MMDTools;
using SkiaSharp.Views.Maui.Controls;
using Vector2 = OpenTK.Mathematics.Vector2;
using Vector3 = OpenTK.Mathematics.Vector3;
using Vector4 = OpenTK.Mathematics.Vector4;

namespace MiniMikuDanceMaui;

public class PmxRenderer : IDisposable
{
    private int _program;
    private class RenderMesh
    {
        public int Vao;
        public int Vbo;
        public int Ebo;
        public int IndexCount;
        public Vector4 Color = Vector4.One;
        public int Texture;
        public bool HasTexture;
        public Vector3[] Vertices = Array.Empty<Vector3>();
        public Vector3[] BaseVertices = Array.Empty<Vector3>();
        public Vector3[] Normals = Array.Empty<Vector3>();
        public Vector2[] TexCoords = Array.Empty<Vector2>();
        public Vector4[] JointIndices = Array.Empty<Vector4>();
        public Vector4[] JointWeights = Array.Empty<Vector4>();
    }
    private readonly System.Collections.Generic.List<RenderMesh> _meshes = new();
    private readonly Dictionary<string, MorphData> _morphs = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, float> _morphValues = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, List<(RenderMesh Mesh, int Index)>> _morphVertexMap = new();
    private readonly HashSet<int> _changedOriginalVertices = new();
    private readonly Dictionary<int, List<(Vector3 Offset, string Name)>> _vertexMorphContribs = new();
    public SKGLView? Viewer { get; set; }
    private int _gridVao;
    private int _gridVbo;
    private int _gridVertexCount;
    private int _modelLoc;
    private int _viewLoc;
    private int _projLoc;
    private int _colorLoc;
    private float _orbitX;
    // 初期カメラ位置: 水平回転は0（正面から）
    private float _orbitY = 0f;
    private float _distance = 4f;
    // モデル中心より少し高い位置を基準にカメラを配置する
    private Vector3 _target = new Vector3(0f, 0.5f, 0f);
    private int _groundVao;
    private int _groundVbo;
    private int _groundVertexCount;
    private int _boneVao;
    private int _boneVbo;
    private int _boneVertexCount;
    private System.Numerics.Matrix4x4[] _worldMats = Array.Empty<System.Numerics.Matrix4x4>();
    private System.Numerics.Matrix4x4[] _skinMats = Array.Empty<System.Numerics.Matrix4x4>();
    private float[] _boneLines = Array.Empty<float>();
    private int _boneCapacity;
    private int _modelProgram;
    private int _modelViewLoc;
    private int _modelProjLoc;
    private int _modelMatrixLoc;
    private int _modelColorLoc;
    private int _modelTexLoc;
    private int _modelUseTexLoc;
    private int _modelLightDirLoc;
    private int _modelViewDirLoc;
    private int _modelShadeShiftLoc;
    private int _modelShadeToonyLoc;
    private int _modelRimIntensityLoc;
    private int _modelAmbientLoc;
    private Matrix4 _modelTransform = Matrix4.Identity;
    public Matrix4 ModelTransform
    {
        get => _modelTransform;
        set => _modelTransform = value;
    }
    private int _width;
    private int _height;
    private Matrix4 _viewMatrix = Matrix4.Identity;
    private Matrix4 _projMatrix = Matrix4.Identity;
    private Matrix4 _cameraRot = Matrix4.Identity;
    private Vector3 _cameraPos;
    private bool _viewProjDirty = true;
    private readonly List<Vector3> _boneRotations = new();
    private readonly List<Vector3> _boneTranslations = new();
    private bool _bonesDirty;
    private bool _morphDirty;
    private List<MiniMikuDance.Import.BoneData> _bones = new();
    private readonly Dictionary<int, string> _indexToHumanoidName = new();
    private float[] _tmpVertexBuffer = Array.Empty<float>();
    public BonesConfig? BonesConfig { get; set; }
    private Quaternion _externalRotation = Quaternion.Identity;
    // デフォルトのカメラ感度をスライダーの最小値に合わせる
    public float RotateSensitivity { get; set; } = 0.1f;
    public float PanSensitivity { get; set; } = 1f;
    public float ZoomSensitivity { get; set; } = 1f;
    public float ShadeShift { get; set; } = -0.1f;
    public float ShadeToony { get; set; } = 0.9f;
    public float RimIntensity { get; set; } = 0.5f;
    public float Ambient { get; set; } = 0.3f;
    private bool _showBoneOutline;
    public bool ShowBoneOutline
    {
        get => _showBoneOutline;
        set
        {
            if (_showBoneOutline != value)
            {
                _showBoneOutline = value;
                // Ensure bones buffer gets (re)built on next frame
                _bonesDirty = true;
                Viewer?.InvalidateSurface();
            }
        }
    }

    private float _stageSize = AppSettings.DefaultStageSize;
    public float StageSize
    {
        get => _stageSize;
        set
        {
            if (_stageSize != value)
            {
                _stageSize = value;
                if (_program != 0)
                    GenerateGrid();
            }
        }
    }

    private float _defaultCameraDistance = AppSettings.DefaultCameraDistance;
    public float DefaultCameraDistance
    {
        get => _defaultCameraDistance;
        set => _defaultCameraDistance = value;
    }

    private float _defaultCameraTargetY = AppSettings.DefaultCameraTargetY;
    public float DefaultCameraTargetY
    {
        get => _defaultCameraTargetY;
        set => _defaultCameraTargetY = value;
    }

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

    public void Initialize()
    {
        const string vert = @"#version 300 es
layout(location = 0) in vec3 aPosition;
uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProj;
void main(){
    gl_Position = uProj * uView * uModel * vec4(aPosition,1.0);
}";
        const string frag = @"#version 300 es
precision mediump float;
uniform vec4 uColor;
out vec4 FragColor;
void main(){
    FragColor = uColor;
}";
        int vs = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vs, vert);
        GL.CompileShader(vs);
        int fs = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fs, frag);
        GL.CompileShader(fs);
        _program = GL.CreateProgram();
        GL.AttachShader(_program, vs);
        GL.AttachShader(_program, fs);
        GL.LinkProgram(_program);
        GL.DeleteShader(vs);
        GL.DeleteShader(fs);
        _modelLoc = GL.GetUniformLocation(_program, "uModel");
        _viewLoc = GL.GetUniformLocation(_program, "uView");
        _projLoc = GL.GetUniformLocation(_program, "uProj");
        _colorLoc = GL.GetUniformLocation(_program, "uColor");

        // 透過描画設定（デフォルトでは無効）
        GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

        const string modelVert = @"#version 300 es
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aTex;
uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProj;
out vec3 vNormal;
out vec2 vTex;
void main(){
    vec4 pos = uModel * vec4(aPosition,1.0);
    vNormal = mat3(uModel) * aNormal;
    vTex = aTex;
    gl_Position = uProj * uView * pos;
}";
        const string modelFrag = @"#version 300 es
precision mediump float;
in vec3 vNormal;
in vec2 vTex;
uniform vec4 uColor;
uniform sampler2D uTex;
uniform bool uUseTex;
uniform vec3 uLightDir;
uniform vec3 uViewDir;
uniform float uShadeShift;
uniform float uShadeToony;
uniform float uRimIntensity;
uniform float uAmbient;
out vec4 FragColor;
void main(){
    vec4 base = uUseTex ? texture(uTex, vTex) : uColor;
    float ndotl = max(dot(normalize(vNormal), normalize(uLightDir)), 0.0);
    float light = clamp((ndotl + uShadeShift) * uShadeToony, 0.0, 1.0);
    float rim = pow(1.0 - max(dot(normalize(vNormal), normalize(uViewDir)), 0.0), 3.0) * uRimIntensity;
    vec3 color = base.rgb * (light + uAmbient) + base.rgb * rim;
    FragColor = vec4(color, base.a);
}";
        int mvs = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(mvs, modelVert);
        GL.CompileShader(mvs);
        int mfs = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(mfs, modelFrag);
        GL.CompileShader(mfs);
        _modelProgram = GL.CreateProgram();
        GL.AttachShader(_modelProgram, mvs);
        GL.AttachShader(_modelProgram, mfs);
        GL.LinkProgram(_modelProgram);
        GL.DeleteShader(mvs);
        GL.DeleteShader(mfs);
        _modelViewLoc = GL.GetUniformLocation(_modelProgram, "uView");
        _modelProjLoc = GL.GetUniformLocation(_modelProgram, "uProj");
        _modelMatrixLoc = GL.GetUniformLocation(_modelProgram, "uModel");
        _modelColorLoc = GL.GetUniformLocation(_modelProgram, "uColor");
        _modelTexLoc = GL.GetUniformLocation(_modelProgram, "uTex");
        _modelUseTexLoc = GL.GetUniformLocation(_modelProgram, "uUseTex");
        _modelLightDirLoc = GL.GetUniformLocation(_modelProgram, "uLightDir");
        _modelViewDirLoc = GL.GetUniformLocation(_modelProgram, "uViewDir");
        _modelShadeShiftLoc = GL.GetUniformLocation(_modelProgram, "uShadeShift");
        _modelShadeToonyLoc = GL.GetUniformLocation(_modelProgram, "uShadeToony");
        _modelRimIntensityLoc = GL.GetUniformLocation(_modelProgram, "uRimIntensity");
        _modelAmbientLoc = GL.GetUniformLocation(_modelProgram, "uAmbient");

        GenerateGrid();
    }

    private void GenerateGrid()
    {
        int range = (int)_stageSize;
        _gridVertexCount = (range * 2 + 1) * 4;
        float[] grid = new float[_gridVertexCount * 3];
        int idx = 0;
        for (int i = -range; i <= range; i++)
        {
            grid[idx++] = i; grid[idx++] = 0f; grid[idx++] = -range;
            grid[idx++] = i; grid[idx++] = 0f; grid[idx++] = range;
            grid[idx++] = -range; grid[idx++] = 0f; grid[idx++] = i;
            grid[idx++] = range; grid[idx++] = 0f; grid[idx++] = i;
        }
        if (_gridVao == 0) _gridVao = GL.GenVertexArray();
        if (_gridVbo == 0) _gridVbo = GL.GenBuffer();
        GL.BindVertexArray(_gridVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _gridVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, grid.Length * sizeof(float), grid, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);

        float r = _stageSize;
        float[] plane =
        {
            -r, 0f, -r,
             r, 0f, -r,
            -r, 0f,  r,
             r, 0f, -r,
             r, 0f,  r,
            -r, 0f,  r
        };
        _groundVertexCount = 6;
        if (_groundVao == 0) _groundVao = GL.GenVertexArray();
        if (_groundVbo == 0) _groundVbo = GL.GenBuffer();
        GL.BindVertexArray(_groundVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _groundVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, plane.Length * sizeof(float), plane, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
        GL.Viewport(0, 0, width, height);
        _viewProjDirty = true;
    }

    public void Orbit(float dx, float dy)
    {
        _orbitY -= dx * 0.01f * RotateSensitivity;
        _orbitX -= dy * 0.01f * RotateSensitivity;
        // Clamp pitch to [-90°, 90°]
        float limit = MathF.PI * 0.5f;
        if (_orbitX < -limit) _orbitX = -limit;
        if (_orbitX > limit) _orbitX = limit;
        _viewProjDirty = true;
    }

    public void Pan(float dx, float dy)
    {
        Matrix4 rot = Matrix4.CreateFromQuaternion(_externalRotation) *
                     Matrix4.CreateRotationX(_orbitX) *
                     Matrix4.CreateRotationY(_orbitY);
        Vector3 right = Vector3.TransformNormal(Vector3.UnitX, rot);
        Vector3 up = Vector3.TransformNormal(Vector3.UnitY, rot);
        _target += (-right * dx + up * dy) * 0.01f * PanSensitivity;
        _viewProjDirty = true;
    }

    public void Dolly(float delta)
    {
        _distance -= delta * 0.01f * MathF.Max(0.01f, ZoomSensitivity);
        if (_distance < 1f) _distance = 1f;
        if (_distance > 100f) _distance = 100f;
        _viewProjDirty = true;
    }

    public void ResetCamera()
    {
        _orbitX = 0f;
        _orbitY = 0f;
        _target = new Vector3(0f, _defaultCameraTargetY, 0f);
        _distance = _defaultCameraDistance;
        if (_distance < 1f) _distance = 1f;
        if (_distance > 100f) _distance = 100f;
        _externalRotation = Quaternion.Identity;
        _viewProjDirty = true;
    }

    public void SetExternalRotation(Quaternion q)
    {
        _externalRotation = q;
        _viewProjDirty = true;
    }

    public void ClearBoneRotations()
    {
        _boneRotations.Clear();
        _boneTranslations.Clear();
        _bonesDirty = true;
    }

    public void SetBoneRotation(int index, Vector3 degrees)
    {
        if (index < 0)
            return;
        if (BonesConfig != null && index < _bones.Count)
        {
            var name = _indexToHumanoidName.TryGetValue(index, out var n) ? n : _bones[index].Name;
            var clamped = BonesConfig.Clamp(name, degrees.ToNumerics());
            degrees = clamped.ToOpenTK();
        }
        while (_boneRotations.Count <= index)
            _boneRotations.Add(Vector3.Zero);
        _boneRotations[index] = degrees;
        _bonesDirty = true;
    }

    public void SetBoneTranslation(int index, Vector3 translation)
    {
        if (index < 0)
            return;
        while (_boneTranslations.Count <= index)
            _boneTranslations.Add(Vector3.Zero);
        _boneTranslations[index] = translation;
        _bonesDirty = true;
    }

    public Vector3 GetBoneRotation(int index)
    {
        if (index < 0 || index >= _boneRotations.Count)
            return Vector3.Zero;
        return _boneRotations[index];
    }

    public Vector3 GetBoneTranslation(int index)
    {
        if (index < 0 || index >= _boneTranslations.Count)
            return Vector3.Zero;
        return _boneTranslations[index];
    }

    public void SetMorph(string name, float value)
    {
        if (!_morphs.TryGetValue(name, out var morph) || morph.Type != MorphType.Vertex)
            return;

        value = Math.Clamp(value, 0f, 1f);
        if (MathF.Abs(value) < 1e-5f) value = 0f;

        _morphValues[name] = value;
        _morphDirty = true;

        // Recompute affected vertices from base with all contributing morphs
        foreach (var off in morph.Offsets)
        {
            int vid = off.Index;
            _changedOriginalVertices.Add(vid);
            if (_morphVertexMap.TryGetValue(vid, out var list) && _vertexMorphContribs.TryGetValue(vid, out var contribs))
            {
                Vector3 total = Vector3.Zero;
                for (int i = 0; i < contribs.Count; i++)
                {
                    var (ofs, morphName) = contribs[i];
                    if (_morphValues.TryGetValue(morphName, out var w) && w != 0f)
                        total += ofs * w;
                }
                foreach (var (mesh, idx) in list)
                {
                    mesh.Vertices[idx] = mesh.BaseVertices[idx] + total;
                }
            }
        }

        Viewer?.InvalidateSurface();
    }

    public void LoadModel(MiniMikuDance.Import.ModelData data)
    {
        foreach (var rm in _meshes)
        {
            if (rm.Vao != 0) GL.DeleteVertexArray(rm.Vao);
            if (rm.Vbo != 0) GL.DeleteBuffer(rm.Vbo);
            if (rm.Ebo != 0) GL.DeleteBuffer(rm.Ebo);
            if (rm.Texture != 0) GL.DeleteTexture(rm.Texture);
        }
        _meshes.Clear();
        _indexToHumanoidName.Clear();
        _bones = data.Bones;
        _worldMats = new System.Numerics.Matrix4x4[_bones.Count];
        _skinMats = new System.Numerics.Matrix4x4[_bones.Count];
        foreach (var (name, idx) in data.HumanoidBoneList)
        {
            _indexToHumanoidName[idx] = name;
        }

        _modelTransform = data.Transform.ToMatrix4();

        if (data.SubMeshes.Count == 0)
        {
            data.SubMeshes.Add(new MiniMikuDance.Import.SubMeshData
            {
                Mesh = data.Mesh
            });
        }

        foreach (var sm in data.SubMeshes)
        {
            int vcount = sm.Mesh.VertexCount;
            float[] verts = new float[vcount * 8];
            for (int i = 0; i < vcount; i++)
            {
                var v = sm.Mesh.Vertices[i];
                verts[i * 8 + 0] = v.X;
                verts[i * 8 + 1] = v.Y;
                verts[i * 8 + 2] = v.Z;
                if (i < sm.Mesh.Normals.Count)
                {
                    var n = sm.Mesh.Normals[i];
                    verts[i * 8 + 3] = n.X;
                    verts[i * 8 + 4] = n.Y;
                    verts[i * 8 + 5] = n.Z;
                }
                else
                {
                    verts[i * 8 + 3] = 0f;
                    verts[i * 8 + 4] = 0f;
                    verts[i * 8 + 5] = 1f;
                }
                if (i < sm.TexCoords.Count)
                {
                    var uv = sm.TexCoords[i];
                    verts[i * 8 + 6] = uv.X;
                    verts[i * 8 + 7] = uv.Y;
                }
                else
                {
                    verts[i * 8 + 6] = 0f;
                    verts[i * 8 + 7] = 0f;
                }
            }

            var indices = new System.Collections.Generic.List<uint>();
            foreach (var f in sm.Mesh.Faces)
            {
                foreach (var idx in f.Indices)
                    indices.Add((uint)idx);
            }

            var rm = new RenderMesh();
            rm.IndexCount = indices.Count;
            rm.Vertices = sm.Mesh.Vertices.Select(v => new Vector3(v.X, v.Y, v.Z)).ToArray();
            rm.BaseVertices = rm.Vertices.ToArray();
            rm.Normals = sm.Mesh.Normals.Select(n => new Vector3(n.X, n.Y, n.Z)).ToArray();
            rm.TexCoords = sm.TexCoords.Select(t => new Vector2(t.X, t.Y)).ToArray();
            rm.JointIndices = sm.JointIndices.Select(j => new Vector4(j.X, j.Y, j.Z, j.W)).ToArray();
            rm.JointWeights = sm.JointWeights.Select(w => new Vector4(w.X, w.Y, w.Z, w.W)).ToArray();
            rm.Vao = GL.GenVertexArray();
            rm.Vbo = GL.GenBuffer();
            rm.Ebo = GL.GenBuffer();
            rm.Color = sm.ColorFactor.ToVector4();

            GL.BindVertexArray(rm.Vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, rm.Vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, verts.Length * sizeof(float), verts, BufferUsageHint.StaticDraw);
            int stride = 8 * sizeof(float);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));
            GL.EnableVertexAttribArray(2);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, rm.Ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Count * sizeof(uint), indices.ToArray(), BufferUsageHint.StaticDraw);
            GL.BindVertexArray(0);

            if (sm.TextureBytes != null)
            {
                rm.Texture = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, rm.Texture);
                var handle = System.Runtime.InteropServices.GCHandle.Alloc(sm.TextureBytes, System.Runtime.InteropServices.GCHandleType.Pinned);
                try
                {
#pragma warning disable CS0618
                    GL.TexImage2D(
                        (All)TextureTarget.Texture2D,
                        0,
                        (All)PixelInternalFormat.Rgba,
                        sm.TextureWidth,
                        sm.TextureHeight,
                        0,
                        (All)PixelFormat.Rgba,
                        (All)PixelType.UnsignedByte,
                        handle.AddrOfPinnedObject());
#pragma warning restore CS0618
                }
                finally
                {
                    handle.Free();
                }
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                rm.HasTexture = true;
            }

            _meshes.Add(rm);
        }

        int maxVertices = _meshes.Count > 0 ? _meshes.Max(m => m.Vertices.Length) : 0;
        _tmpVertexBuffer = new float[maxVertices * 8];

        _morphs.Clear();
        _morphValues.Clear();
        _morphVertexMap.Clear();
        _vertexMorphContribs.Clear();
        foreach (var morph in data.Morphs)
        {
            if (morph.Type != MorphType.Vertex) continue;
            _morphs[morph.Name] = morph;
            foreach (var off in morph.Offsets)
            {
                if (!_vertexMorphContribs.TryGetValue(off.Index, out var lst))
                {
                    lst = new List<(Vector3, string)>();
                    _vertexMorphContribs[off.Index] = lst;
                }
                lst.Add((new Vector3(off.Offset.X, off.Offset.Y, off.Offset.Z), morph.Name));
            }
        }

        var lookup = new Dictionary<(Vector3 Pos, Vector3 Nor, Vector2 Uv), List<int>>();
        var meshVerts = data.Mesh.Vertices;
        var meshNorms = data.Mesh.Normals;
        var meshUVs = data.Mesh.TextureCoordinateChannels[0];
        for (int i = 0; i < data.Mesh.VertexCount; i++)
        {
            var pos = new Vector3(meshVerts[i].X, meshVerts[i].Y, meshVerts[i].Z);
            var nor = i < meshNorms.Count ? new Vector3(meshNorms[i].X, meshNorms[i].Y, meshNorms[i].Z) : Vector3.Zero;
            var uv = i < meshUVs.Count ? new Vector2(meshUVs[i].X, meshUVs[i].Y) : Vector2.Zero;
            var key = (pos, nor, uv);
            if (!lookup.TryGetValue(key, out var list))
            {
                list = new List<int>();
                lookup[key] = list;
            }
            list.Add(i);
        }

        foreach (var rm in _meshes)
        {
            for (int i = 0; i < rm.Vertices.Length; i++)
            {
                var pos = rm.BaseVertices[i];
                var nor = i < rm.Normals.Length ? rm.Normals[i] : Vector3.Zero;
                var uv = i < rm.TexCoords.Length ? rm.TexCoords[i] : Vector2.Zero;
                var key = (pos, nor, uv);
                if (lookup.TryGetValue(key, out var idxList))
                {
                    foreach (var idx in idxList)
                    {
                        if (!_morphVertexMap.TryGetValue(idx, out var l))
                        {
                            l = new List<(RenderMesh, int)>();
                            _morphVertexMap[idx] = l;
                        }
                        l.Add((rm, i));
                    }
                }
            }
        }

        // Auto-fit camera using bone parents' bind positions (preferred),
        // falling back to mesh bounds if unavailable.
        try
        {
            bool fitDone = false;
            if (_bones.Count > 0)
            {
                var isParent = new bool[_bones.Count];
                foreach (var b in _bones)
                {
                    if (b.Parent >= 0 && b.Parent < _bones.Count) isParent[b.Parent] = true;
                }

                System.Numerics.Vector3? minN = null;
                System.Numerics.Vector3? maxN = null;
                for (int i = 0; i < _bones.Count; i++)
                {
                    if (!isParent[i]) continue;
                    var p = _bones[i].BindMatrix.Translation;
                    if (minN == null)
                    {
                        minN = p; maxN = p;
                    }
                    else
                    {
                        minN = System.Numerics.Vector3.Min(minN.Value, p);
                        maxN = System.Numerics.Vector3.Max(maxN.Value, p);
                    }
                }

                if (minN.HasValue && maxN.HasValue)
                {
                    var center = (minN.Value + maxN.Value) * 0.5f;
                    var ext = maxN.Value - minN.Value;
                    float radius = 0.5f * ext.Length();
                    if (radius < 0.01f) radius = 1f;
                    float fov = MathF.PI / 4f; // must match projection
                    float dist = (radius / MathF.Tan(fov * 0.5f)) * 1.2f; // margin
                    // Only respect height (Y). X and Z centered to 0 per request.
                    _target = new Vector3(0f, center.Y, 0f);
                    _distance = Math.Clamp(dist, 1f, 100f);
                    _orbitX = 0f;
                    _orbitY = 0f;
                    _viewProjDirty = true;
                    fitDone = true;
                }
            }

            if (!fitDone && data.Mesh != null && data.Mesh.Vertices.Count > 0)
            {
                var v0 = data.Mesh.Vertices[0];
                System.Numerics.Vector3 min = new(v0.X, v0.Y, v0.Z);
                System.Numerics.Vector3 max = min;
                foreach (var v in data.Mesh.Vertices)
                {
                    var p = new System.Numerics.Vector3(v.X, v.Y, v.Z);
                    min = System.Numerics.Vector3.Min(min, p);
                    max = System.Numerics.Vector3.Max(max, p);
                }
                var center = (min + max) * 0.5f;
                var ext = max - min;
                float radius = 0.5f * ext.Length();
                if (radius < 0.01f) radius = 1f;
                float fov = MathF.PI / 4f;
                float dist = (radius / MathF.Tan(fov * 0.5f)) * 1.2f;
                _target = new Vector3(0f, center.Y, 0f);
                _distance = Math.Clamp(dist, 1f, 100f);
                _orbitX = 0f;
                _orbitY = 0f;
                _viewProjDirty = true;
            }
        }
        catch { /* ignore fit errors */ }
    }

    public void Render()
    {
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
        GL.Enable(EnableCap.Blend); // 半透明描画のためブレンドを有効化
        GL.FrontFace(FrontFaceDirection.Ccw);
        GL.ClearColor(1f, 1f, 1f, 1f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        if (_viewProjDirty)
        {
            _cameraRot = Matrix4.CreateFromQuaternion(_externalRotation) *
                         Matrix4.CreateRotationX(_orbitX) *
                         Matrix4.CreateRotationY(_orbitY);
            _cameraPos = Vector3.TransformPosition(new Vector3(0, 0, _distance), _cameraRot) + _target;
            _viewMatrix = Matrix4.LookAt(_cameraPos, _target, Vector3.UnitY);
            float aspect = _width == 0 || _height == 0 ? 1f : _width / (float)_height;
            _projMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspect, 0.1f, 100f);
            _viewProjDirty = false;
        }
        var modelMat = ModelTransform;

        bool needsUpdate = _bonesDirty || _morphDirty;

        // CPU スキニングと頂点バッファ更新
        if (needsUpdate && _bones.Count > 0)
        {
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
                System.Numerics.Vector3 trans = bone.Translation;
                if (i < _boneTranslations.Count)
                    trans += _boneTranslations[i].ToNumerics();
                var local = System.Numerics.Matrix4x4.CreateFromQuaternion(bone.Rotation * delta) * System.Numerics.Matrix4x4.CreateTranslation(trans);
                if (bone.Parent >= 0)
                    _worldMats[i] = local * _worldMats[bone.Parent];
                else
                    _worldMats[i] = local;
            }

            for (int i = 0; i < _bones.Count; i++)
                _skinMats[i] = _bones[i].InverseBindMatrix * _worldMats[i];

            if (_bonesDirty)
            {
                foreach (var rm in _meshes)
                {
                    if (rm.JointIndices.Length != rm.Vertices.Length)
                        continue;
                    int required = rm.Vertices.Length * 8;
                    if (_tmpVertexBuffer.Length < required)
                        _tmpVertexBuffer = new float[required];
                    else
                        Array.Clear(_tmpVertexBuffer, 0, required);
                    for (int vi = 0; vi < rm.Vertices.Length; vi++)
                    {
                        var pos = System.Numerics.Vector3.Zero;
                        var norm = System.Numerics.Vector3.Zero;
                        var jp = rm.JointIndices[vi];
                        var jw = rm.JointWeights[vi];
                        for (int k = 0; k < 4; k++)
                        {
                            int bi = (int)jp[k];
                            float w = jw[k];
                            if (bi >= 0 && bi < _skinMats.Length && w > 0f)
                            {
                                var m = _skinMats[bi];
                                pos += System.Numerics.Vector3.Transform(rm.Vertices[vi].ToNumerics(), m) * w;
                                norm += System.Numerics.Vector3.TransformNormal(rm.Normals[vi].ToNumerics(), m) * w;
                            }
                        }
                        if (norm.LengthSquared() > 0)
                            norm = System.Numerics.Vector3.Normalize(norm);

                        _tmpVertexBuffer[vi * 8 + 0] = pos.X;
                        _tmpVertexBuffer[vi * 8 + 1] = pos.Y;
                        _tmpVertexBuffer[vi * 8 + 2] = pos.Z;
                        _tmpVertexBuffer[vi * 8 + 3] = norm.X;
                        _tmpVertexBuffer[vi * 8 + 4] = norm.Y;
                        _tmpVertexBuffer[vi * 8 + 5] = norm.Z;
                        if (vi < rm.TexCoords.Length)
                        {
                            _tmpVertexBuffer[vi * 8 + 6] = rm.TexCoords[vi].X;
                            _tmpVertexBuffer[vi * 8 + 7] = rm.TexCoords[vi].Y;
                        }
                        else
                        {
                            _tmpVertexBuffer[vi * 8 + 6] = 0f;
                            _tmpVertexBuffer[vi * 8 + 7] = 0f;
                        }
                    }
                    GL.BindBuffer(BufferTarget.ArrayBuffer, rm.Vbo);
                    var handle = System.Runtime.InteropServices.GCHandle.Alloc(_tmpVertexBuffer, System.Runtime.InteropServices.GCHandleType.Pinned);
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
            else if (_morphDirty && _changedOriginalVertices.Count > 0)
            {
                var small = new float[8];
                var handleSmall = System.Runtime.InteropServices.GCHandle.Alloc(small, System.Runtime.InteropServices.GCHandleType.Pinned);
                try
                {
                    foreach (var origIdx in _changedOriginalVertices)
                    {
                        if (!_morphVertexMap.TryGetValue(origIdx, out var mapped)) continue;
                        foreach (var (rm, vi) in mapped)
                        {
                            var pos = System.Numerics.Vector3.Zero;
                            var norm = System.Numerics.Vector3.Zero;
                            var jp = rm.JointIndices[vi];
                            var jw = rm.JointWeights[vi];
                            for (int k = 0; k < 4; k++)
                            {
                                int bi = (int)jp[k];
                                float w = jw[k];
                                if (bi >= 0 && bi < _skinMats.Length && w > 0f)
                                {
                                    var m = _skinMats[bi];
                                    pos += System.Numerics.Vector3.Transform(rm.Vertices[vi].ToNumerics(), m) * w;
                                    norm += System.Numerics.Vector3.TransformNormal(rm.Normals[vi].ToNumerics(), m) * w;
                                }
                            }
                            if (norm.LengthSquared() > 0)
                                norm = System.Numerics.Vector3.Normalize(norm);

                            small[0] = pos.X; small[1] = pos.Y; small[2] = pos.Z;
                            small[3] = norm.X; small[4] = norm.Y; small[5] = norm.Z;
                            if (vi < rm.TexCoords.Length)
                            { small[6] = rm.TexCoords[vi].X; small[7] = rm.TexCoords[vi].Y; }
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

            _bonesDirty = false;
            _morphDirty = false;
            _changedOriginalVertices.Clear();
        }
        else if (needsUpdate)
        {
            // No bones: just push morphed (or base) vertices to GPU
            foreach (var rm in _meshes)
            {
                int required = rm.Vertices.Length * 8;
                if (_tmpVertexBuffer.Length < required)
                    _tmpVertexBuffer = new float[required];
                else
                    Array.Clear(_tmpVertexBuffer, 0, required);
                for (int vi = 0; vi < rm.Vertices.Length; vi++)
                {
                    var pos = rm.Vertices[vi];
                    var nor = vi < rm.Normals.Length ? rm.Normals[vi] : new Vector3(0, 0, 1);
                    _tmpVertexBuffer[vi * 8 + 0] = pos.X;
                    _tmpVertexBuffer[vi * 8 + 1] = pos.Y;
                    _tmpVertexBuffer[vi * 8 + 2] = pos.Z;
                    _tmpVertexBuffer[vi * 8 + 3] = nor.X;
                    _tmpVertexBuffer[vi * 8 + 4] = nor.Y;
                    _tmpVertexBuffer[vi * 8 + 5] = nor.Z;
                    if (vi < rm.TexCoords.Length)
                    {
                        _tmpVertexBuffer[vi * 8 + 6] = rm.TexCoords[vi].X;
                        _tmpVertexBuffer[vi * 8 + 7] = rm.TexCoords[vi].Y;
                    }
                    else
                    {
                        _tmpVertexBuffer[vi * 8 + 6] = 0f;
                        _tmpVertexBuffer[vi * 8 + 7] = 0f;
                    }
                }
                GL.BindBuffer(BufferTarget.ArrayBuffer, rm.Vbo);
                var handle = System.Runtime.InteropServices.GCHandle.Alloc(_tmpVertexBuffer, System.Runtime.InteropServices.GCHandleType.Pinned);
                try
                {
                    GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, required * sizeof(float), handle.AddrOfPinnedObject());
                }
                finally
                {
                    handle.Free();
                }
            }
            _morphDirty = false;
            _changedOriginalVertices.Clear();
        }

        GL.UseProgram(_modelProgram);
        GL.UniformMatrix4(_modelViewLoc, false, ref _viewMatrix);
        GL.UniformMatrix4(_modelProjLoc, false, ref _projMatrix);
        // ライトと視線方向をカメラ角度に合わせて更新
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
    }

    public void Dispose()
    {
        foreach (var rm in _meshes)
        {
            if (rm.Vao != 0) GL.DeleteVertexArray(rm.Vao);
            if (rm.Vbo != 0) GL.DeleteBuffer(rm.Vbo);
            if (rm.Ebo != 0) GL.DeleteBuffer(rm.Ebo);
        }
        _meshes.Clear();
        _indexToHumanoidName.Clear();
        GL.DeleteBuffer(_gridVbo);
        GL.DeleteBuffer(_groundVbo);
        GL.DeleteBuffer(_boneVbo);
        GL.DeleteVertexArray(_gridVao);
        GL.DeleteVertexArray(_groundVao);
        GL.DeleteVertexArray(_boneVao);
        GL.DeleteProgram(_program);
        GL.DeleteProgram(_modelProgram);
    }
}
