using Assimp;
using System;
using System.IO;
using System.Collections.Generic;
using System.Buffers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Vector3D = Assimp.Vector3D;
using MMDTools;
using MiniMikuDance.App;

namespace MiniMikuDance.Import;

public class ModelData
{
    public List<SubMeshData> SubMeshes { get; set; } = new();
    public Assimp.Mesh Mesh { get; set; } = null!;
    public System.Numerics.Matrix4x4 Transform { get; set; } = System.Numerics.Matrix4x4.Identity;
    public List<BoneData> Bones { get; set; } = new();
    public Dictionary<string, int> HumanoidBones { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<(string Name, int Index)> HumanoidBoneList { get; set; } = new();
    public List<MorphData> Morphs { get; set; } = new();
    public float ShadeShift { get; set; } = -0.1f;
    public float ShadeToony { get; set; } = 0.9f;
    public float RimIntensity { get; set; } = 0.5f;
}

public class ModelImporter : IDisposable
{
    private readonly AssimpContext _context = new();
    private sealed class TextureData
    {
        public int Width;
        public int Height;
        public byte[] Pixels = Array.Empty<byte>();
    }

    private sealed class CacheItem
    {
        public TextureData Texture = null!;
        public LinkedListNode<string> Node = null!;
    }

    private static readonly Dictionary<string, CacheItem> s_textureCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly LinkedList<string> s_lruList = new();
    private static readonly object s_cacheLock = new();
    private static int s_cacheCapacity = AppSettings.DefaultTextureCacheSize;

    public static int CacheCapacity
    {
        get
        {
            lock (s_cacheLock)
            {
                return s_cacheCapacity;
            }
        }
        set
        {
            lock (s_cacheLock)
            {
                s_cacheCapacity = Math.Max(0, value);
                TrimCache();
            }
        }
    }

    public float Scale { get; set; } = AppSettings.DefaultModelScale;

    public static void ClearCache()
    {
        lock (s_cacheLock)
        {
            foreach (var item in s_textureCache.Values)
            {
                ArrayPool<byte>.Shared.Return(item.Texture.Pixels);
            }
            s_textureCache.Clear();
            s_lruList.Clear();
        }
    }

    private static void TrimCache()
    {
        lock (s_cacheLock)
        {
            while (s_textureCache.Count > s_cacheCapacity)
            {
                var last = s_lruList.Last;
                if (last is null) break;
                if (s_textureCache.TryGetValue(last.Value, out var item))
                {
                    ArrayPool<byte>.Shared.Return(item.Texture.Pixels);
                    s_textureCache.Remove(last.Value);
                }
                s_lruList.RemoveLast();
            }
        }
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    public ModelData ImportModel(Stream stream, string? textureDir = null)
    {
        Span<byte> header = stackalloc byte[4];
        int read = stream.Read(header);
        if (read < 4)
        {
            throw new NotSupportedException("PMX 以外の形式には対応していません。");
        }

        Stream src = stream;
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }
        else
        {
            var ms = new MemoryStream();
            ms.Write(header);
            stream.CopyTo(ms);
            ms.Position = 0;
            src = ms;
        }

        if (header[0] == 'P' && header[1] == 'M' && header[2] == 'X' && header[3] == ' ')
        {
            return ImportPmx(src, textureDir);
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
        var worldPositions = new System.Numerics.Vector3[bones.Length];
        for (int i = 0; i < bones.Length; i++)
        {
            var b = bones[i];
            string name = string.IsNullOrEmpty(b.NameEnglish) ? b.Name : b.NameEnglish;
            var pos = new System.Numerics.Vector3(b.Position.X, b.Position.Y, b.Position.Z) * Scale;
            worldPositions[i] = pos;
            var bd = new BoneData
            {
                Name = name,
                Parent = b.ParentBone,
                Rotation = System.Numerics.Quaternion.Identity,
                Translation = pos
            };
            if (b.IKLinkCount > 0)
            {
                var ik = new IkInfo { Target = b.IKTarget };
                foreach (var link in b.IKLinks.ToArray())
                    ik.Chain.Add(link.Bone);
                bd.Ik = ik;
            }
            boneDatas.Add(bd);
        }

        for (int i = 0; i < boneDatas.Count; i++)
        {
            int parent = boneDatas[i].Parent;
            boneDatas[i].Translation = parent >= 0 ? worldPositions[i] - worldPositions[parent] : worldPositions[i];
        }

        // Bind/InverseBind 行列を計算
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

        // ヒューマノイドボーンのマッピング
        var nameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["下半身"] = "hips",
            ["上半身"] = "spine",
            ["上半身2"] = "chest",
            ["首"] = "neck",
            ["頭"] = "head",
            ["左腕"] = "leftUpperArm",
            ["左ひじ"] = "leftLowerArm",
            ["左手首"] = "leftHand",
            ["右腕"] = "rightUpperArm",
            ["右ひじ"] = "rightLowerArm",
            ["右手首"] = "rightHand",
            ["左足"] = "leftUpperLeg",
            ["左ひざ"] = "leftLowerLeg",
            ["左足首"] = "leftFoot",
            ["右足"] = "rightUpperLeg",
            ["右ひざ"] = "rightLowerLeg",
            ["右足首"] = "rightFoot"
        };

        foreach (var hb in MiniMikuDance.Import.HumanoidBones.StandardOrder)
        {
            for (int i = 0; i < boneDatas.Count; i++)
            {
                var name = boneDatas[i].Name;
                if (nameMap.TryGetValue(name, out var mapped))
                    name = mapped;
                if (name.Equals(hb, StringComparison.OrdinalIgnoreCase))
                {
                    data.HumanoidBones[hb] = i;
                    data.HumanoidBoneList.Add((hb, i));
                    break;
                }
            }
        }
        var morphDatas = new List<MorphData>(morphs.Length);
        var nameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in morphs)
        {
            string name = string.IsNullOrEmpty(m.NameEnglish) ? m.Name : m.NameEnglish;
            name = name.Trim();
            // Ensure unique morph names so left/right or duplicates are not collapsed later
            if (nameCounts.TryGetValue(name, out var cnt))
            {
                cnt++;
                nameCounts[name] = cnt;
                var candidate = $"{name}_{cnt}";
                if (!usedNames.Add(candidate))
                {
                    Console.Error.WriteLine($"モーフ名 '{name}' のユニーク化に失敗しました。後続処理で無視されます。");
                    continue;
                }
                name = candidate;
            }
            else
            {
                nameCounts[name] = 0;
                if (!usedNames.Add(name))
                {
                    Console.Error.WriteLine($"モーフ名 '{name}' のユニーク化に失敗しました。後続処理で無視されます。");
                    continue;
                }
            }
            var md = new MorphData { Name = name, Type = m.MorphType };
            if (m.MorphType == MorphType.Vertex)
            {
                foreach (var elem in m.VertexMorphElements.Span)
                {
                    md.Offsets.Add(new MorphOffset
                    {
                        Index = elem.TargetVertex,
                        Offset = new System.Numerics.Vector3(elem.PosOffset.X * Scale, elem.PosOffset.Y * Scale, elem.PosOffset.Z * Scale)
                    });
                }
            }
            morphDatas.Add(md);
        }
        data.Morphs = morphDatas;
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
                    CacheItem? item;
                    lock (s_cacheLock)
                    {
                        if (s_textureCache.TryGetValue(texPath, out item))
                        {
                            var node = item.Node;
                            s_lruList.Remove(node);
                            s_lruList.AddFirst(node);
                            smd.TextureWidth = item.Texture.Width;
                            smd.TextureHeight = item.Texture.Height;
                            smd.TextureBytes = item.Texture.Pixels;
                        }
                    }

                    if (item is null)
                    {
                        using var image = Image.Load<Rgba32>(texPath);
                        int width = image.Width;
                        int height = image.Height;
                        int size = width * height * 4;
                        var pool = ArrayPool<byte>.Shared;
                        var pixels = pool.Rent(size);
                        image.CopyPixelDataTo(pixels);

                        lock (s_cacheLock)
                        {
                            if (!s_textureCache.TryGetValue(texPath, out item))
                            {
                                var tex = new TextureData
                                {
                                    Width = width,
                                    Height = height,
                                    Pixels = pixels
                                };
                                var node = s_lruList.AddFirst(texPath);
                                item = new CacheItem { Texture = tex, Node = node };
                                s_textureCache[texPath] = item;
                                TrimCache();
                            }
                            else
                            {
                                pool.Return(pixels);
                                var node = item.Node;
                                s_lruList.Remove(node);
                                s_lruList.AddFirst(node);
                            }
                            smd.TextureWidth = item.Texture.Width;
                            smd.TextureHeight = item.Texture.Height;
                            smd.TextureBytes = item.Texture.Pixels;
                        }
                    }
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
