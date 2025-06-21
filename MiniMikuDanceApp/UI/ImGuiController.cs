using System;
using System.Numerics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;
using ImGuiNET;

namespace MiniMikuDanceApp.UI;

public class ImGuiController : IDisposable
{
    private int _vertexArray;
    private int _vertexBuffer;
    private int _indexBuffer;
    private int _vertexShader;
    private int _fragmentShader;
    private int _shaderProgram;
    private int _textureLocation;
    private int _projLocation;
    private int _fontTexture;

    private int _width;
    private int _height;

    public ImGuiController(int width, int height)
    {
        _width = width;
        _height = height;
        ImGui.CreateContext();
        ImGui.StyleColorsDark();
        ImGui.GetIO().Fonts.AddFontDefault();
        CreateDeviceResources();
    }

    public void WindowResized(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public void Update(GameWindow window, float delta)
    {
        var io = ImGui.GetIO();
        io.DisplaySize = new Vector2(_width, _height);
        io.DeltaTime = delta > 0 ? delta : 1f / 60f;
        UpdateInput(window);
        ImGui.NewFrame();
    }

    private void UpdateInput(GameWindow window)
    {
        var io = ImGui.GetIO();
        var mouse = window.MouseState;
        io.MousePos = new Vector2(mouse.X, mouse.Y);
        io.MouseDown[0] = mouse.IsButtonDown(MouseButton.Left);
        io.MouseDown[1] = mouse.IsButtonDown(MouseButton.Right);
        io.MouseDown[2] = mouse.IsButtonDown(MouseButton.Middle);
        var keyboard = window.KeyboardState;
        for (int i = 0; i < io.KeysDown.Count; i++)
        {
            io.KeysDown[i] = keyboard.IsKeyDown((Keys)i);
        }
    }

    public void Render()
    {
        ImGui.Render();
        RenderImDrawData(ImGui.GetDrawData());
    }

    private unsafe void CreateDeviceResources()
    {
        string vertexSource = "#version 330 core\n" +
            "layout(location = 0) in vec2 in_position;\n" +
            "layout(location = 1) in vec2 in_texCoord;\n" +
            "layout(location = 2) in vec4 in_color;\n" +
            "uniform mat4 projection;\n" +
            "out vec2 frag_UV;\n" +
            "out vec4 frag_Color;\n" +
            "void main()\n" +
            "{\n" +
            "    frag_UV = in_texCoord;\n" +
            "    frag_Color = in_color;\n" +
            "    gl_Position = projection * vec4(in_position, 0, 1);\n" +
            "}";
        string fragmentSource = "#version 330 core\n" +
            "in vec2 frag_UV;\n" +
            "in vec4 frag_Color;\n" +
            "uniform sampler2D in_texture;\n" +
            "out vec4 out_Color;\n" +
            "void main()\n" +
            "{\n" +
            "    out_Color = frag_Color * texture(in_texture, frag_UV);\n" +
            "}";
        _vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(_vertexShader, vertexSource);
        GL.CompileShader(_vertexShader);
        _fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(_fragmentShader, fragmentSource);
        GL.CompileShader(_fragmentShader);
        _shaderProgram = GL.CreateProgram();
        GL.AttachShader(_shaderProgram, _vertexShader);
        GL.AttachShader(_shaderProgram, _fragmentShader);
        GL.LinkProgram(_shaderProgram);

        _textureLocation = GL.GetUniformLocation(_shaderProgram, "in_texture");
        _projLocation = GL.GetUniformLocation(_shaderProgram, "projection");

        _vertexArray = GL.GenVertexArray();
        _vertexBuffer = GL.GenBuffer();
        _indexBuffer = GL.GenBuffer();

        GL.BindVertexArray(_vertexArray);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
        GL.EnableVertexAttribArray(0);
        GL.EnableVertexAttribArray(1);
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 20, 0);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 20, 8);
        GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, 20, 16);
        GL.BindVertexArray(0);

        CreateFontTexture();
    }

    private unsafe void CreateFontTexture()
    {
        var io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out int width, out int height, out int _);
        _fontTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _fontTexture);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, (IntPtr)pixels);
        io.Fonts.SetTexID((IntPtr)_fontTexture);
        io.Fonts.ClearTexData();
    }

    private unsafe void RenderImDrawData(ImDrawDataPtr drawData)
    {
        var io = ImGui.GetIO();
        drawData.ScaleClipRects(io.DisplayFramebufferScale);

        GL.Enable(EnableCap.Blend);
        GL.BlendEquation(BlendEquationMode.FuncAdd);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.DepthTest);

        GL.UseProgram(_shaderProgram);
        GL.Uniform1(_textureLocation, 0);

        float l = 0.0f;
        float r = io.DisplaySize.X;
        float t = 0.0f;
        float b = io.DisplaySize.Y;
        var mvp = new Matrix4x4(
            2.0f / (r - l), 0, 0, 0,
            0, 2.0f / (t - b), 0, 0,
            0, 0, -1, 0,
            (r + l) / (l - r), (t + b) / (b - t), 0, 1);
        GL.UniformMatrix4fv(_projLocation, 1, false, (float*)&mvp);

        GL.BindVertexArray(_vertexArray);
        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            var cmd = drawData.CmdListsRange[n];
            int vtxSize = cmd.VtxBuffer.Size * sizeof(ImDrawVert);
            int idxSize = cmd.IdxBuffer.Size * sizeof(ushort);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer, vtxSize, IntPtr.Zero, BufferUsageHint.StreamDraw);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, vtxSize, cmd.VtxBuffer.Data);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
            GL.BufferData(BufferTarget.ElementArrayBuffer, idxSize, IntPtr.Zero, BufferUsageHint.StreamDraw);
            GL.BufferSubData(BufferTarget.ElementArrayBuffer, IntPtr.Zero, idxSize, cmd.IdxBuffer.Data);

            int offset = 0;
            for (int i = 0; i < cmd.CmdBuffer.Size; i++)
            {
                var pcmd = cmd.CmdBuffer[i];
                GL.BindTexture(TextureTarget.Texture2D, (int)pcmd.TextureId);
                var clip = pcmd.ClipRect;
                GL.Scissor((int)clip.X, (int)(_height - clip.W), (int)(clip.Z - clip.X), (int)(clip.W - clip.Y));
                GL.DrawElementsBaseVertex(PrimitiveType.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort, (IntPtr)(offset * sizeof(ushort)), 0);
                offset += (int)pcmd.ElemCount;
            }
        }
        GL.Disable(EnableCap.Blend);
    }

    public void Dispose()
    {
        GL.DeleteBuffer(_vertexBuffer);
        GL.DeleteBuffer(_indexBuffer);
        GL.DeleteVertexArray(_vertexArray);
        GL.DeleteProgram(_shaderProgram);
        GL.DeleteTexture(_fontTexture);
    }

    public unsafe int CreateTexture(ReadOnlySpan<byte> rgba, int width, int height)
    {
        int tex = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, tex);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        fixed(byte* ptr = rgba)
        {
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, (IntPtr)ptr);
        }
        return tex;
    }
}
