using System;

namespace MiniMikuDance.Morph;

public static class MorphController
{
    public static void SetWeight(IntPtr model, int index, float weight)
    {
        if (model == IntPtr.Zero)
            return;
        Nanoem.SetMorphWeight(model, index, weight);
        Nanoem.ModelUpdateMorph(model);
    }
}
