using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
        public int Texture;
        public int IndexCount;
        public Vector4 Color = Vector4.One;
    }

    private readonly GameWindow _window;
    private readonly List<RenderMesh> _meshes = new();
    private readonly int _program;
    private readonly int _modelLoc;
    private readonly int _viewLoc;
    private readonly int _projLoc;
    private readonly int _texLoc;
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
                           "layout(location=1) in vec3 aNormal;\n" +
                           "layout(location=2) in vec2 aUV;\n" +
                           "out vec3 vNormal;\n" +
                           "out vec2 vUV;\n" +
                           "uniform mat4 uModel;\n" +
                           "uniform mat4 uView;\n" +
                           "uniform mat4 uProj;\n" +
                           "void main(){\n" +
                           "vNormal = mat3(uModel) * aNormal;\n" +
                           "vUV = aUV;\n" +
                           "gl_Position = uProj * uView * uModel * vec4(aPos,1.0);\n" +
                           "}";
        const string frag = "#version 330 core\n" +
                           "in vec3 vNormal;\n" +
                           "in vec2 vUV;\n" +
                           "out vec4 FragColor;\n" +
                           "uniform sampler2D uTex;\n" +
                           "uniform vec4 uColor;\n" +
                           "void main(){\n" +
                           "vec3 lightDir = normalize(vec3(0.3,0.6,0.7));\n" +
                           "float diff = max(dot(normalize(vNormal), lightDir), 0.2);\n" +
                           "vec4 col = texture(uTex, vUV) * uColor;\n" +
                           // テクスチャのアルファも利用する
                           "FragColor = vec4(col.rgb * diff, col.a);\n" +
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
        _modelLoc = GL.GetUniformLocation(_program, "uModel");
        _viewLoc = GL.GetUniformLocation(_program, "uView");
        _projLoc = GL.GetUniformLocation(_program, "uProj");
        _texLoc = GL.GetUniformLocation(_program, "uTex");
        _colorLoc = GL.GetUniformLocation(_program, "uColor");

        foreach (var sm in model.SubMeshes)
        {
            int vcount = sm.Positions.Length / 3;
            float[] verts = new float[vcount * 8];
            for (int i = 0; i < vcount; i++)
            {
                verts[i * 8 + 0] = sm.Positions[i * 3 + 0];
                verts[i * 8 + 1] = sm.Positions[i * 3 + 1];
                verts[i * 8 + 2] = sm.Positions[i * 3 + 2];
                verts[i * 8 + 3] = i * 3 < sm.Normals.Length ? sm.Normals[i * 3 + 0] : 0f;
                verts[i * 8 + 4] = i * 3 < sm.Normals.Length ? sm.Normals[i * 3 + 1] : 0f;
                verts[i * 8 + 5] = i * 3 < sm.Normals.Length ? sm.Normals[i * 3 + 2] : 1f;
                verts[i * 8 + 6] = i * 2 < sm.TexCoords.Length ? sm.TexCoords[i * 2 + 0] : 0f;
                verts[i * 8 + 7] = i * 2 < sm.TexCoords.Length ? sm.TexCoords[i * 2 + 1] : 0f;
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
            int stride = 8 * sizeof(float);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));
            GL.EnableVertexAttribArray(2);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, rm.Ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, sm.Indices.Length * sizeof(uint), sm.Indices, BufferUsageHint.StaticDraw);
            GL.BindVertexArray(0);

            rm.Texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, rm.Texture);
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
            if (sm.Texture != null)
            {
                var handle = GCHandle.Alloc(sm.Texture, GCHandleType.Pinned);
                try
                {
                    GL.TexImage2D((All)TextureTarget.Texture2D, 0,
                        (All)PixelInternalFormat.Rgba,
                        sm.TextureWidth, sm.TextureHeight, 0,
                        (All)PixelFormat.Rgba, (All)PixelType.UnsignedByte,
                        handle.AddrOfPinnedObject());
                }
                finally
                {
                    handle.Free();
                }
            }
            else
            {
                byte[] white = { 255, 255, 255, 255 };
                var handle = GCHandle.Alloc(white, GCHandleType.Pinned);
                try
                {
                    GL.TexImage2D((All)TextureTarget.Texture2D, 0,
                        (All)PixelInternalFormat.Rgba,
                        1, 1, 0,
                        (All)PixelFormat.Rgba, (All)PixelType.UnsignedByte,
                        handle.AddrOfPinnedObject());
                }
                finally
                {
                    handle.Free();
                }
            }
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 4);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.BindTexture(TextureTarget.Texture2D, 0);

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
        GL.UniformMatrix4(_modelLoc, false, ref model);
        GL.UniformMatrix4(_viewLoc, false, ref _view);
        GL.UniformMatrix4(_projLoc, false, ref proj);
        // メッシュ描画時も透過処理を行う
        GL.Enable(EnableCap.Blend);
        foreach (var rm in _meshes)
        {
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, rm.Texture);
            GL.Uniform1(_texLoc, 0);
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
            if (rm.Texture != 0) GL.DeleteTexture(rm.Texture);
        }
        _meshes.Clear();
        GL.DeleteProgram(_program);
        _window.Close();
        _window.Dispose();
    }
}
