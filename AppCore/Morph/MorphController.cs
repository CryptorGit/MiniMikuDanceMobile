using System;

namespace MiniMikuDance.Morph;

public static class MorphController
{
    public static void SetWeight(IntPtr model, int index, float weight)
    {
        if (model == IntPtr.Zero)
            return;
        NanoemMorph.SetMorphWeight(model, index, weight);
        NanoemMorph.Update(model);
    }
}
