using System;
using System.IO;
using System.Text.Json;

if (args.Length == 0)
{
    Console.WriteLine("Usage: VrmAnalyzer <vrm_json>");
    return;
}

var path = args[0];
if (!File.Exists(path))
{
    Console.WriteLine($"File not found: {path}");
    return;
}

using var stream = File.OpenRead(path);
using var doc = JsonDocument.Parse(stream);
var root = doc.RootElement;

if (root.TryGetProperty("extensions", out var ext) &&
    ext.TryGetProperty("VRM", out var vrm))
{
    if (vrm.TryGetProperty("humanoid", out var humanoid) &&
        humanoid.TryGetProperty("humanBones", out var bones) &&
        bones.ValueKind == JsonValueKind.Array)
    {
        Console.WriteLine("Humanoid Bones:");
        foreach (var b in bones.EnumerateArray())
        {
            if (b.TryGetProperty("bone", out var boneEl) &&
                b.TryGetProperty("node", out var nodeEl))
            {
                Console.WriteLine($"  {boneEl.GetString()} -> node {nodeEl.GetInt32()}");
            }
        }
    }
    if (vrm.TryGetProperty("materialProperties", out var matProps) &&
        matProps.ValueKind == JsonValueKind.Array)
    {
        Console.WriteLine("Materials:");
        foreach (var m in matProps.EnumerateArray())
        {
            var name = m.GetProperty("name").GetString();
            var shader = m.GetProperty("shader").GetString();
            Console.WriteLine($"  {name} (shader: {shader})");
        }
    }
}
else
{
    Console.WriteLine("VRM extension not found.");
}
