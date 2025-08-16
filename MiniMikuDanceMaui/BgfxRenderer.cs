using System;
using System.Numerics;
using MiniMikuDance.App;
using SharpBgfx;

namespace MiniMikuDanceMaui;

public class BgfxRenderer : IRenderer
{
    private FrameBuffer? _frameBuffer;
    private Program? _program;
    private VertexBuffer? _vertexBuffer;
    private IndexBuffer? _indexBuffer;

    public void Initialize()
    {
        Bgfx.Init();
        var size = App.Initializer.Viewer?.Size ?? new Vector2(1f, 1f);
        Bgfx.Reset((uint)size.X, (uint)size.Y, ResetFlags.Vsync);
        Bgfx.SetViewClear(0, ClearTargets.Color | ClearTargets.Depth, 0x000000ff);

        Span<PosColorVertex> vertices = stackalloc PosColorVertex[]
        {
            new(-0.5f, -0.5f, 0f, 0xff0000ff),
            new( 0.5f, -0.5f, 0f, 0xff00ff00),
            new( 0f,   0.5f, 0f, 0xffff0000)
        };
        Span<ushort> indices = stackalloc ushort[] { 0, 1, 2 };
        _vertexBuffer = new VertexBuffer(MemoryBlock.FromArray(vertices), PosColorVertex.Layout);
        _indexBuffer = new IndexBuffer(MemoryBlock.FromArray(indices));

        var emptyShader = MemoryBlock.FromArray(Array.Empty<byte>());
        var vs = Bgfx.CreateShader(emptyShader);
        var fs = Bgfx.CreateShader(emptyShader);
        _program = Bgfx.CreateProgram(vs, fs, true);

        _frameBuffer = Bgfx.CreateFrameBuffer((uint)size.X, (uint)size.Y, TextureFormat.BGRA8);
    }

    public void Render()
    {
        Bgfx.Touch(0);
        if (_program != null && _vertexBuffer != null && _indexBuffer != null)
        {
            Bgfx.SetVertexBuffer(0, _vertexBuffer);
            Bgfx.SetIndexBuffer(_indexBuffer);
            Bgfx.SetRenderState(RenderState.Default);
            Bgfx.Submit(0, _program);
        }
        Bgfx.Frame();
    }

    public void Resize(int width, int height)
    {
        var size = App.Initializer.Viewer?.Size ?? new Vector2(width, height);
        Bgfx.Reset((uint)size.X, (uint)size.Y, ResetFlags.Vsync);
    }

    public void Dispose()
    {
        _indexBuffer?.Dispose();
        _vertexBuffer?.Dispose();
        _program?.Dispose();
        _frameBuffer?.Dispose();
        Bgfx.Shutdown();
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
                .Add(VertexAttribute.Position, 3, VertexAttributeType.Float)
                .Add(VertexAttribute.Color0, 4, VertexAttributeType.Uint8, normalized: true)
                .End();
        }
    }
}
