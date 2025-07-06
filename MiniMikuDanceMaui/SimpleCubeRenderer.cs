namespace MiniMikuDanceMaui;

public class SimpleCubeRenderer : System.IDisposable
{
    public float RotateSensitivity { get; set; } = 1f;
    public float PanSensitivity { get; set; } = 1f;
    public bool CameraLocked { get; set; }
    public float ShadeShift { get; set; } = -0.1f;
    public float ShadeToony { get; set; } = 0.9f;
    public float RimIntensity { get; set; } = 0.5f;

    public void Initialize() { }
    public void Resize(int width, int height) { }
    public void Orbit(float dx, float dy) { }
    public void Pan(float dx, float dy) { }
    public void Dolly(float delta) { }
    public void ResetCamera() { }
    public void LoadModel(MiniMikuDance.Import.ModelData data) { }
    public void Render() { }
    public void Dispose() { }
}
