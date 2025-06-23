using System;
using OpenTK.Mathematics;
using OpenTK.Graphics.ES30;

namespace MiniMikuDanceMaui;

public class SimpleCubeRenderer : IDisposable
{
    private int _program;
    private int _vbo;
    private int _vao;
    private int _modelLoc;
    private int _viewLoc;
    private int _projLoc;
    private float _angle;
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
out vec4 FragColor;
void main(){
    FragColor = vec4(0.3,0.7,1.0,1.0);
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
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
        GL.Viewport(0, 0, width, height);
    }

    public void Render()
    {
        GL.Enable(EnableCap.DepthTest);
        GL.ClearColor(0f, 0f, 0f, 1f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _angle += 0.02f;
        Matrix4 model = Matrix4.CreateRotationY(_angle) * Matrix4.CreateRotationX(_angle * 0.5f);
        Matrix4 view = Matrix4.LookAt(new Vector3(2, 2, 2), Vector3.Zero, Vector3.UnitY);
        float aspect = _width == 0 || _height == 0 ? 1f : _width / (float)_height;
        Matrix4 proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspect, 0.1f, 100f);

        GL.UseProgram(_program);
        GL.UniformMatrix4(_modelLoc, false, ref model);
        GL.UniformMatrix4(_viewLoc, false, ref view);
        GL.UniformMatrix4(_projLoc, false, ref proj);
        GL.BindVertexArray(_vao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
        GL.BindVertexArray(0);
    }

    public void Dispose()
    {
        GL.DeleteBuffer(_vbo);
        GL.DeleteVertexArray(_vao);
        GL.DeleteProgram(_program);
    }
}
