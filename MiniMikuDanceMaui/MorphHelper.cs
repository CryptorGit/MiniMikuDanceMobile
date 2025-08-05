namespace MiniMikuDanceMaui;

using MiniMikuDance.Import;
using System;
using System.Collections.Generic;

public static class MorphHelper
{
    public static IEnumerable<(string originalName, string labelName)> BuildMorphEntries(ModelData model)
    {
        if (model.Morphs == null)
            yield break;

        var nameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var morph in model.Morphs)
        {
            var displayName = morph.Name.Trim();
            nameCounts[displayName] = nameCounts.TryGetValue(displayName, out var c) ? c + 1 : 1;
        }

        var nameIndices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var morph in model.Morphs)
        {
            string originalName = morph.Name;
            string displayName = originalName.Trim();

            int dupIndex = nameIndices.TryGetValue(displayName, out var v) ? v + 1 : 1;
            nameIndices[displayName] = dupIndex;

            string labelName = nameCounts[displayName] > 1
                ? $"{displayName} ({dupIndex})"
                : displayName;

            yield return (originalName, labelName);
        }
    }
}
