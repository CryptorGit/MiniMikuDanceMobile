using System;

namespace MiniMikuDance.Util;

public static class MorphNameUtil
{
    public static string EnsureUniqueName(string name, Func<string, bool> exists, Action<string>? logger = null)
    {
        if (!exists(name))
            return name;

        logger?.Invoke($"Duplicate morph name detected: {name}");
        var baseName = name;
        int suffix = 1;
        string newName;
        do
        {
            newName = $"{baseName}_{suffix++}";
        } while (exists(newName));
        logger?.Invoke($"Renaming morph '{baseName}' to '{newName}'");
        return newName;
    }
}
