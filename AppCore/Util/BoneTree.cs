using MiniMikuDance.Import;
using System.Collections.Generic;
using System.Linq;

namespace MiniMikuDance.Util;

public class BoneNode
{
    public string Name { get; set; } = string.Empty;
    public List<BoneNode> Children { get; } = new();
}

public static class BoneTreeBuilder
{
    public static List<BoneNode> Build(IReadOnlyList<BoneData> bones)
    {
        var nodes = bones.Select(b => new BoneNode { Name = b.Name }).ToArray();
        var roots = new List<BoneNode>();
        for (int i = 0; i < bones.Count; i++)
        {
            var parent = bones[i].Parent;
            if (parent >= 0 && parent < nodes.Length)
                nodes[parent].Children.Add(nodes[i]);
            else
                roots.Add(nodes[i]);
        }
        return roots;
    }
}
