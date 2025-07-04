using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace ViewerApp;

public class Viewer : IDisposable
{
    private readonly GameWindow _window;
    private readonly int _vao;
    private readonly int _vbo;
    private readonly int _ebo;
    private readonly int _tex;
    private readonly int _program;
    private readonly int _modelLoc;
    private readonly int _viewLoc;
    private readonly int _projLoc;
    private readonly int _texLoc;
    private readonly int _indexCount;
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

        var model = VrmLoader.Load(modelPath);
        _indexCount = model.Indices.Length;
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
                           "void main(){\n" +
                           "vec3 lightDir = normalize(vec3(0.3,0.6,0.7));\n" +
                           "float diff = max(dot(normalize(vNormal), lightDir), 0.2);\n" +
                           "vec4 col = texture(uTex, vUV);\n" +
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

        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        _ebo = GL.GenBuffer();
        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        int vcount = model.Positions.Length / 3;
        float[] buffer = new float[vcount * 8];
        for (int i = 0; i < vcount; i++)
        {
            buffer[i * 8 + 0] = model.Positions[i * 3 + 0];
            buffer[i * 8 + 1] = model.Positions[i * 3 + 1];
            buffer[i * 8 + 2] = model.Positions[i * 3 + 2];
            buffer[i * 8 + 3] = i * 3 < model.Normals.Length ? model.Normals[i * 3 + 0] : 0f;
            buffer[i * 8 + 4] = i * 3 < model.Normals.Length ? model.Normals[i * 3 + 1] : 0f;
            buffer[i * 8 + 5] = i * 3 < model.Normals.Length ? model.Normals[i * 3 + 2] : 1f;
            buffer[i * 8 + 6] = i * 2 < model.TexCoords.Length ? model.TexCoords[i * 2 + 0] : 0f;
            buffer[i * 8 + 7] = i * 2 < model.TexCoords.Length ? model.TexCoords[i * 2 + 1] : 0f;
        }
        GL.BufferData(BufferTarget.ArrayBuffer, buffer.Length * sizeof(float), buffer, BufferUsageHint.StaticDraw);
        int stride = 8 * sizeof(float);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));
        GL.EnableVertexAttribArray(2);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, model.Indices.Length * sizeof(uint), model.Indices, BufferUsageHint.StaticDraw);
        GL.BindVertexArray(0);

        _tex = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _tex);
        if (model.Texture != null)
        {
            var handle = GCHandle.Alloc(model.Texture, GCHandleType.Pinned);
            try
            {
                GL.TexImage2D(TextureTarget.Texture2D, 0,
                    PixelInternalFormat.Rgba,
                    model.TextureWidth, model.TextureHeight, 0,
                    PixelFormat.Rgba, PixelType.UnsignedByte,
                    handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }
        else
        {
            // 代替テクスチャとして 1x1 の白色ピクセルを設定
            byte[] white = { 255, 255, 255, 255 };
            var handle = GCHandle.Alloc(white, GCHandleType.Pinned);
            try
            {
                GL.TexImage2D(TextureTarget.Texture2D, 0,
                    PixelInternalFormat.Rgba,
                    1, 1, 0,
                    PixelFormat.Rgba, PixelType.UnsignedByte,
                    handle.AddrOfPinnedObject());
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

        GL.Enable(EnableCap.DepthTest);
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
        GL.Viewport(0, 0, Size.X, Size.Y);
        GL.ClearColor(1f, 1f, 1f, 1f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        Matrix4 model = _modelTransform;
        Matrix4 proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, Size.X / (float)Size.Y, 0.1f, 100f);

        GL.UseProgram(_program);
        GL.UniformMatrix4(_modelLoc, false, ref model);
        GL.UniformMatrix4(_viewLoc, false, ref _view);
        GL.UniformMatrix4(_projLoc, false, ref proj);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _tex);
        GL.Uniform1(_texLoc, 0);
        GL.BindVertexArray(_vao);
        GL.DrawElements(PrimitiveType.Triangles, _indexCount, DrawElementsType.UnsignedInt, 0);
        GL.BindVertexArray(0);
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
        GL.DeleteBuffer(_vbo);
        GL.DeleteBuffer(_ebo);
        GL.DeleteVertexArray(_vao);
        GL.DeleteTexture(_tex);
        GL.DeleteProgram(_program);
        _window.Close();
        _window.Dispose();
    }
}
