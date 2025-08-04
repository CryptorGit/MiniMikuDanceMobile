using Assimp;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Vector3D = Assimp.Vector3D;
using MMDTools;

namespace MiniMikuDance.Import;

public class ModelData
{
    public List<SubMeshData> SubMeshes { get; set; } = new();
    public Assimp.Mesh Mesh { get; set; } = null!;
    public System.Numerics.Matrix4x4 Transform { get; set; } = System.Numerics.Matrix4x4.Identity;
    public List<BoneData> Bones { get; set; } = new();
    public Dictionary<string, int> HumanoidBones { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<(string Name, int Index)> HumanoidBoneList { get; set; } = new();
    public List<int> IkBoneIndices { get; set; } = new();
    public float ShadeShift { get; set; } = -0.1f;
    public float ShadeToony { get; set; } = 0.9f;
    public float RimIntensity { get; set; } = 0.5f;
    public List<MorphData> Morphs { get; set; } = new();
}

public class MorphData
{
    public string Name { get; set; } = string.Empty;
    public MorphType Type { get; set; }
    public List<MorphOffset> Offsets { get; set; } = new();
}

public class MorphOffset
{
    public int Index { get; set; }
    public System.Numerics.Vector3 Offset { get; set; }
}

public class ModelImporter
{
    private readonly AssimpContext _context = new();
    public float Scale { get; set; } = 1.0f;

    public ModelData ImportModel(Stream stream, string? textureDir = null)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var bytes = ms.ToArray();
        ms.Position = 0;

        // ファイルヘッダを確認して PMX かどうか判断する
        if (bytes.Length >= 4 &&
            bytes[0] == 'P' && bytes[1] == 'M' && bytes[2] == 'X' && bytes[3] == ' ')
        {
            return ImportPmx(ms, textureDir);
        }

        throw new NotSupportedException("PMX 以外の形式には対応していません。");
    }

    public ModelData ImportModel(string path)
    {


        if (!File.Exists(path))
        {

            throw new FileNotFoundException("Model file not found", path);
        }

        string ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext == ".pmx")
        {
            using var fs = File.OpenRead(path);
            return ImportModel(fs, Path.GetDirectoryName(path));
        }

        var scene = _context.ImportFile(path, PostProcessSteps.Triangulate | PostProcessSteps.GenerateNormals);

        return new ModelData { Mesh = scene.Meshes[0] };
    }

    private ModelData ImportPmx(Stream stream, string? textureDir = null)
    {
        var pmx = PMXParser.Parse(stream);
        var verts = pmx.VertexList.ToArray();
        var faces = pmx.SurfaceList.ToArray();
        var mats = pmx.MaterialList.ToArray();
        var texList = pmx.TextureList.ToArray();
        var bones = pmx.BoneList.ToArray();
        var morphs = pmx.MorphList.ToArray();

        // ボーン情報を ModelData に格納する
        var data = new ModelData();
        var boneDatas = new List<BoneData>(bones.Length);
        var absPositions = new System.Numerics.Vector3[bones.Length];
        for (int i = 0; i < bones.Length; i++)
        {
            var b = bones[i];
            string name = string.IsNullOrEmpty(b.NameEnglish) ? b.Name : b.NameEnglish;
            var absPos = new System.Numerics.Vector3(b.Position.X, b.Position.Y, b.Position.Z) * Scale;
            absPositions[i] = absPos;
            var localPos = absPos;
            if (b.ParentBone >= 0)
                localPos -= absPositions[b.ParentBone];
            var bd = new BoneData
            {
                Name = name,
                Parent = b.ParentBone,
                Rotation = System.Numerics.Quaternion.Identity,
                Translation = localPos
            };
            if (b.BoneFlag.HasFlag(BoneFlag.IK))
            {
                bd.IsIk = true;
                bd.IkTargetIndex = b.IKTarget;
                bd.IkLoopCount = b.IterCount;
                bd.IkAngleLimit = b.MaxRadianPerIter;
                foreach (var link in b.IKLinks.Span)
                {
                    bd.IkChainIndices.Add(link.Bone);
                }
                data.IkBoneIndices.Add(i);
            }
            boneDatas.Add(bd);
        }

        // Bind/InverseBind 行列を計算（ローカル位置を使用）
        var world = new System.Numerics.Matrix4x4[boneDatas.Count];
        for (int i = 0; i < boneDatas.Count; i++)
        {
            var bd = boneDatas[i];
            var local = System.Numerics.Matrix4x4.CreateFromQuaternion(bd.Rotation) *
                        System.Numerics.Matrix4x4.CreateTranslation(bd.Translation);
            if (bd.Parent >= 0)
                world[i] = local * world[bd.Parent];
            else
                world[i] = local;
            bd.BindMatrix = world[i];
            System.Numerics.Matrix4x4.Invert(world[i], out var inv);
            bd.InverseBindMatrix = inv;
        }
        data.Bones = boneDatas;

        // モーフ情報の解析
        var morphDatas = new List<MorphData>(morphs.Length);
        foreach (var m in morphs)
        {
            var md = new MorphData
            {
                Name = string.IsNullOrEmpty(m.NameEnglish) ? m.Name : m.NameEnglish,
                Type = m.MorphType
            };
            foreach (var vm in m.VertexMorphElements.ToArray())
            {
                md.Offsets.Add(new MorphOffset
                {
                    Index = vm.TargetVertex,
                    Offset = new System.Numerics.Vector3(vm.PosOffset.X, vm.PosOffset.Y, vm.PosOffset.Z) * Scale
                });
            }
            morphDatas.Add(md);
        }
        data.Morphs = morphDatas;

        // ヒューマノイドボーンのマッピング
        foreach (var hb in MiniMikuDance.Import.HumanoidBones.StandardOrder)
        {
            for (int i = 0; i < boneDatas.Count; i++)
            {
                if (boneDatas[i].Name.Equals(hb, StringComparison.OrdinalIgnoreCase))
                {
                    data.HumanoidBones[hb] = i;
                    data.HumanoidBoneList.Add((hb, i));
                    break;
                }
            }
        }
        var combined = new Assimp.Mesh("pmx", Assimp.PrimitiveType.Triangle);
        for (int i = 0; i < verts.Length; i++)
        {
            var v = verts[i];
            combined.Vertices.Add(new Vector3D(v.Position.X * Scale, v.Position.Y * Scale, v.Position.Z * Scale));
            combined.Normals.Add(new Vector3D(v.Normal.X, v.Normal.Y, v.Normal.Z));
            combined.TextureCoordinateChannels[0].Add(new Vector3D(v.UV.X, v.UV.Y, 0));
        }
        foreach (var f in faces)
        {
            var face = new Face();
            face.Indices.Add(f.V1);
            face.Indices.Add(f.V2);
            face.Indices.Add(f.V3);
            combined.Faces.Add(face);
        }

        data.Mesh = combined;
        int faceOffset = 0;
        string dir = textureDir ?? string.Empty;
        foreach (var mat in mats)
        {
            var sub = new Assimp.Mesh("pmx", Assimp.PrimitiveType.Triangle);
            var smd = new SubMeshData
            {
                Mesh = sub,
                ColorFactor = new System.Numerics.Vector4(mat.Diffuse.R, mat.Diffuse.G, mat.Diffuse.B, mat.Diffuse.A)
            };

            int faceCount = mat.VertexCount / 3;
            for (int i = 0; i < faceCount; i++)
            {
                var sf = faces[faceOffset + i];
                int[] idxs = { sf.V1, sf.V2, sf.V3 };
                int baseIndex = sub.Vertices.Count;
                for (int j = 0; j < 3; j++)
                {
                    var vv = verts[idxs[j]];
                    sub.Vertices.Add(new Vector3D(vv.Position.X * Scale, vv.Position.Y * Scale, vv.Position.Z * Scale));
                    sub.Normals.Add(new Vector3D(vv.Normal.X, vv.Normal.Y, vv.Normal.Z));
                    sub.TextureCoordinateChannels[0].Add(new Vector3D(vv.UV.X, vv.UV.Y, 0));
                    smd.TexCoords.Add(new System.Numerics.Vector2(vv.UV.X, vv.UV.Y));

                    System.Numerics.Vector4 ji = System.Numerics.Vector4.Zero;
                    System.Numerics.Vector4 jw = System.Numerics.Vector4.Zero;
                    switch (vv.WeightTransformType)
                    {
                        case WeightTransformType.BDEF1:
                            ji.X = vv.BoneIndex1;
                            jw.X = 1f;
                            break;
                        case WeightTransformType.BDEF2:
                        case WeightTransformType.SDEF:
                            ji.X = vv.BoneIndex1;
                            ji.Y = vv.BoneIndex2;
                            jw.X = vv.Weight1;
                            jw.Y = 1f - vv.Weight1;
                            break;
                        case WeightTransformType.BDEF4:
                        case WeightTransformType.QDEF:
                        default:
                            ji = new System.Numerics.Vector4(vv.BoneIndex1, vv.BoneIndex2, vv.BoneIndex3, vv.BoneIndex4);
                            jw = new System.Numerics.Vector4(vv.Weight1, vv.Weight2, vv.Weight3, vv.Weight4);
                            break;
                    }
                    smd.JointIndices.Add(ji);
                    smd.JointWeights.Add(jw);
                    smd.BaseVertexIndices.Add(idxs[j]);
                }
                var face = new Face();
                face.Indices.Add(baseIndex);
                face.Indices.Add(baseIndex + 1);
                face.Indices.Add(baseIndex + 2);
                sub.Faces.Add(face);
            }

            if (!string.IsNullOrEmpty(dir) && mat.Texture >= 0 && mat.Texture < texList.Length)
            {
                var texName = texList[mat.Texture]
                    .Replace('\\', Path.DirectorySeparatorChar);
                var texPath = Path.Combine(dir, texName);
                smd.TextureFilePath = texName;
                if (File.Exists(texPath))
                {
                    using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(texPath);
                    smd.TextureWidth = image.Width;
                    smd.TextureHeight = image.Height;
                    smd.TextureBytes = new byte[image.Width * image.Height * 4];
                    image.CopyPixelDataTo(smd.TextureBytes);
                }
            }

            data.SubMeshes.Add(smd);
            faceOffset += faceCount;
        }
        data.Transform = System.Numerics.Matrix4x4.CreateScale(Scale);
        return data;
    }
    // 現在は PMX モデルのみに対応しています
}
