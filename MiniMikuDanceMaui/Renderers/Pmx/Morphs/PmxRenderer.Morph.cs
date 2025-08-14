using System;
using System.Collections.Generic;
using MiniMikuDance.Morph;

namespace MiniMikuDanceMaui.Renderers.Pmx;

public partial class PmxRenderer
{

    public void SetMorph(string name, float value)
    {
        if (!_morphs.TryGetValue(name, out var morph))
            return;

        value = Math.Clamp(value, 0f, 1f);
        if (MathF.Abs(value) < 1e-5f) value = 0f;

        _morphValues.TryGetValue(name, out var current);
        if (MathF.Abs(current - value) < 1e-5f)
            return;

        if (value == 0f)
            _morphValues.Remove(name);
        else
            _morphValues[name] = value;

        if (_modelHandle != IntPtr.Zero)
        {
            var pairs = new (int, float)[_morphValues.Count];
            int i = 0;
            foreach (var (mName, mv) in _morphValues)
            {
                pairs[i++] = (_morphs[mName].Index, mv);
            }
            MorphController.SetWeight(_modelHandle, pairs);
        }

        _morphDirty = true;
        _uvMorphDirty = true;
        _bonesDirty = true;
        Viewer?.InvalidateSurface();
    }

    public IReadOnlyList<MorphData> GetMorphs(MorphCategory category)
    {
        return _morphsByCategory.TryGetValue(category, out var list) ? list : Array.Empty<MorphData>();
    }

    public void SetMorph(MorphCategory category, string name, float value)
    {
        if (_morphs.TryGetValue(name, out var morph) && morph.Category == category)
        {
            SetMorph(name, value);
        }
    }

    partial void InitializeMorphModule()
    {
        RegisterModule(new MorphModule());
    }

    private class MorphModule : PmxRendererModuleBase
    {
    }
}
