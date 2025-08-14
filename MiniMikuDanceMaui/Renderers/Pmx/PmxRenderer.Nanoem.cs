using MiniMikuDance;
using MiniMikuDance.Util;
using OpenTK.Mathematics;

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
        var cameraPos = _cameraPos.ToNumerics();
        var target = _target.ToNumerics();
        Nanoem.RenderingSetCamera(cameraPos, target);

        Vector3 light = Vector3.Normalize(new Vector3(0.3f, 0.6f, -0.7f));
        light = Vector3.TransformNormal(light, _cameraRot);
        Nanoem.RenderingSetLight(light.ToNumerics());

        Nanoem.RenderingSetGridVisible(true);
        Nanoem.RenderingSetStageSize(_stageSize);

        Nanoem.RenderingUpdateFrame();
    }

    private void RenderNanoemFrame()
    {
        Nanoem.RenderingRenderFrame();
    }
}
