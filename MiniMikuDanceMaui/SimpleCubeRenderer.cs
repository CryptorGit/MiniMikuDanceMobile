using System;
using OpenTK.Mathematics;
using OpenTK.Graphics.ES30;
using System.Runtime.InteropServices;
using MiniMikuDance.Util;

namespace MiniMikuDanceMaui;

public class SimpleCubeRenderer : IDisposable
{
    private int _program;
    private int _modelVbo;
    private int _modelVao;
    private int _modelEbo;
    private int _modelIndexCount;
    private int _gridVao;
    private int _gridVbo;
    private int _modelLoc;
    private int _viewLoc;
    private int _projLoc;
    private int _colorLoc;
    private float _orbitX;
    private float _orbitY = MathHelper.PiOver4;
    private float _distance = 4f;
    private Vector3 _target = Vector3.Zero;
    private int _groundVao;
    private int _groundVbo;
    private int _modelProgram;
    private int _modelModelLoc;
    private int _modelViewLoc;
    private int _modelProjLoc;
    private int _texLoc;
    private int _tex;
    private Matrix4 _modelTransform = Matrix4.Identity;
    private int _width;
    private int _height;
    public float RotateSensitivity { get; set; } = 1f;
    public float PanSensitivity { get; set; } = 1f;
    public bool CameraLocked { get; set; }

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

        const string modelVert = @"#version 300 es
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aUV;
uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProj;
out vec3 vNormal;
out vec2 vUV;
void main(){
    vNormal = mat3(uModel) * aNormal;
    vUV = aUV;
    gl_Position = uProj * uView * uModel * vec4(aPosition,1.0);
}";
        const string modelFrag = @"#version 300 es
precision mediump float;
in vec3 vNormal;
in vec2 vUV;
uniform sampler2D uTex;
out vec4 FragColor;
void main(){
    vec3 lightDir = normalize(vec3(0.3,0.6,0.7));
    float diff = max(dot(normalize(vNormal), lightDir), 0.2);
    vec4 col = texture(uTex, vUV);
    FragColor = vec4(col.rgb * diff, col.a);
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
        _modelModelLoc = GL.GetUniformLocation(_modelProgram, "uModel");
        _modelViewLoc = GL.GetUniformLocation(_modelProgram, "uView");
        _modelProjLoc = GL.GetUniformLocation(_modelProgram, "uProj");
        _texLoc = GL.GetUniformLocation(_modelProgram, "uTex");



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
        _orbitY = MathHelper.PiOver4;
        _distance = 4f;
        _target = Vector3.Zero;
    }

    public void LoadModel(MiniMikuDance.Import.ModelData data)
    {
        if (_modelVao != 0)
        {
            GL.DeleteVertexArray(_modelVao);
            GL.DeleteBuffer(_modelVbo);
            GL.DeleteBuffer(_modelEbo);
            if (_tex != 0)
            {
                GL.DeleteTexture(_tex);
                _tex = 0;
            }
        }

        _modelTransform = data.Transform.ToMatrix4();

        int vcount = data.Mesh.VertexCount;
        float[] verts = new float[vcount * 8];
        for (int i = 0; i < vcount; i++)
        {
            var v = data.Mesh.Vertices[i];
            verts[i * 8 + 0] = v.X;
            verts[i * 8 + 1] = v.Y;
            verts[i * 8 + 2] = v.Z;
            if (i < data.Mesh.Normals.Count)
            {
                var n = data.Mesh.Normals[i];
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
            if (data.Mesh.TextureCoordinateChannelCount > 0 && i < data.Mesh.TextureCoordinateChannels[0].Count)
            {
                var uv = data.Mesh.TextureCoordinateChannels[0][i];
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
        foreach (var f in data.Mesh.Faces)
        {
            foreach (var idx in f.Indices)
                indices.Add((uint)idx);
        }
        _modelIndexCount = indices.Count;

        _modelVao = GL.GenVertexArray();
        _modelVbo = GL.GenBuffer();
        _modelEbo = GL.GenBuffer();
        GL.BindVertexArray(_modelVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _modelVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, verts.Length * sizeof(float), verts, BufferUsageHint.StaticDraw);
        int stride = 8 * sizeof(float);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));
        GL.EnableVertexAttribArray(2);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _modelEbo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Count * sizeof(uint), indices.ToArray(), BufferUsageHint.StaticDraw);
        GL.BindVertexArray(0);

        _tex = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _tex);
        if (data.TextureData != null)
        {
            // GL.TexImage2D の強く型付けされたオーバーロードを利用するため
            // テクスチャデータを固定してポインタを取得する
            var handle = GCHandle.Alloc(data.TextureData, GCHandleType.Pinned);
            try
            {
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                    data.TextureWidth, data.TextureHeight, 0,
                    PixelFormat.Rgba, PixelType.UnsignedByte, handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }
        else
        {
            // モデルにテクスチャが無い場合は 1x1 の白テクスチャを生成
            byte[] white = { 255, 255, 255, 255 };
            var handle = GCHandle.Alloc(white, GCHandleType.Pinned);
            try
            {
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                    1, 1, 0, PixelFormat.Rgba, PixelType.UnsignedByte, handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    public void Render()
    {
        GL.Enable(EnableCap.DepthTest);
        GL.ClearColor(1f, 1f, 1f, 1f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        Matrix4 model = _modelTransform;
        Matrix4 rot = Matrix4.CreateRotationX(_orbitX) * Matrix4.CreateRotationY(_orbitY);
        Vector3 cam = Vector3.TransformPosition(new Vector3(0, 0, _distance), rot) + _target;
        Matrix4 view = Matrix4.LookAt(cam, _target, Vector3.UnitY);
        float aspect = _width == 0 || _height == 0 ? 1f : _width / (float)_height;
        Matrix4 proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspect, 0.1f, 100f);

        GL.UseProgram(_modelProgram);
        GL.UniformMatrix4(_modelViewLoc, false, ref view);
        GL.UniformMatrix4(_modelProjLoc, false, ref proj);
        if (_modelIndexCount > 0)
        {
            GL.UniformMatrix4(_modelModelLoc, false, ref model);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _tex);
            GL.Uniform1(_texLoc, 0);
            GL.BindVertexArray(_modelVao);
            GL.DrawElements(PrimitiveType.Triangles, _modelIndexCount, DrawElementsType.UnsignedInt, IntPtr.Zero);
            GL.BindVertexArray(0);
        }

        GL.UseProgram(_program);
        GL.UniformMatrix4(_viewLoc, false, ref view);
        GL.UniformMatrix4(_projLoc, false, ref proj);

        Matrix4 gridModel = Matrix4.Identity;
        GL.UniformMatrix4(_modelLoc, false, ref gridModel);
        GL.Uniform4(_colorLoc, new Vector4(1f, 1f, 1f, 1f));
        GL.BindVertexArray(_groundVao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        GL.BindVertexArray(0);

        GL.UniformMatrix4(_modelLoc, false, ref gridModel);
        GL.Uniform4(_colorLoc, new Vector4(0.8f, 0.8f, 0.8f, 1.0f));
        GL.BindVertexArray(_gridVao);
        GL.DrawArrays(PrimitiveType.Lines, 0, ((10 - (-10) + 1) * 2) * 2);
        GL.BindVertexArray(0);
    }

    public void Dispose()
    {
        if (_modelVbo != 0) GL.DeleteBuffer(_modelVbo);
        if (_modelEbo != 0) GL.DeleteBuffer(_modelEbo);
        GL.DeleteBuffer(_gridVbo);
        GL.DeleteBuffer(_groundVbo);
        if (_modelVao != 0) GL.DeleteVertexArray(_modelVao);
        GL.DeleteVertexArray(_gridVao);
        GL.DeleteVertexArray(_groundVao);
        if (_tex != 0) GL.DeleteTexture(_tex);
        GL.DeleteProgram(_program);
        GL.DeleteProgram(_modelProgram);
   }
}
