using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace ViewerApp;

public static class VrmUtil
{
    public static Dictionary<int, int> ReadMainTextureIndices(byte[] glb)
    {
        var map = new Dictionary<int, int>();
        try
        {
            using var ms = new MemoryStream(glb);
            using var br = new BinaryReader(ms);
            uint magic = br.ReadUInt32();
            if (magic != 0x46546C67) return map; // 'glTF'
            br.ReadUInt32(); // version
            br.ReadUInt32(); // length
            uint jsonLen = br.ReadUInt32();
            uint chunkType = br.ReadUInt32();
            if (chunkType != 0x4E4F534A) return map; // JSON
            byte[] jsonBytes = br.ReadBytes((int)jsonLen);
            string json = Encoding.UTF8.GetString(jsonBytes);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            // VRM 0.x extension
            if (root.TryGetProperty("extensions", out var exts) &&
                exts.TryGetProperty("VRM", out var vrmExt) &&
                vrmExt.TryGetProperty("materialProperties", out var matProps) &&
                matProps.ValueKind == JsonValueKind.Array &&
                root.TryGetProperty("materials", out var materials))
            {
                foreach (var m in matProps.EnumerateArray())
                {
                    if (!m.TryGetProperty("name", out var nameEl)) continue;
                    string? name = nameEl.GetString();
                    int matIndex = -1;
                    for (int i = 0; i < materials.GetArrayLength(); i++)
                    {
                        var mat = materials[i];
                        if (mat.TryGetProperty("name", out var nm) && nm.GetString() == name)
                        {
                            matIndex = i;
                            break;
                        }
                    }
                    if (matIndex >= 0 &&
                        m.TryGetProperty("textureProperties", out var texProps) &&
                        texProps.TryGetProperty("_MainTex", out var mainTexEl))
                    {
                        map[matIndex] = mainTexEl.GetInt32();
                    }
                }
            }
            // VRM 1.0 extension
            if (root.TryGetProperty("materials", out var mats1))
            {
                for (int i = 0; i < mats1.GetArrayLength(); i++)
                {
                    var mat = mats1[i];
                    if (mat.TryGetProperty("extensions", out var mext) &&
                        mext.TryGetProperty("VRMC_materials_mtoon", out var mtoon) &&
                        mtoon.TryGetProperty("textures", out var texs) &&
                        texs.TryGetProperty("mainTexture", out var main) &&
                        main.TryGetProperty("index", out var idxEl))
                    {
                        map[i] = idxEl.GetInt32();
                    }
                }
            }
        }
        catch
        {
        }
        return map;
    }
}
