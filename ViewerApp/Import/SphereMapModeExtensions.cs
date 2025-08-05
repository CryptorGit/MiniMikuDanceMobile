namespace ViewerApp.Import;

public static class SphereMapModeExtensions
{
    public static SphereMapMode ToSphereMapMode(this MMDTools.SphereTextureMode mode, bool validate = false)
    {
        int value = (int)mode;
        if (validate && !Enum.IsDefined(typeof(SphereMapMode), value))
        {
            return SphereMapMode.None;
        }
        return (SphereMapMode)value;
    }
}
