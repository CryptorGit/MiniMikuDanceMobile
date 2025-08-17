using System;
using System.IO;
using System.Numerics;
using System.Diagnostics;
using MiniMikuDance.App;
using SharpBgfx;

namespace MiniMikuDanceMaui;

public class BgfxRenderer : IRenderer
{

    private FrameBuffer? _frameBuffer;
    private Program? _program;
    private VertexBuffer? _vertexBuffer;
    private IndexBuffer? _indexBuffer;
#if DEBUG
    private uint _vertexCount;
    private uint _indexCount;
#endif

    public void Initialize()
    {
        var size = App.Initializer.Viewer?.Size ?? new Vector2(1f, 1f);
        Bgfx.Reset((int)size.X, (int)size.Y, ResetFlags.Vsync);
        Bgfx.SetViewClear(0, ClearTargets.Color | ClearTargets.Depth, 0x000000ff);

        Span<PosColorVertex> vertices = stackalloc PosColorVertex[]
        {
            new(-0.5f, -0.5f, 0f, 0xff0000ff),
            new( 0.5f, -0.5f, 0f, 0xff00ff00),
            new( 0f,   0.5f, 0f, 0xffff0000)
        };
        Span<ushort> indices = stackalloc ushort[] { 0, 1, 2 };
        _vertexBuffer = new VertexBuffer(MemoryBlock.FromArray<PosColorVertex>(vertices.ToArray()), PosColorVertex.Layout);
        _indexBuffer = new IndexBuffer(MemoryBlock.FromArray<ushort>(indices.ToArray()));
#if DEBUG
        _vertexCount = (uint)vertices.Length;
        _indexCount = (uint)indices.Length;
#endif

        _program = LoadProgram("simple");

        _frameBuffer = new FrameBuffer((int)size.X, (int)size.Y, TextureFormat.BGRA8);
    }

    public void Render()
    {
        Bgfx.Touch(0);
        if (_program != null && _vertexBuffer != null && _indexBuffer != null)
        {
            var vertexBuffer = _vertexBuffer.Value;
            var indexBuffer = _indexBuffer.Value;
            var program = _program.Value;
            Bgfx.SetVertexBuffer(0, vertexBuffer);
            Bgfx.SetIndexBuffer(indexBuffer);
#if DEBUG
            Bgfx.DebugTextClear();
            //Bgfx.DebugTextPrintf(0, 0, DebugColor.White, "VB:{0} IB:{1}", _vertexCount, _indexCount);
#endif
            Bgfx.SetRenderState(RenderState.Default);
            Bgfx.Submit(0, program, 0);
        }
        Bgfx.Frame();
    }

    public void Resize(int width, int height)
    {
        var size = App.Initializer.Viewer?.Size ?? new Vector2(width, height);
        Bgfx.Reset((int)size.X, (int)size.Y, ResetFlags.Vsync);
    }

    public void Dispose()
    {
        _indexBuffer?.Dispose();
        _vertexBuffer?.Dispose();
        _program?.Dispose();
        _frameBuffer?.Dispose();
    }

    private static Shader LoadShader(string name)
    {
        var assembly = typeof(BgfxRenderer).Assembly;
        using var stream = assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException($"Shader resource '{name}' not found.");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return new Shader(MemoryBlock.FromArray(ms.ToArray()));
    }

    private static Program LoadProgram(string baseName)
    {
#if __ANDROID__
        var suffix = "gles3";
#elif __IOS__
        var suffix = "metal";
#else
        var suffix = "spirv";
#endif
        var vsName = $"Shaders/{baseName}.vs.{suffix}.sc";
        var fsName = $"Shaders/{baseName}.fs.{suffix}.sc";
        try
        {
            var vs = LoadShader(vsName);
            var fs = LoadShader(fsName);
            return new Program(vs, fs, true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load shader program '{baseName}': {ex.Message}");
            return default;
        }
    }

    private struct PosColorVertex
    {
        public float X;
        public float Y;
        public float Z;
        public uint Abgr;

        public PosColorVertex(float x, float y, float z, uint abgr)
        {
            X = x;
            Y = y;
            Z = z;
            Abgr = abgr;
        }

        public static readonly VertexLayout Layout;

        static PosColorVertex()
        {
            Layout = new VertexLayout();
            Layout.Begin()
                .Add(VertexAttributeUsage.Position, 3, VertexAttributeType.Float)
                .Add(VertexAttributeUsage.Color0, 4, VertexAttributeType.UInt8, normalized: true)
                .End();
        }
    }
}
