using System.Collections.Generic;
using MiniMikuDance.Data;
using MMDTools;

namespace MiniMikuDance.Import;

public static class MorphDataExtensions
{
    public static Morph ToMorph(this MorphData src)
    {
        var morph = new Morph
        {
            Index = src.Index,
            NameJa = src.Name,
            NameEn = src.Name,
            Type = src.Type,
            Category = src.Category,
            Objects = new List<MorphObject>()
        };

        foreach (var off in src.Offsets)
        {
            MorphObject? obj = src.Type switch
            {
                MorphType.Vertex => new VertexMorphObject
                {
                    Index = off.Index,
                    Offset = off.Vertex
                },
                MorphType.Group => new GroupMorphObject
                {
                    Index = off.Index,
                    MorphIndex = off.Group.MorphIndex,
                    Rate = off.Group.Rate
                },
                MorphType.Bone => new BoneMorphObject
                {
                    Index = off.Index,
                    Translation = off.Bone.Translation,
                    Rotation = off.Bone.Rotation
                },
                MorphType.UV => new UvMorphObject
                {
                    Index = off.Index,
                    Offset = off.Uv.Offset
                },
                MorphType.Material => new MaterialMorphObject
                {
                    Index = off.Index,
                    IsAll = off.Material.IsAll,
                    CalcMode = off.Material.CalcMode,
                    Diffuse = off.Material.Diffuse,
                    Specular = off.Material.Specular,
                    SpecularPower = off.Material.SpecularPower,
                    EdgeColor = off.Material.EdgeColor,
                    EdgeSize = off.Material.EdgeSize,
                    ToonColor = off.Material.ToonColor,
                    TextureTint = off.Material.TextureTint
                },
                _ => null
            };
            if (obj != null)
            {
                morph.Objects.Add(obj);
            }
        }
        return morph;
    }
}
