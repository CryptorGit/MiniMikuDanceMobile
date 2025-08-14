using OpenTK.Mathematics;
using MiniMikuDance;

namespace MiniMikuDanceMaui.Renderers.Pmx;

public partial class PmxRenderer
{
    public void Render()
    {
        InitializeRenderingIfNeeded();
        UpdateViewProjection();
        UpdateNanoemFrame();
        RenderNanoemFrame();
    }

    private void UpdateViewProjection()
    {
        if (!_viewProjDirty)
            return;

        _cameraRot = Matrix4.CreateFromQuaternion(_externalRotation) *
                     Matrix4.CreateRotationX(_orbitX) *
                     Matrix4.CreateRotationY(_orbitY);
        _cameraPos = Vector3.TransformPosition(new Vector3(0, 0, _distance), _cameraRot) + _target;
        _viewMatrix = Matrix4.LookAt(_cameraPos, _target, Vector3.UnitY);
        float aspect = _width == 0 || _height == 0 ? 1f : _width / (float)_height;
        _projMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspect, 0.1f, 100f);
        _viewProjDirty = false;
    }
}
