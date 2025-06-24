using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using OpenTK.Graphics.ES30;

namespace MiniMikuDanceMaui;

public class SimpleCubeRenderer : IDisposable
{
    private int _program;
    private int _vbo;
    private int _vao;
    private int _modelVao;
    private int _modelVbo;
    private int _modelEbo;
    private int _modelIndexCount;
    private Vector3 _modelOffset = Vector3.Zero;
    private int _gridVao;
    private int _gridVbo;
    private int _modelLoc;
    private int _viewLoc;
    private int _projLoc;
    private int _colorLoc;
    private float _angle;
    private float _orbitX;
    private float _orbitY;
    private float _distance = 4f;
    private Vector3 _target = Vector3.Zero;
    private int _groundVao;
    private int _groundVbo;
    private int _width;
    private int _height;

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

        float[] verts = {
            -0.5f,-0.5f,-0.5f,  0.5f,-0.5f,-0.5f,  0.5f, 0.5f,-0.5f,
            -0.5f,-0.5f,-0.5f,  0.5f, 0.5f,-0.5f, -0.5f, 0.5f,-0.5f,
            -0.5f,-0.5f, 0.5f,  0.5f,-0.5f, 0.5f,  0.5f, 0.5f, 0.5f,
            -0.5f,-0.5f, 0.5f,  0.5f, 0.5f, 0.5f, -0.5f, 0.5f, 0.5f,
            -0.5f,-0.5f,-0.5f, -0.5f, 0.5f,-0.5f, -0.5f, 0.5f, 0.5f,
            -0.5f,-0.5f,-0.5f, -0.5f, 0.5f, 0.5f, -0.5f,-0.5f, 0.5f,
             0.5f,-0.5f,-0.5f,  0.5f, 0.5f,-0.5f,  0.5f, 0.5f, 0.5f,
             0.5f,-0.5f,-0.5f,  0.5f, 0.5f, 0.5f,  0.5f,-0.5f, 0.5f,
            -0.5f,-0.5f,-0.5f, -0.5f,-0.5f, 0.5f,  0.5f,-0.5f, 0.5f,
            -0.5f,-0.5f,-0.5f,  0.5f,-0.5f, 0.5f,  0.5f,-0.5f,-0.5f,
            -0.5f, 0.5f,-0.5f, -0.5f, 0.5f, 0.5f,  0.5f, 0.5f, 0.5f,
            -0.5f, 0.5f,-0.5f,  0.5f, 0.5f, 0.5f,  0.5f, 0.5f,-0.5f
        };

        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, verts.Length * sizeof(float), verts, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);

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

    public void LoadModel(ModelData data)
    {
        if (_modelVao != 0)
        {
            GL.DeleteVertexArray(_modelVao);
            GL.DeleteBuffer(_modelVbo);
            GL.DeleteBuffer(_modelEbo);
            _modelVao = _modelVbo = _modelEbo = 0;
        }

        var verts = data.Mesh.Vertices;
        var indices = new List<uint>();
        foreach (var f in data.Mesh.Faces)
        {
            indices.Add((uint)f.Indices[0]);
            indices.Add((uint)f.Indices[1]);
            indices.Add((uint)f.Indices[2]);
        }
        _modelIndexCount = indices.Count;

        float minY = float.MaxValue;
        foreach (var v in verts)
        {
            if (v.Y < minY) minY = v.Y;
        }
        _modelOffset = minY < 0f ? new Vector3(0f, -minY, 0f) : Vector3.Zero;

        float[] vbuf = new float[verts.Count * 3];
        for (int i = 0; i < verts.Count; i++)
        {
            vbuf[i * 3 + 0] = verts[i].X;
            vbuf[i * 3 + 1] = verts[i].Y;
            vbuf[i * 3 + 2] = verts[i].Z;
        }

        _modelVao = GL.GenVertexArray();
        _modelVbo = GL.GenBuffer();
        _modelEbo = GL.GenBuffer();
        GL.BindVertexArray(_modelVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _modelVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vbuf.Length * sizeof(float), vbuf, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _modelEbo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Count * sizeof(uint), indices.ToArray(), BufferUsageHint.StaticDraw);
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
        _orbitY += dx * 0.01f;
        _orbitX += dy * 0.01f;
    }

    public void Pan(float dx, float dy)
    {
        Vector3 right = Vector3.UnitX;
        Vector3 up = Vector3.UnitY;
        _target += (-right * dx + up * dy) * 0.01f;
    }

    public void Dolly(float delta)
    {
        _distance *= 1f - delta * 0.01f;
        if (_distance < 1f) _distance = 1f;
        if (_distance > 20f) _distance = 20f;
    }

    public void ResetCamera()
    {
        _orbitX = 0f;
        _orbitY = 0f; // 正面向き
        _distance = 4f;
        _target = Vector3.Zero;
    }

    public void Render()
    {
        GL.Enable(EnableCap.DepthTest);
        GL.ClearColor(1f, 1f, 1f, 1f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        Matrix4 model = Matrix4.CreateTranslation(_modelOffset);
        Matrix4 rot = Matrix4.CreateRotationX(_orbitX) * Matrix4.CreateRotationY(_orbitY);
        Vector3 cam = Vector3.TransformPosition(new Vector3(0, 0, _distance), rot) + _target;
        Matrix4 view = Matrix4.LookAt(cam, _target, Vector3.UnitY);
        float aspect = _width == 0 || _height == 0 ? 1f : _width / (float)_height;
        Matrix4 proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspect, 0.1f, 100f);

        GL.UseProgram(_program);
        GL.UniformMatrix4(_viewLoc, false, ref view);
        GL.UniformMatrix4(_projLoc, false, ref proj);
        // draw ground
        Matrix4 gridModel = Matrix4.Identity;
        GL.UniformMatrix4(_modelLoc, false, ref gridModel);
        GL.Uniform4(_colorLoc, new Vector4(1f, 1f, 1f, 1f));
        GL.BindVertexArray(_groundVao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        GL.BindVertexArray(0);

        // draw cube or loaded model
        GL.UniformMatrix4(_modelLoc, false, ref model);
        GL.Uniform4(_colorLoc, new Vector4(0.3f, 0.7f, 1.0f, 1.0f));
        if (_modelVao != 0)
        {
            GL.BindVertexArray(_modelVao);
            GL.DrawElements(PrimitiveType.Triangles, _modelIndexCount, DrawElementsType.UnsignedInt, 0);
        }
        else
        {
            GL.BindVertexArray(_vao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
        }
        GL.BindVertexArray(0);

        // draw grid
        GL.UniformMatrix4(_modelLoc, false, ref gridModel);
        GL.Uniform4(_colorLoc, new Vector4(0.8f, 0.8f, 0.8f, 1.0f));
        GL.BindVertexArray(_gridVao);
        GL.DrawArrays(PrimitiveType.Lines, 0, ((10 - (-10) + 1) * 2) * 2);
        GL.BindVertexArray(0);
    }

    public void Dispose()
    {
        GL.DeleteBuffer(_vbo);
        if (_modelVbo != 0) GL.DeleteBuffer(_modelVbo);
        if (_modelEbo != 0) GL.DeleteBuffer(_modelEbo);
        GL.DeleteBuffer(_gridVbo);
        GL.DeleteBuffer(_groundVbo);
        GL.DeleteVertexArray(_vao);
        if (_modelVao != 0) GL.DeleteVertexArray(_modelVao);
        GL.DeleteVertexArray(_gridVao);
        GL.DeleteVertexArray(_groundVao);
        GL.DeleteProgram(_program);
    }
}
