namespace MiniMikuDanceMaui.Renderers.Pmx;

public interface IPmxRendererModule
{
    void Initialize(PmxRenderer renderer);
}

public abstract class PmxRendererModuleBase : IPmxRendererModule
{
    protected PmxRenderer Renderer { get; private set; } = null!;

    public virtual void Initialize(PmxRenderer renderer)
    {
        Renderer = renderer;
    }
}

