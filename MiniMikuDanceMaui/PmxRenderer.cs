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
    }
    private readonly System.Collections.Generic.List<RenderMesh> _meshes = new();
    private readonly Dictionary<string, MorphData> _morphs = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, float> _morphValues = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, List<(RenderMesh Mesh, int Index)>> _morphVertexMap = new();
    private bool _morphDirty;
    public SKGLView? Viewer { get; set; }
    private int _gridVao;
    private int _gridVbo;
    private int _gridVertexCount;
    private int _modelLoc;
    private int _viewLoc;
    private int _projLoc;
    private int _colorLoc;
    private float _orbitX;
    // 初期カメラ位置はモデルの正面が表示されるようY軸回転を180度に設定
    private float _orbitY = MathF.PI;
    private float _distance = 4f;
    // モデル中心より少し高い位置を基準にカメラを配置する
    private Vector3 _target = new Vector3(0f, 0.5f, 0f);
    private int _groundVao;
    private int _groundVbo;
    private int _groundVertexCount;
    private int _boneVao;
    private int _boneVbo;
    private int _boneVertexCount;
    private float[] _boneLineVertices = Array.Empty<float>();
    private int[] _boneLinePairs = Array.Empty<int>();
    private float[] _boneArray = Array.Empty<float>();
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
    private int _modelBonesLoc;
    private Matrix4 _modelTransform = Matrix4.Identity;
    public Matrix4 ModelTransform
    {
        get => _modelTransform;
        set => _modelTransform = value;
    }
    private int _width;
    private int _height;
    private readonly List<Vector3> _boneRotations = new();
    private readonly List<Vector3> _boneTranslations = new();
    private List<MiniMikuDance.Import.BoneData> _bones = new();
    private readonly Dictionary<int, string> _indexToHumanoidName = new();
    public BonesConfig? BonesConfig { get; set; }
    private Quaternion _externalRotation = Quaternion.Identity;
    // デフォルトのカメラ感度をスライダーの最小値に合わせる
    public float RotateSensitivity { get; set; } = 0.1f;
    public float PanSensitivity { get; set; } = 1f;
    public float ShadeShift { get; set; } = -0.1f;
    public float ShadeToony { get; set; } = 0.9f;
    public float RimIntensity { get; set; } = 0.5f;
    public float Ambient { get; set; } = 0.3f;
    public bool ShowBoneOutline { get; set; }

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
#define MAX_BONES 256
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aTex;
layout(location = 3) in vec4 aJointIndices;
layout(location = 4) in vec4 aJointWeights;
uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProj;
uniform mat4 uBones[MAX_BONES];
out vec3 vNormal;
out vec2 vTex;
void main(){
    mat4 skin =
        aJointWeights.x * uBones[int(aJointIndices.x)] +
        aJointWeights.y * uBones[int(aJointIndices.y)] +
        aJointWeights.z * uBones[int(aJointIndices.z)] +
        aJointWeights.w * uBones[int(aJointIndices.w)];
    vec4 pos = uModel * skin * vec4(aPosition,1.0);
    vNormal = mat3(uModel * skin) * aNormal;
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
        _modelBonesLoc = GL.GetUniformLocation(_modelProgram, "uBones");

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
    }

    public void Orbit(float dx, float dy)
    {
        _orbitY -= dx * 0.01f * RotateSensitivity;
        _orbitX -= dy * 0.01f * RotateSensitivity;
    }

    public void Pan(float dx, float dy)
    {
        Matrix4 rot = Matrix4.CreateFromQuaternion(_externalRotation) *
                     Matrix4.CreateRotationX(_orbitX) *
                     Matrix4.CreateRotationY(_orbitY);
        Vector3 right = Vector3.TransformNormal(Vector3.UnitX, rot);
        Vector3 up = Vector3.TransformNormal(Vector3.UnitY, rot);
        _target += (-right * dx + up * dy) * 0.01f * PanSensitivity;
    }

    public void Dolly(float delta)
    {
        _distance -= delta * 0.01f;
        if (_distance < 1f) _distance = 1f;
        if (_distance > 100f) _distance = 100f;
    }

    public void ResetCamera()
    {
        _orbitX = 0f;
        _orbitY = MathF.PI;
        _target = new Vector3(0f, _defaultCameraTargetY, 0f);
        _distance = _defaultCameraDistance;
        if (_distance < 1f) _distance = 1f;
        if (_distance > 100f) _distance = 100f;
        _externalRotation = Quaternion.Identity;
    }

    public void SetExternalRotation(Quaternion q)
    {
        _externalRotation = q;
    }

    public void ClearBoneRotations()
    {
        _boneRotations.Clear();
        _boneTranslations.Clear();
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
        _morphDirty = true;
    }

    public void SetBoneTranslation(int index, Vector3 translation)
    {
        if (index < 0)
            return;
        while (_boneTranslations.Count <= index)
            _boneTranslations.Add(Vector3.Zero);
        _boneTranslations[index] = translation;
        _morphDirty = true;
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

        _morphValues.TryGetValue(name, out var oldValue);
        if (MathF.Abs(oldValue - value) < 1e-6f)
            return;

        _morphValues[name] = value;
        float delta = value - oldValue;

        var updateMap = new Dictionary<RenderMesh, HashSet<int>>();
        foreach (var off in morph.Offsets)
        {
            if (_morphVertexMap.TryGetValue(off.Index, out var list))
            {
                var offset = new Vector3(off.Offset.X, off.Offset.Y, off.Offset.Z) * delta;
                foreach (var (mesh, idx) in list)
                {
                    mesh.Vertices[idx] += offset;
                    if (!updateMap.TryGetValue(mesh, out var set))
                    {
                        set = new HashSet<int>();
                        updateMap[mesh] = set;
                    }
                    set.Add(idx);
                }
            }
        }

        foreach (var kv in updateMap)
        {
            var mesh = kv.Key;
            GL.BindBuffer(BufferTarget.ArrayBuffer, mesh.Vbo);
            foreach (var idx in kv.Value)
            {
                var v = mesh.Vertices[idx];
                float[] buf = { v.X, v.Y, v.Z };
                GL.BufferSubData(BufferTarget.ArrayBuffer, (IntPtr)(idx * 16 * sizeof(float)), buf.Length * sizeof(float), buf);
            }
        }
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

        _morphDirty = true;
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
        _bones = data.Bones.ToList();
        _boneArray = Array.Empty<float>();
        if (_boneVao != 0) { GL.DeleteVertexArray(_boneVao); _boneVao = 0; }
        if (_boneVbo != 0) { GL.DeleteBuffer(_boneVbo); _boneVbo = 0; }
        var pairList = new List<int>();
        for (int i = 0; i < _bones.Count; i++)
        {
            var bone = _bones[i];
            if (bone.Parent >= 0)
            {
                pairList.Add(bone.Parent);
                pairList.Add(i);
            }
        }
        _boneLinePairs = pairList.ToArray();
        _boneLineVertices = new float[_boneLinePairs.Length / 2 * 6];
        _boneVertexCount = _boneLineVertices.Length / 3;
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
            float[] verts = new float[vcount * 16];
            for (int i = 0; i < vcount; i++)
            {
                var v = sm.Mesh.Vertices[i];
                verts[i * 16 + 0] = v.X;
                verts[i * 16 + 1] = v.Y;
                verts[i * 16 + 2] = v.Z;
                if (i < sm.Mesh.Normals.Count)
                {
                    var n = sm.Mesh.Normals[i];
                    verts[i * 16 + 3] = n.X;
                    verts[i * 16 + 4] = n.Y;
                    verts[i * 16 + 5] = n.Z;
                }
                else
                {
                    verts[i * 16 + 3] = 0f;
                    verts[i * 16 + 4] = 0f;
                    verts[i * 16 + 5] = 1f;
                }
                if (i < sm.TexCoords.Count)
                {
                    var uv = sm.TexCoords[i];
                    verts[i * 16 + 6] = uv.X;
                    verts[i * 16 + 7] = uv.Y;
                }
                else
                {
                    verts[i * 16 + 6] = 0f;
                    verts[i * 16 + 7] = 0f;
                }
                if (i < sm.JointIndices.Count)
                {
                    var j = sm.JointIndices[i];
                    verts[i * 16 + 8] = j.X;
                    verts[i * 16 + 9] = j.Y;
                    verts[i * 16 +10] = j.Z;
                    verts[i * 16 +11] = j.W;
                }
                else
                {
                    verts[i * 16 + 8] = 0f;
                    verts[i * 16 + 9] = 0f;
                    verts[i * 16 +10] = 0f;
                    verts[i * 16 +11] = 0f;
                }
                if (i < sm.JointWeights.Count)
                {
                    var w = sm.JointWeights[i];
                    verts[i * 16 +12] = w.X;
                    verts[i * 16 +13] = w.Y;
                    verts[i * 16 +14] = w.Z;
                    verts[i * 16 +15] = w.W;
                }
                else
                {
                    verts[i * 16 +12] = 0f;
                    verts[i * 16 +13] = 0f;
                    verts[i * 16 +14] = 0f;
                    verts[i * 16 +15] = 0f;
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
            rm.Vao = GL.GenVertexArray();
            rm.Vbo = GL.GenBuffer();
            rm.Ebo = GL.GenBuffer();
            rm.Color = sm.ColorFactor.ToVector4();

            GL.BindVertexArray(rm.Vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, rm.Vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, verts.Length * sizeof(float), verts, BufferUsageHint.StaticDraw);
            int stride = 16 * sizeof(float);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, stride, 8 * sizeof(float));
            GL.EnableVertexAttribArray(3);
            GL.VertexAttribPointer(4, 4, VertexAttribPointerType.Float, false, stride, 12 * sizeof(float));
            GL.EnableVertexAttribArray(4);
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

        _morphs.Clear();
        _morphValues.Clear();
        _morphVertexMap.Clear();
        foreach (var morph in data.Morphs)
        {
            if (morph.Type == MorphType.Vertex)
                _morphs[morph.Name] = morph;
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
        _morphDirty = true;
    }

    public void Render()
    {
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
        GL.Enable(EnableCap.Blend); // 半透明描画のためブレンドを有効化
        GL.FrontFace(FrontFaceDirection.Ccw);
        GL.ClearColor(1f, 1f, 1f, 1f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        Matrix4 rot = Matrix4.CreateFromQuaternion(_externalRotation) *
                      Matrix4.CreateRotationX(_orbitX) *
                      Matrix4.CreateRotationY(_orbitY);
        Vector3 cam = Vector3.TransformPosition(new Vector3(0, 0, _distance), rot) + _target;
        Matrix4 view = Matrix4.LookAt(cam, _target, Vector3.UnitY);
        float aspect = _width == 0 || _height == 0 ? 1f : _width / (float)_height;
        Matrix4 proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspect, 0.1f, 100f);
        var modelMat = ModelTransform;

        // CPU skinning: update vertex buffers based on current bone rotations
        if (_bones.Count > 0 && _morphDirty)
        {
            var worldMats = new System.Numerics.Matrix4x4[_bones.Count];
            var computed = new bool[_bones.Count];

            System.Numerics.Matrix4x4 ComputeWorld(int index)
            {
                if (computed[index])
                    return worldMats[index];

                var bone = _bones[index];
                System.Numerics.Vector3 euler = index < _boneRotations.Count ? _boneRotations[index].ToNumerics() : System.Numerics.Vector3.Zero;
                var delta = euler.FromEulerDegrees();
                System.Numerics.Vector3 trans = bone.Translation;
                if (index < _boneTranslations.Count)
                    trans += _boneTranslations[index].ToNumerics();
                var local = System.Numerics.Matrix4x4.CreateFromQuaternion(bone.Rotation * delta) * System.Numerics.Matrix4x4.CreateTranslation(trans);
                if (bone.Parent >= 0)
                    worldMats[index] = local * ComputeWorld(bone.Parent);
                else
                    worldMats[index] = local;

                computed[index] = true;
                return worldMats[index];
            }

            for (int i = 0; i < _bones.Count; i++)
                ComputeWorld(i);

            var skinMats = new System.Numerics.Matrix4x4[_bones.Count];
            for (int i = 0; i < _bones.Count; i++)
                skinMats[i] = _bones[i].InverseBindMatrix * worldMats[i];

            _boneArray = new float[_bones.Count * 16];
            for (int i = 0; i < _bones.Count; i++)
            {
                var m = skinMats[i];
                int offset = i * 16;
                _boneArray[offset + 0] = m.M11;
                _boneArray[offset + 1] = m.M12;
                _boneArray[offset + 2] = m.M13;
                _boneArray[offset + 3] = m.M14;
                _boneArray[offset + 4] = m.M21;
                _boneArray[offset + 5] = m.M22;
                _boneArray[offset + 6] = m.M23;
                _boneArray[offset + 7] = m.M24;
                _boneArray[offset + 8] = m.M31;
                _boneArray[offset + 9] = m.M32;
                _boneArray[offset +10] = m.M33;
                _boneArray[offset +11] = m.M34;
                _boneArray[offset +12] = m.M41;
                _boneArray[offset +13] = m.M42;
                _boneArray[offset +14] = m.M43;
                _boneArray[offset +15] = m.M44;
            }

            if (ShowBoneOutline && _boneLinePairs.Length > 0)
            {
                for (int i = 0; i < _boneLinePairs.Length / 2; i++)
                {
                    int parent = _boneLinePairs[i * 2];
                    int child = _boneLinePairs[i * 2 + 1];
                    var pp = worldMats[parent].Translation;
                    var cp = worldMats[child].Translation;
                    int offset = i * 6;
                    _boneLineVertices[offset + 0] = pp.X;
                    _boneLineVertices[offset + 1] = pp.Y;
                    _boneLineVertices[offset + 2] = pp.Z;
                    _boneLineVertices[offset + 3] = cp.X;
                    _boneLineVertices[offset + 4] = cp.Y;
                    _boneLineVertices[offset + 5] = cp.Z;
                }
                if (_boneVao == 0) _boneVao = GL.GenVertexArray();
                if (_boneVbo == 0)
                {
                    _boneVbo = GL.GenBuffer();
                    GL.BindVertexArray(_boneVao);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, _boneVbo);
                    GL.BufferData(BufferTarget.ArrayBuffer, _boneLineVertices.Length * sizeof(float), IntPtr.Zero, BufferUsageHint.DynamicDraw);
                    GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
                    GL.EnableVertexAttribArray(0);
                }
                else
                {
                    GL.BindVertexArray(_boneVao);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, _boneVbo);
                }
                var lineHandle = System.Runtime.InteropServices.GCHandle.Alloc(_boneLineVertices, System.Runtime.InteropServices.GCHandleType.Pinned);
                try
                {
                    GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, _boneLineVertices.Length * sizeof(float), lineHandle.AddrOfPinnedObject());
                }
                finally
                {
                    lineHandle.Free();
                }
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
                GL.BindVertexArray(0);
            }
            else
            {
                _boneVertexCount = 0;
            }

            _morphDirty = false;
        }

        GL.UseProgram(_modelProgram);
        GL.UniformMatrix4(_modelViewLoc, false, ref view);
        GL.UniformMatrix4(_modelProjLoc, false, ref proj);
        // ライトと視線方向をカメラ角度に合わせて更新
        Vector3 light = Vector3.Normalize(new Vector3(0.3f, 0.6f, -0.7f));
        light = Vector3.TransformNormal(light, rot);
        GL.Uniform3(_modelLightDirLoc, ref light);
        Vector3 viewDir = Vector3.Normalize(_target - cam);
        GL.Uniform3(_modelViewDirLoc, ref viewDir);
        GL.Uniform1(_modelShadeShiftLoc, ShadeShift);
        GL.Uniform1(_modelShadeToonyLoc, ShadeToony);
        GL.Uniform1(_modelRimIntensityLoc, RimIntensity);
        GL.Uniform1(_modelAmbientLoc, Ambient);
        if (_bones.Count > 0 && _boneArray.Length > 0)
            // _boneArray は行優先で格納されているため、転置フラグを有効にして送信する
            GL.UniformMatrix4(_modelBonesLoc, _bones.Count, true, _boneArray);
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
        GL.UniformMatrix4(_viewLoc, false, ref view);
        GL.UniformMatrix4(_projLoc, false, ref proj);

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
