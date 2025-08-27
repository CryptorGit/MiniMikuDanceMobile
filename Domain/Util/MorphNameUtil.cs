using System;

namespace MiniMikuDance.Util;

public static class MorphNameUtil
{
    public static string EnsureUniqueName(string name, Func<string, bool> exists)
    {
        if (!exists(name))
            return name;

        var baseName = name;
        int suffix = 1;
        string newName;
        do
        {
            newName = $"{baseName}_{suffix++}";
        } while (exists(newName));

        return newName;
    }
}
