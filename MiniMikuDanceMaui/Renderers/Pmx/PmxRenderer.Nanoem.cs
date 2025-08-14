using MiniMikuDance;

namespace MiniMikuDanceMaui.Renderers.Pmx;

public partial class PmxRenderer
{
    private bool _renderingInitialized;

    private void InitializeRenderingIfNeeded()
    {
        if (_renderingInitialized)
            return;

        Nanoem.RenderingInitialize(_width, _height);
        _renderingInitialized = true;
    }

    private void UpdateNanoemFrame()
    {
        Nanoem.RenderingUpdateFrame();
    }

    private void RenderNanoemFrame()
    {
        Nanoem.RenderingRenderFrame();
    }
}
