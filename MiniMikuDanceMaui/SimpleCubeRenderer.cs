using System;
using OpenTK.Mathematics;
using OpenTK.Graphics.ES30;
using GL = OpenTK.Graphics.ES30.GL;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using MiniMikuDance.Util;

namespace MiniMikuDanceMaui;

public class SimpleCubeRenderer : IDisposable
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
        public Vector3[] Normals = Array.Empty<Vector3>();
        public Vector4[] Joints = Array.Empty<Vector4>();
        public Vector4[] Weights = Array.Empty<Vector4>();
    }
    private readonly System.Collections.Generic.List<RenderMesh> _meshes = new();
    private int _gridVao;
    private int _gridVbo;
    private int _modelLoc;
    private int _viewLoc;
    private int _projLoc;
    private int _colorLoc;
    private float _orbitX;
    // 初期カメラ位置はモデル正面を向くようY軸回転を0に設定
    private float _orbitY = 0f;
    private float _distance = 4f;
    private Vector3 _target = Vector3.Zero;
    private int _groundVao;
    private int _groundVbo;
    private int _modelProgram;
    private int _modelViewLoc;
    private int _modelProjLoc;
    private int _modelColorLoc;
    private int _modelTexLoc;
    private int _modelUseTexLoc;
    private int _modelLightDirLoc;
    private int _modelViewDirLoc;
    private int _modelShadeShiftLoc;
    private int _modelShadeToonyLoc;
    private int _modelRimIntensityLoc;
    private Matrix4 _modelTransform = Matrix4.Identity;
    private readonly Dictionary<int, Vector3> _boneRotations = new();
    private readonly List<MiniMikuDance.Import.BoneData> _bones = new();
    private Matrix4[] _boneMatrices = Array.Empty<Matrix4>();
    private Matrix4[] _boneWorldMatrices = Array.Empty<Matrix4>();
    private int _modelBonesLoc;
    private int _width;
    private int _height;
    public float RotateSensitivity { get; set; } = 1f;
    public float PanSensitivity { get; set; } = 1f;
    public bool CameraLocked { get; set; }
    public float ShadeShift { get; set; } = -0.1f;
    public float ShadeToony { get; set; } = 0.9f;
    public float RimIntensity { get; set; } = 0.5f;
    public void SetBoneRotation(int index, Vector3 degrees)
        => _boneRotations[index] = degrees;

    public void ClearBoneRotations()
        => _boneRotations.Clear();

    private void UpdateBoneMatrices()
    {
        if (_bones.Count == 0)
            return;
        if (_boneMatrices.Length != _bones.Count)
        {
            _boneMatrices = new Matrix4[_bones.Count];
            _boneWorldMatrices = new Matrix4[_bones.Count];
        }

        for (int i = 0; i < _bones.Count; i++)
        {
            var bone = _bones[i];
            var rot = bone.Rotation;
            if (_boneRotations.TryGetValue(i, out var euler))
            {
                var add = new System.Numerics.Vector3(euler.X, euler.Y, euler.Z);
                var qadd = System.Numerics.Quaternion.CreateFromYawPitchRoll(
                    MathHelper.DegreesToRadians(add.Y),
                    MathHelper.DegreesToRadians(add.X),
                    MathHelper.DegreesToRadians(add.Z));
                rot = System.Numerics.Quaternion.Normalize(qadd * rot);
            }

            Matrix4 local = rot.ToMatrix4() * Matrix4.CreateTranslation(bone.Translation.ToOpenTK());
            if (bone.Parent >= 0)
                _boneWorldMatrices[i] = _boneWorldMatrices[bone.Parent] * local;
            else
                _boneWorldMatrices[i] = _modelTransform * local;

            Matrix4 invBind = bone.InverseBindMatrix.ToMatrix4();
            _boneMatrices[i] = _boneWorldMatrices[i] * invBind;
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
#define MAX_BONES 64
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aTex;
layout(location = 3) in vec4 aJoints;
layout(location = 4) in vec4 aWeights;
uniform mat4 uView;
uniform mat4 uProj;
uniform mat4 uBones[MAX_BONES];
out vec3 vNormal;
out vec2 vTex;
void main(){
    float wsum = aWeights.x + aWeights.y + aWeights.z + aWeights.w;
    mat4 skin = aWeights.x * uBones[int(aJoints.x)] +
                aWeights.y * uBones[int(aJoints.y)] +
                aWeights.z * uBones[int(aJoints.z)] +
                aWeights.w * uBones[int(aJoints.w)];
    if (wsum == 0.0) skin = uBones[0];
    vec4 pos = skin * vec4(aPosition,1.0);
    vNormal = mat3(skin) * aNormal;
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
out vec4 FragColor;
void main(){
    vec4 base = uUseTex ? texture(uTex, vTex) : uColor;
    float ndotl = max(dot(normalize(vNormal), normalize(uLightDir)), 0.0);
    float light = clamp((ndotl + uShadeShift) * uShadeToony, 0.0, 1.0);
    float rim = pow(1.0 - max(dot(normalize(vNormal), normalize(uViewDir)), 0.0), 3.0) * uRimIntensity;
    vec3 color = base.rgb * light + base.rgb * rim;
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
        _modelBonesLoc = GL.GetUniformLocation(_modelProgram, "uBones[0]");
        _modelColorLoc = GL.GetUniformLocation(_modelProgram, "uColor");
        _modelTexLoc = GL.GetUniformLocation(_modelProgram, "uTex");
        _modelUseTexLoc = GL.GetUniformLocation(_modelProgram, "uUseTex");
        _modelLightDirLoc = GL.GetUniformLocation(_modelProgram, "uLightDir");
        _modelViewDirLoc = GL.GetUniformLocation(_modelProgram, "uViewDir");
        _modelShadeShiftLoc = GL.GetUniformLocation(_modelProgram, "uShadeShift");
        _modelShadeToonyLoc = GL.GetUniformLocation(_modelProgram, "uShadeToony");
        _modelRimIntensityLoc = GL.GetUniformLocation(_modelProgram, "uRimIntensity");

        // grid vertices (XZ plane)
        int gridLines = (10 - (-10) + 1) * 2; // 21 lines along each axis
        float[] grid = new float[gridLines * 2 * 3];
        int idx = 0;
        for (int i = -10; i <= 10; i++)
        {
            grid[idx++] = i; grid[idx++] = 0; grid[idx++] = -10;
            grid[idx++] = i; grid[idx++] = 0; grid[idx++] = 10;
            grid[idx++] = -10; grid[idx++] = 0; grid[idx++] = i;
            grid[idx++] = 10;  grid[idx++] = 0; grid[idx++] = i;
        }
        _gridVao = GL.GenVertexArray();
        _gridVbo = GL.GenBuffer();
        GL.BindVertexArray(_gridVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _gridVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, grid.Length * sizeof(float), grid, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);

        // ground plane
        float[] plane = {
            -10f, 0f, -10f,
             10f, 0f, -10f,
            -10f, 0f,  10f,
             10f, 0f, -10f,
             10f, 0f,  10f,
            -10f, 0f,  10f
        };
        _groundVao = GL.GenVertexArray();
        _groundVbo = GL.GenBuffer();
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
        if (CameraLocked) return;
        _orbitY -= dx * 0.01f * RotateSensitivity;
        _orbitX -= dy * 0.01f * RotateSensitivity;
    }

    public void Pan(float dx, float dy)
    {
        if (CameraLocked) return;
        Matrix4 rot = Matrix4.CreateRotationX(_orbitX) * Matrix4.CreateRotationY(_orbitY);
        Vector3 right = Vector3.TransformNormal(Vector3.UnitX, rot);
        Vector3 up = Vector3.TransformNormal(Vector3.UnitY, rot);
        _target += (-right * dx + up * dy) * 0.01f * PanSensitivity;
    }

    public void Dolly(float delta)
    {
        if (CameraLocked) return;
        _distance *= 1f + delta * 0.01f * PanSensitivity;
        if (_distance < 1f) _distance = 1f;
        if (_distance > 20f) _distance = 20f;
    }

    public void ResetCamera()
    {
        _orbitX = 0f;
        // モデル読み込み時は正面から表示する
        _orbitY = 0f;
        _distance = 4f;
        _target = Vector3.Zero;
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

        _modelTransform = data.Transform.ToMatrix4();
        _bones.Clear();
        _bones.AddRange(data.Bones);
        _boneRotations.Clear();
        _boneMatrices = new Matrix4[_bones.Count];
        _boneWorldMatrices = new Matrix4[_bones.Count];

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
            var jointArr = sm.Joints;
            var weightArr = sm.Weights;
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
                if (jointArr.Count > i)
                {
                    var j = jointArr[i];
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
                if (weightArr.Count > i)
                {
                    var w = weightArr[i];
                    verts[i * 16 +12] = w.X;
                    verts[i * 16 +13] = w.Y;
                    verts[i * 16 +14] = w.Z;
                    verts[i * 16 +15] = w.W;
                    if (w.X + w.Y + w.Z + w.W == 0f)
                    {
                        verts[i * 16 +12] = 1f;
                    }
                }
                else
                {
                    verts[i * 16 +12] = 1f;
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
            rm.Normals = sm.Mesh.Normals.Select(n => new Vector3(n.X, n.Y, n.Z)).ToArray();
            rm.Joints = jointArr.ToArray();
            rm.Weights = weightArr.ToArray();
            rm.Vao = GL.GenVertexArray();
            rm.Vbo = GL.GenBuffer();
            rm.Ebo = GL.GenBuffer();
            var cf = sm.ColorFactor.ToVector4();
            cf.W = 1.0f; // 不透明にする
            rm.Color = cf;

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
    }

    public void Render()
    {
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
        GL.FrontFace(FrontFaceDirection.Ccw);
        GL.ClearColor(1f, 1f, 1f, 1f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        UpdateBoneMatrices();
        Matrix4 rot = Matrix4.CreateRotationX(_orbitX) * Matrix4.CreateRotationY(_orbitY);
        Vector3 cam = Vector3.TransformPosition(new Vector3(0, 0, _distance), rot) + _target;
        Matrix4 view = Matrix4.LookAt(cam, _target, Vector3.UnitY);
        float aspect = _width == 0 || _height == 0 ? 1f : _width / (float)_height;
        Matrix4 proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspect, 0.1f, 100f);

        GL.UseProgram(_modelProgram);
        GL.UniformMatrix4(_modelViewLoc, false, ref view);
        GL.UniformMatrix4(_modelProjLoc, false, ref proj);
        Vector3 light = Vector3.Normalize(new Vector3(0.3f, 0.6f, 0.7f));
        GL.Uniform3(_modelLightDirLoc, ref light);
        Vector3 viewDir = Vector3.UnitZ;
        GL.Uniform3(_modelViewDirLoc, ref viewDir);
        GL.Uniform1(_modelShadeShiftLoc, ShadeShift);
        GL.Uniform1(_modelShadeToonyLoc, ShadeToony);
        GL.Uniform1(_modelRimIntensityLoc, RimIntensity);
        // テクスチャのアルファを利用するためブレンドを有効化
        GL.Enable(EnableCap.Blend);
        for (int i = 0; i < _bones.Count && i < 64; i++)
        {
            GL.UniformMatrix4(_modelBonesLoc + i, false, ref _boneMatrices[i]);
        }
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
        // グリッド描画では透過を利用するためブレンドを再度有効化
        GL.Enable(EnableCap.Blend);

        GL.UseProgram(_program);
        GL.UniformMatrix4(_viewLoc, false, ref view);
        GL.UniformMatrix4(_projLoc, false, ref proj);

        Matrix4 gridModel = Matrix4.Identity;
        GL.DepthMask(false);
        GL.UniformMatrix4(_modelLoc, false, ref gridModel);
        GL.Uniform4(_colorLoc, new Vector4(1f, 1f, 1f, 0.3f));
        GL.BindVertexArray(_groundVao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        GL.BindVertexArray(0);

        GL.UniformMatrix4(_modelLoc, false, ref gridModel);
        GL.Uniform4(_colorLoc, new Vector4(0.8f, 0.8f, 0.8f, 0.5f));
        GL.BindVertexArray(_gridVao);
        GL.DrawArrays(PrimitiveType.Lines, 0, ((10 - (-10) + 1) * 2) * 2);
        GL.BindVertexArray(0);
        GL.DepthMask(true);
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
        GL.DeleteBuffer(_gridVbo);
        GL.DeleteBuffer(_groundVbo);
        GL.DeleteVertexArray(_gridVao);
        GL.DeleteVertexArray(_groundVao);
        GL.DeleteProgram(_program);
        GL.DeleteProgram(_modelProgram);
   }
}
