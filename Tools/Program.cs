using System;
using System.IO;
using System.Text.Json;

if (args.Length == 0)
{
    Console.WriteLine("Usage: VrmAnalyzer <vrm_or_json>");
    return;
}

var path = args[0];
if (!File.Exists(path))
{
    Console.WriteLine($"File not found: {path}");
    return;
}

JsonDocument doc;
if (Path.GetExtension(path).Equals(".vrm", StringComparison.OrdinalIgnoreCase))
{
    using var fs = File.OpenRead(path);
    using var br = new BinaryReader(fs);
    if (br.ReadUInt32() != 0x46546C67)
    {
        Console.WriteLine("Invalid VRM file.");
        return;
    }
    br.ReadUInt32();
    br.ReadUInt32();
    uint jsonLen = br.ReadUInt32();
    if (br.ReadUInt32() != 0x4E4F534A)
    {
        Console.WriteLine("JSON chunk not found.");
        return;
    }
    var jsonBytes = br.ReadBytes((int)jsonLen);
    doc = JsonDocument.Parse(jsonBytes);
}
else
{
    using var stream = File.OpenRead(path);
    doc = JsonDocument.Parse(stream);
}
var root = doc.RootElement;

if (root.TryGetProperty("extensions", out var ext) &&
    ext.TryGetProperty("VRM", out var vrm))
{
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
