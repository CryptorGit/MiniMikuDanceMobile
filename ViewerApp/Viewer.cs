using System;
using System.Diagnostics;
using System.Collections.Generic;
// Use OpenGL ES 3.0 across projects to avoid enum mismatches
using OpenTK.Graphics.ES30;
using GL = OpenTK.Graphics.ES30.GL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace ViewerApp;

public class Viewer : IDisposable
{
    private class RenderMesh
    {
        public int Vao;
        public int Vbo;
        public int Ebo;
        public int IndexCount;
        public Vector4 Color = Vector4.One;
    }

    private readonly GameWindow _window;
    private readonly List<RenderMesh> _meshes = new();
    private readonly int _program;
    private readonly int _mvpLoc;
    private readonly int _colorLoc;
    private readonly Matrix4 _modelTransform;
    private Matrix4 _view = Matrix4.Identity;
    private readonly Stopwatch _timer = new();

    public Vector2i Size { get; private set; } = new Vector2i(640, 480);

    public event Action<float>? FrameUpdated;

    public Viewer(string modelPath)
    {
        var nativeSettings = new NativeWindowSettings
        {
            Size = Size,
            StartVisible = false,
            StartFocused = false,
            Flags = ContextFlags.ForwardCompatible
        };
        _window = new GameWindow(GameWindowSettings.Default, nativeSettings);
        _window.MakeCurrent();
        GL.LoadBindings(new GLFWBindingsContext());
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

        var model = VrmLoader.Load(modelPath);
        _modelTransform = model.Transform;

        const string vert = "#version 330 core\n" +
                           "layout(location=0) in vec3 aPos;\n" +
                           "uniform mat4 uMVP;\n" +
                           "void main(){\n" +
                           "gl_Position = uMVP * vec4(aPos,1.0);\n" +
                           "}";
        const string frag = "#version 330 core\n" +
                           "out vec4 FragColor;\n" +
                           "uniform vec4 uColor;\n" +
                           "void main(){\n" +
                           "FragColor = uColor;\n" +
                           "}";
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
        _mvpLoc = GL.GetUniformLocation(_program, "uMVP");
        _colorLoc = GL.GetUniformLocation(_program, "uColor");

        foreach (var sm in model.SubMeshes)
        {
            int vcount = sm.Positions.Length / 3;
            float[] verts = new float[vcount * 3];
            for (int i = 0; i < vcount; i++)
            {
                verts[i * 3 + 0] = sm.Positions[i * 3 + 0];
                verts[i * 3 + 1] = sm.Positions[i * 3 + 1];
                verts[i * 3 + 2] = sm.Positions[i * 3 + 2];
            }

            var rm = new RenderMesh();
            rm.IndexCount = sm.Indices.Length;
            rm.Vao = GL.GenVertexArray();
            rm.Vbo = GL.GenBuffer();
            rm.Ebo = GL.GenBuffer();
            rm.Color = sm.ColorFactor;

            GL.BindVertexArray(rm.Vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, rm.Vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, verts.Length * sizeof(float), verts, BufferUsageHint.StaticDraw);
        int stride = 3 * sizeof(float);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, rm.Ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, sm.Indices.Length * sizeof(uint), sm.Indices, BufferUsageHint.StaticDraw);
            GL.BindVertexArray(0);

            // テクスチャ読み込みを省略

            _meshes.Add(rm);
        }

        // 深度バッファを正しく利用するための設定
        GL.ClearDepth(1.0f);
        GL.DepthFunc(DepthFunction.Lequal);
        GL.Enable(EnableCap.DepthTest);
        GL.DepthMask(true);

        GL.Enable(EnableCap.CullFace);
        GL.FrontFace(FrontFaceDirection.Ccw);
        Debug.WriteLine("[Viewer] Initialized");
        _timer.Start();
    }

    public void SetViewMatrix(Matrix4 view)
    {
        _view = view;
    }

    private void Render()
    {
        NativeWindow.ProcessWindowEvents(false);
        GL.Enable(EnableCap.DepthTest);
        GL.DepthMask(true);
        GL.Viewport(0, 0, Size.X, Size.Y);
        GL.ClearColor(1f, 1f, 1f, 1f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        Matrix4 model = _modelTransform;
        Matrix4 proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, Size.X / (float)Size.Y, 0.1f, 100f);

        GL.UseProgram(_program);
        Matrix4 mvp = proj * _view * model;
        GL.UniformMatrix4(_mvpLoc, false, ref mvp);
        // メッシュ描画時も透過処理を行う
        GL.Enable(EnableCap.Blend);
        foreach (var rm in _meshes)
        {
            GL.Uniform4(_colorLoc, rm.Color);
            GL.BindVertexArray(rm.Vao);
            GL.DrawElements(PrimitiveType.Triangles, rm.IndexCount, DrawElementsType.UnsignedInt, IntPtr.Zero);
            GL.BindVertexArray(0);
        }
        _window.SwapBuffers();
    }

    public byte[] CaptureFrame()
    {
        byte[] pixels = new byte[Size.X * Size.Y * 4];
        GL.ReadPixels(0, 0, Size.X, Size.Y, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
        return pixels;
    }

    public void Update(float deltaTime)
    {
        Render();
        FrameUpdated?.Invoke(deltaTime);
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
        GL.DeleteProgram(_program);
        _window.Close();
        _window.Dispose();
    }
}
