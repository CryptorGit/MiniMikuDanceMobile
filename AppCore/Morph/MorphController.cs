using System;

namespace MiniMikuDance.Morph;

public static class MorphController
{
    public static void SetWeight(IntPtr model, ReadOnlySpan<(int index, float weight)> morphs)
    {
        if (model == IntPtr.Zero)
            return;
        foreach (var (index, weight) in morphs)
        {
            NanoemMorph.SetMorphWeight(model, index, weight);
        }
        NanoemMorph.Update(model);
    }

    public static void SetWeight(IntPtr model, int index, float weight)
    {
        Span<(int, float)> values = stackalloc (int, float)[1];
        values[0] = (index, weight);
        SetWeight(model, values);
    }
}
