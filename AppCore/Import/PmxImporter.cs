using Assimp;
using System;
using System.IO;
using System.Collections.Generic;
using System.Buffers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
    public List<RigidBodyData> RigidBodies { get; set; } = new();
    public List<JointData> Joints { get; set; } = new();
    public List<SoftBodyData> SoftBodies { get; set; } = new();
    public List<DisplayFrameData> DisplayFrames { get; set; } = new();
    public float ShadeShift { get; set; } = -0.1f;
    public float ShadeToony { get; set; } = 0.9f;
    public float RimIntensity { get; set; } = 0.5f;
    public string ModelName { get; set; } = string.Empty;
    public string ModelNameEnglish { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public string CommentEnglish { get; set; } = string.Empty;
}

public class PmxImporter : IModelImporter
{
    private readonly AssimpContext _context = new();
    private readonly ILogger<PmxImporter> _logger;
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

public PmxImporter(ILogger<PmxImporter>? logger = null)
    {
        _logger = logger ?? NullLogger<PmxImporter>.Instance;
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    public ModelData ImportModel(Stream stream, string? textureDir = null)
    {
        var header = ReadHeader(stream);
        if (IsPmx(header))
        {
            if (stream.CanSeek)
            {
                stream.Position = 0;
                return ImportPmx(stream, textureDir);
            }

            using var ms = CopyToMemoryStream(stream, header);
            return ImportPmx(ms, textureDir);
        }

        throw new NotSupportedException("PMX 以外の形式には対応していません。");
    }

    private static byte[] ReadHeader(Stream stream)
    {
        var header = new byte[4];
        int read = stream.Read(header, 0, 4);
        if (read < 4)
        {
            throw new NotSupportedException("PMX 以外の形式には対応していません。");
        }
        return header;
    }

    private static bool IsPmx(byte[] header)
    {
        return header.Length >= 4 && header[0] == 'P' && header[1] == 'M' && header[2] == 'X' && header[3] == ' ';
    }

    private static MemoryStream CopyToMemoryStream(Stream stream, byte[] header)
    {
        var ms = new MemoryStream();
        ms.Write(header, 0, header.Length);
        stream.CopyTo(ms);
        ms.Position = 0;
        return ms;
    }

    private System.Numerics.Vector3 ScaleVector(dynamic v)
    {
        return new System.Numerics.Vector3(v.X * Scale, v.Y * Scale, v.Z * Scale);
    }

    private SubMeshData CreateSubMesh(dynamic mat)
    {
        var sub = new Assimp.Mesh("pmx", Assimp.PrimitiveType.Triangle);
        return new SubMeshData
        {
            Mesh = sub,
            ColorFactor = new System.Numerics.Vector4(mat.Diffuse.R, mat.Diffuse.G, mat.Diffuse.B, mat.Diffuse.A),
            Specular = new System.Numerics.Vector3(mat.Specular.R, mat.Specular.G, mat.Specular.B),
            SpecularPower = mat.Shininess,
            EdgeColor = new System.Numerics.Vector4(mat.EdgeColor.R, mat.EdgeColor.G, mat.EdgeColor.B, mat.EdgeColor.A),
            EdgeSize = mat.EdgeSize,
            ToonColor = System.Numerics.Vector3.One,
            TextureTint = System.Numerics.Vector4.One
        };
    }

    private void ProcessVertex(Assimp.Mesh mesh, SubMeshData smd, dynamic vertex)
    {
        var pos = ScaleVector(vertex.Position);
        mesh.Vertices.Add(new Vector3D(pos.X, pos.Y, pos.Z));
        mesh.Normals.Add(new Vector3D(vertex.Normal.X, vertex.Normal.Y, vertex.Normal.Z));
        mesh.TextureCoordinateChannels[0].Add(new Vector3D(vertex.UV.X, vertex.UV.Y, 0));
        smd.TexCoords.Add(new System.Numerics.Vector2(vertex.UV.X, vertex.UV.Y));
        var auv1 = vertex.AdditionalUV1;
        smd.AdditionalUV1.Add(new System.Numerics.Vector4(auv1.X, auv1.Y, auv1.Z, auv1.W));
        var auv2 = vertex.AdditionalUV2;
        smd.AdditionalUV2.Add(new System.Numerics.Vector4(auv2.X, auv2.Y, auv2.Z, auv2.W));
        var auv3 = vertex.AdditionalUV3;
        smd.AdditionalUV3.Add(new System.Numerics.Vector4(auv3.X, auv3.Y, auv3.Z, auv3.W));
        var auv4 = vertex.AdditionalUV4;
        smd.AdditionalUV4.Add(new System.Numerics.Vector4(auv4.X, auv4.Y, auv4.Z, auv4.W));

        System.Numerics.Vector4 ji = System.Numerics.Vector4.Zero;
        System.Numerics.Vector4 jw = System.Numerics.Vector4.Zero;
        System.Numerics.Vector3 sc = System.Numerics.Vector3.Zero;
        System.Numerics.Vector3 sr0 = System.Numerics.Vector3.Zero;
        System.Numerics.Vector3 sr1 = System.Numerics.Vector3.Zero;
        switch ((WeightTransformType)vertex.WeightTransformType)
        {
            case WeightTransformType.BDEF1:
                ji.X = vertex.BoneIndex1;
                jw.X = 1f;
                break;
            case WeightTransformType.BDEF2:
                ji.X = vertex.BoneIndex1;
                ji.Y = vertex.BoneIndex2;
                jw.X = vertex.Weight1;
                jw.Y = 1f - vertex.Weight1;
                break;
            case WeightTransformType.SDEF:
                ji.X = vertex.BoneIndex1;
                ji.Y = vertex.BoneIndex2;
                jw.X = vertex.Weight1;
                jw.Y = 1f - vertex.Weight1;
                sc = ScaleVector(vertex.C);
                sr0 = ScaleVector(vertex.R0);
                sr1 = ScaleVector(vertex.R1);
                break;
            case WeightTransformType.BDEF4:
            case WeightTransformType.QDEF:
            default:
                ji = new System.Numerics.Vector4(vertex.BoneIndex1, vertex.BoneIndex2, vertex.BoneIndex3, vertex.BoneIndex4);
                jw = new System.Numerics.Vector4(vertex.Weight1, vertex.Weight2, vertex.Weight3, vertex.Weight4);
                break;
        }
        smd.JointIndices.Add(ji);
        smd.JointWeights.Add(jw);
        smd.SdefC.Add(sc);
        smd.SdefR0.Add(sr0);
        smd.SdefR1.Add(sr1);
    }

    private void AddFace(Assimp.Mesh mesh, SubMeshData smd, dynamic[] verts, Surface face)
    {
        int[] idxs = { (int)face.V1, (int)face.V2, (int)face.V3 };
        int baseIndex = mesh.Vertices.Count;
        for (int j = 0; j < 3; j++)
        {
            ProcessVertex(mesh, smd, verts[idxs[j]]);
        }
        var f = new Face();
        f.Indices.Add(baseIndex);
        f.Indices.Add(baseIndex + 1);
        f.Indices.Add(baseIndex + 2);
        mesh.Faces.Add(f);
    }

    private void GenerateSubMeshes(ModelData data, dynamic[] verts, Surface[] faces, dynamic[] mats, string[] texList, string? textureDir)
    {
        int faceOffset = 0;
        string dir = textureDir ?? string.Empty;
        foreach (var mat in mats)
        {
            var smd = CreateSubMesh(mat);
            var sub = smd.Mesh;
            smd.SphereMode = mat.SphereMode;
            smd.ToonMode = mat.ToonMode;
            int faceCount = mat.VertexCount / 3;
            for (int i = 0; i < faceCount; i++)
            {
                var sf = faces[faceOffset + i];
                AddFace(sub, smd, verts, sf);
            }

            if (!string.IsNullOrEmpty(dir) && mat.Texture >= 0 && mat.Texture < texList.Length)
            {
                TryLoadTexture(smd, texList[mat.Texture], dir);
            }

            if (!string.IsNullOrEmpty(dir) && mat.SphereTexture >= 0 && mat.SphereTexture < texList.Length)
            {
                var tmp = new SubMeshData();
                TryLoadTexture(tmp, texList[mat.SphereTexture], dir);
                smd.SphereTextureBytes = tmp.TextureBytes;
                smd.SphereTextureWidth = tmp.TextureWidth;
                smd.SphereTextureHeight = tmp.TextureHeight;
                smd.SphereTextureFilePath = tmp.TextureFilePath;
            }

            if (!string.IsNullOrEmpty(dir) && mat.ToonTexture >= 0 && mat.ToonTexture < texList.Length)
            {
                var tmp = new SubMeshData();
                TryLoadTexture(tmp, texList[mat.ToonTexture], dir);
                smd.ToonTextureBytes = tmp.TextureBytes;
                smd.ToonTextureWidth = tmp.TextureWidth;
                smd.ToonTextureHeight = tmp.TextureHeight;
                smd.ToonTextureFilePath = tmp.TextureFilePath;
            }

            data.SubMeshes.Add(smd);
            faceOffset += faceCount;
        }
    }

    private static string ResolveTexturePath(string textureName, string directory, out string normalized)
    {
        normalized = textureName.Replace('\\', Path.DirectorySeparatorChar);
        return Path.Combine(directory, normalized);
    }

    private void TryLoadTexture(SubMeshData subMeshData, string textureName, string directory)
    {
        var texPath = ResolveTexturePath(textureName, directory, out var normalized);
        subMeshData.TextureFilePath = normalized;
        if (!File.Exists(texPath))
        {
            var fallbackPath = ResolveTexturePath("MissingTexture.png", directory, out _);
            if (!File.Exists(fallbackPath))
            {
                _logger.LogWarning("テクスチャ '{Texture}' が見つからず、MissingTexture.png も存在しません。", normalized);
                return;
            }
            _logger.LogWarning("テクスチャ '{Texture}' が見つかりません。MissingTexture.png を使用します。", normalized);
            texPath = fallbackPath;
        }

        CacheItem? item;
        lock (s_cacheLock)
        {
            if (s_textureCache.TryGetValue(texPath, out item))
            {
                var node = item.Node;
                s_lruList.Remove(node);
                s_lruList.AddFirst(node);
                subMeshData.TextureWidth = item.Texture.Width;
                subMeshData.TextureHeight = item.Texture.Height;
                subMeshData.TextureBytes = item.Texture.Pixels;
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
                subMeshData.TextureWidth = item.Texture.Width;
                subMeshData.TextureHeight = item.Texture.Height;
                subMeshData.TextureBytes = item.Texture.Pixels;
            }
        }
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
        Surface[] faces = pmx.SurfaceList.ToArray();
        var mats = pmx.MaterialList.ToArray();
        var texList = pmx.TextureList.ToArray();
        var bones = pmx.BoneList.ToArray();
        var morphs = pmx.MorphList.ToArray();
        var displayFrames = pmx.DisplayFrameList.ToArray();
        var rigidBodies = pmx.RigidBodyList.ToArray();
        var joints = pmx.JointList.ToArray();
        var softBodies = pmx.SoftBodyList.ToArray();

        var childIndices = new List<int>[bones.Length];
        for (int i = 0; i < bones.Length; i++)
            childIndices[i] = new List<int>();
        for (int i = 0; i < bones.Length; i++)
        {
            int parent = bones[i].ParentBone;
            if (parent >= 0)
                childIndices[parent].Add(i);
        }

        // ボーン情報を ModelData に格納する
        var data = new ModelData
        {
            ModelName = pmx.Name,
            ModelNameEnglish = pmx.NameEnglish,
            Comment = pmx.Comment,
            CommentEnglish = pmx.CommentEnglish
        };
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
                NameEnglish = b.NameEnglish ?? string.Empty,
                Parent = b.ParentBone,
                Rotation = System.Numerics.Quaternion.Identity,
                Translation = pos,
                BaseForward = System.Numerics.Vector3.UnitY,
                BaseUp = System.Numerics.Vector3.UnitY
            };
            if (b.IKLinkCount > 0)
            {
                var ik = new IkInfo
                {
                    Target = b.IKTarget,
                    Iterations = b.IterCount,
                    ControlWeight = b.MaxRadianPerIter
                };
                var links = b.IKLinks.ToArray();
                foreach (var link in links)
                {
                    var il = new IkLink
                    {
                        BoneIndex = link.Bone,
                        HasLimit = link.IsEnableAngleLimited,
                        MinAngle = new System.Numerics.Vector3(link.MinLimit.X, link.MinLimit.Y, link.MinLimit.Z),
                        MaxAngle = new System.Numerics.Vector3(link.MaxLimit.X, link.MaxLimit.Y, link.MaxLimit.Z)
                    };
                    ik.Links.Add(il);
                }
                bd.Ik = ik;
            }
            bd.InheritParent = b.AttatchParent;
            bd.InheritRatio = b.AttatchRatio;
            bd.InheritRotation = (b.BoneFlag & BoneFlag.RotationAttach) != 0;
            bd.InheritTranslation = (b.BoneFlag & BoneFlag.TranslationAttach) != 0;
            if ((b.BoneFlag & BoneFlag.FixedAxis) != 0)
            {
                bd.HasFixedAxis = true;
                bd.FixedAxis = new System.Numerics.Vector3(b.AxisVec.X, b.AxisVec.Y, b.AxisVec.Z);
            }
            if ((b.BoneFlag & BoneFlag.LocalAxis) != 0)
            {
                bd.HasLocalAxis = true;
                bd.LocalAxisX = new System.Numerics.Vector3(b.XAxisVec.X, b.XAxisVec.Y, b.XAxisVec.Z);
                bd.LocalAxisZ = new System.Numerics.Vector3(b.ZAxisVec.X, b.ZAxisVec.Y, b.ZAxisVec.Z);
            }
            if ((b.BoneFlag & BoneFlag.ExternalParentTransform) != 0)
                bd.ExternalParent = b.Key;
            boneDatas.Add(bd);
        }

        for (int i = 0; i < boneDatas.Count; i++)
        {
            int parent = boneDatas[i].Parent;
            boneDatas[i].Translation = parent >= 0 ? worldPositions[i] - worldPositions[parent] : worldPositions[i];
        }

        const float Eps = 1e-6f;
        for (int i = 0; i < boneDatas.Count; i++)
        {
            var b = bones[i];
            var bd = boneDatas[i];
            var forward = System.Numerics.Vector3.UnitY;
            if ((b.BoneFlag & BoneFlag.ConnectionDestination) != 0 && b.ConnectedBone >= 0)
            {
                var tail = worldPositions[b.ConnectedBone];
                var diff = tail - worldPositions[i];
                if (diff.LengthSquared() > Eps)
                    forward = System.Numerics.Vector3.Normalize(diff);
            }
            else
            {
                var off = b.PositionOffset;
                if (off.X != 0 || off.Y != 0 || off.Z != 0)
                {
                    var tail = worldPositions[i] + new System.Numerics.Vector3(off.X, off.Y, off.Z) * Scale;
                    var diff = tail - worldPositions[i];
                    if (diff.LengthSquared() > Eps)
                        forward = System.Numerics.Vector3.Normalize(diff);
                }
                else if (childIndices[i].Count > 0)
                {
                    var childPos = worldPositions[childIndices[i][0]];
                    var diff = childPos - worldPositions[i];
                    if (diff.LengthSquared() > Eps)
                        forward = System.Numerics.Vector3.Normalize(diff);
                }
                else if (bd.Parent >= 0)
                {
                    var diff = worldPositions[i] - worldPositions[bd.Parent];
                    if (diff.LengthSquared() > Eps)
                        forward = System.Numerics.Vector3.Normalize(diff);
                }
            }

            var up = System.Numerics.Vector3.UnitY;
            if (bd.Parent >= 0)
            {
                var parentDir = worldPositions[i] - worldPositions[bd.Parent];
                if (parentDir.LengthSquared() > Eps)
                {
                    var upVec = System.Numerics.Vector3.Cross(parentDir, forward);
                    if (upVec.LengthSquared() > Eps)
                        up = System.Numerics.Vector3.Normalize(upVec);
                }
            }

            bd.BaseForward = forward;
            bd.BaseUp = up;
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
        var morphDatas = new List<MorphData>(morphs.Length);
        var nameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int mi = 0; mi < morphs.Length; mi++)
        {
            var m = morphs[mi];
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
                    _logger.LogError("モーフ名 '{Name}' のユニーク化に失敗しました。後続処理で無視されます。", name);
                    continue;
                }
                name = candidate;
            }
            else
            {
                nameCounts[name] = 0;
                if (!usedNames.Add(name))
                {
                    _logger.LogError("モーフ名 '{Name}' のユニーク化に失敗しました。後続処理で無視されます。", name);
                    continue;
                }
            }

            var md = new MorphData { Index = mi, Name = name, NameEnglish = m.NameEnglish ?? string.Empty, Type = m.MorphType, Category = (MorphCategory)m.MorphTarget };
            switch (m.MorphType)
            {
                case MorphType.Vertex:
                    foreach (var elem in m.VertexMorphElements.Span)
                    {
                        md.Offsets.Add(new MorphOffset
                        {
                            Index = elem.TargetVertex,
                            Vertex = new System.Numerics.Vector3(elem.PosOffset.X * Scale, elem.PosOffset.Y * Scale, elem.PosOffset.Z * Scale)
                        });
                    }
                    break;
                case MorphType.Group:
                    foreach (var elem in m.GroupMorphElements.Span)
                    {
                        md.Offsets.Add(new MorphOffset
                        {
                            Index = elem.TargetMorph,
                            Group = new GroupOffset { MorphIndex = elem.TargetMorph, Rate = elem.MorphRatio }
                        });
                    }
                    break;
                case MorphType.Bone:
                    foreach (var elem in m.BoneMorphElements.Span)
                    {
                        md.Offsets.Add(new MorphOffset
                        {
                            Index = elem.TargetBone,
                            Bone = new BoneOffset
                            {
                                Translation = new System.Numerics.Vector3(elem.Translate.X * Scale, elem.Translate.Y * Scale, elem.Translate.Z * Scale),
                                Rotation = new System.Numerics.Quaternion(elem.Quaternion.X, elem.Quaternion.Y, elem.Quaternion.Z, elem.Quaternion.W)
                            }
                        });
                    }
                    break;
                case MorphType.UV:
                    foreach (var elem in m.UVMorphElements.Span)
                    {
                        md.Offsets.Add(new MorphOffset
                        {
                            Index = elem.TargetVertex,
                            Uv = new UvOffset { Offset = new System.Numerics.Vector4(elem.UVOffset.X, elem.UVOffset.Y, elem.UVOffset.Z, elem.UVOffset.W) }
                        });
                    }
                    break;
                case MorphType.AdditionalUV1:
                    foreach (var elem in m.UVMorphElements.Span)
                    {
                        md.Offsets.Add(new MorphOffset
                        {
                            Index = elem.TargetVertex,
                            Uv1 = new UvOffset { Offset = new System.Numerics.Vector4(elem.UVOffset.X, elem.UVOffset.Y, elem.UVOffset.Z, elem.UVOffset.W) }
                        });
                    }
                    break;
                case MorphType.AdditionalUV2:
                    foreach (var elem in m.UVMorphElements.Span)
                    {
                        md.Offsets.Add(new MorphOffset
                        {
                            Index = elem.TargetVertex,
                            Uv2 = new UvOffset { Offset = new System.Numerics.Vector4(elem.UVOffset.X, elem.UVOffset.Y, elem.UVOffset.Z, elem.UVOffset.W) }
                        });
                    }
                    break;
                case MorphType.AdditionalUV3:
                    foreach (var elem in m.UVMorphElements.Span)
                    {
                        md.Offsets.Add(new MorphOffset
                        {
                            Index = elem.TargetVertex,
                            Uv3 = new UvOffset { Offset = new System.Numerics.Vector4(elem.UVOffset.X, elem.UVOffset.Y, elem.UVOffset.Z, elem.UVOffset.W) }
                        });
                    }
                    break;
                case MorphType.AdditionalUV4:
                    foreach (var elem in m.UVMorphElements.Span)
                    {
                        md.Offsets.Add(new MorphOffset
                        {
                            Index = elem.TargetVertex,
                            Uv4 = new UvOffset { Offset = new System.Numerics.Vector4(elem.UVOffset.X, elem.UVOffset.Y, elem.UVOffset.Z, elem.UVOffset.W) }
                        });
                    }
                    break;
                case MorphType.Material:
                    foreach (var elem in m.MaterialMorphElements.Span)
                    {
                        md.Offsets.Add(new MorphOffset
                        {
                            Index = elem.Material,
                            Material = new MaterialOffset
                            {
                                IsAll = elem.IsAllMaterialTarget,
                                CalcMode = (MaterialCalcMode)elem.CalcMode,
                                Diffuse = new System.Numerics.Vector4(elem.Diffuse.R, elem.Diffuse.G, elem.Diffuse.B, elem.Diffuse.A),
                                Specular = new System.Numerics.Vector3(elem.Specular.R, elem.Specular.G, elem.Specular.B),
                                SpecularPower = elem.Shininess,
                                EdgeColor = new System.Numerics.Vector4(elem.EdgeColor.R, elem.EdgeColor.G, elem.EdgeColor.B, elem.EdgeColor.A),
                                EdgeSize = elem.EdgeSize,
                                ToonColor = new System.Numerics.Vector3(elem.ToonTextureCoef.R, elem.ToonTextureCoef.G, elem.ToonTextureCoef.B),
                            TextureTint = new System.Numerics.Vector4(elem.TextureCoef.R, elem.TextureCoef.G, elem.TextureCoef.B, elem.TextureCoef.A)
                            }
                        });
                    }
                    break;
                case MorphType.Flip:
                    foreach (var elem in m.FlipMorphElements.Span)
                    {
                        md.Offsets.Add(new MorphOffset
                        {
                            Index = elem.TargetMorph,
                            Flip = new FlipOffset { MorphIndex = elem.TargetMorph, Rate = elem.MorphRatio }
                        });
                    }
                    break;
                case MorphType.Impulse:
                    foreach (var elem in m.ImpulseMorphElements.Span)
                    {
                        md.Offsets.Add(new MorphOffset
                        {
                            Index = elem.TargetRigidBody,
                            Impulse = new ImpulseOffset
                            {
                                RigidBodyIndex = elem.TargetRigidBody,
                                IsLocal = elem.IsLocal,
                                Velocity = ScaleVector(elem.Velocity),
                                Torque = new System.Numerics.Vector3(elem.RotationTorque.X, elem.RotationTorque.Y, elem.RotationTorque.Z)
                            }
                        });
                    }
                    break;
                default:
                    break;
            }
            morphDatas.Add(md);
        }
        data.Morphs = morphDatas;

        var displayFrameDatas = new List<DisplayFrameData>(displayFrames.Length);
        for (int i = 0; i < displayFrames.Length; i++)
        {
            var f = displayFrames[i];
            var dfd = new DisplayFrameData
            {
                Name = string.IsNullOrEmpty(f.NameEnglish) ? f.Name : f.NameEnglish,
                NameEnglish = f.NameEnglish ?? string.Empty,
                Type = f.Type
            };
            foreach (var elem in f.Elements.Span)
            {
                dfd.Elements.Add(new DisplayFrameElement
                {
                    TargetType = elem.TargetType,
                    TargetIndex = elem.TargetIndex
                });
            }
            displayFrameDatas.Add(dfd);
        }
        data.DisplayFrames = displayFrameDatas;

        var rigidBodyDatas = new List<RigidBodyData>(rigidBodies.Length);
        for (int i = 0; i < rigidBodies.Length; i++)
        {
            var rb = rigidBodies[i];
            var rbType = rb.GetType();
            var rotProp = rbType.GetProperty("RotationRadian") ?? rbType.GetProperty("Rotation");
            System.Numerics.Vector3 rotation = System.Numerics.Vector3.Zero;
            if (rotProp?.GetValue(rb) is { } rot)
            {
                dynamic v = rot;
                rotation = new System.Numerics.Vector3(v.X, v.Y, v.Z);
            }
            var rbd = new RigidBodyData
            {
                Name = string.IsNullOrEmpty(rb.NameEnglish) ? rb.Name : rb.NameEnglish,
                NameEnglish = rb.NameEnglish ?? string.Empty,
                BoneIndex = rb.Bone,
                Mass = rb.Mass,
                Shape = (RigidBodyShape)rb.Shape,
                LinearDamping = rb.TranslationAttenuation,
                AngularDamping = rb.RotationAttenuation,
                Restitution = rb.Recoil,
                Friction = rb.Friction,
                Position = ScaleVector(rb.Position),
                Rotation = rotation,
                Size = ScaleVector(rb.Size),
                Group = rb.Group,
                Mask = rb.GroupTarget,
                Mode = (int)rb.PhysicsType
            };
            rigidBodyDatas.Add(rbd);
        }
        data.RigidBodies = rigidBodyDatas;

        var jointDatas = new List<JointData>(joints.Length);
        for (int i = 0; i < joints.Length; i++)
        {
            var j = joints[i];
            var type = j.GetType();
            int rbA = type.GetProperty("RigidBodyA")?.GetValue(j) is object a ? Convert.ToInt32(a) : -1;
            if (rbA < 0) rbA = type.GetProperty("RigidBody1")?.GetValue(j) is object a1 ? Convert.ToInt32(a1) : -1;
            int rbB = type.GetProperty("RigidBodyB")?.GetValue(j) is object b ? Convert.ToInt32(b) : -1;
            if (rbB < 0) rbB = type.GetProperty("RigidBody2")?.GetValue(j) is object b1 ? Convert.ToInt32(b1) : -1;
            string name = string.IsNullOrEmpty(j.NameEnglish) ? j.Name : j.NameEnglish;
            var jd = new JointData { Name = name, NameEnglish = j.NameEnglish ?? string.Empty, RigidBodyA = rbA, RigidBodyB = rbB };
            if (type.GetProperty("Position")?.GetValue(j) is object pos)
                jd.Position = ScaleVector((dynamic)pos);
            if (type.GetProperty("Rotation")?.GetValue(j) is object rot)
            {
                dynamic v = rot;
                jd.Rotation = new System.Numerics.Vector3(v.X, v.Y, v.Z);
            }
            if (type.GetProperty("TranslationMinLimit")?.GetValue(j) is object tmin)
                jd.PositionMin = ScaleVector((dynamic)tmin);
            if (type.GetProperty("TranslationMaxLimit")?.GetValue(j) is object tmax)
                jd.PositionMax = ScaleVector((dynamic)tmax);
            if (type.GetProperty("RotationRadianMinLimit")?.GetValue(j) is object rmin)
            {
                dynamic v = rmin;
                jd.RotationMin = new System.Numerics.Vector3(v.X, v.Y, v.Z);
            }
            if (type.GetProperty("RotationRadianMaxLimit")?.GetValue(j) is object rmax)
            {
                dynamic v = rmax;
                jd.RotationMax = new System.Numerics.Vector3(v.X, v.Y, v.Z);
            }
            if (type.GetProperty("TranslationSpring")?.GetValue(j) is object ts)
                jd.SpringPosition = ScaleVector((dynamic)ts);
            if (type.GetProperty("RotationSpring")?.GetValue(j) is object rs)
            {
                dynamic v = rs;
                jd.SpringRotation = new System.Numerics.Vector3(v.X, v.Y, v.Z);
            }
            jointDatas.Add(jd);
        }
        data.Joints = jointDatas;

        var softBodyDatas = new List<SoftBodyData>(softBodies.Length);
        for (int i = 0; i < softBodies.Length; i++)
        {
            var sb = softBodies[i];
            var sbd = new SoftBodyData
            {
                Name = string.IsNullOrEmpty(sb.NameEnglish) ? sb.Name : sb.NameEnglish,
                NameEnglish = sb.NameEnglish ?? string.Empty
            };
            var sbType = sb.GetType();
            if (sbType.GetProperty("Material")?.GetValue(sb) is object mat)
                sbd.MaterialIndex = Convert.ToInt32(mat);
            if (sbType.GetProperty("Shape")?.GetValue(sb) is object shape)
                sbd.Shape = (SoftBodyShape)Convert.ToInt32(shape);
            if (sbType.GetProperty("Group")?.GetValue(sb) is object group)
                sbd.Group = Convert.ToByte(group);
            if (sbType.GetProperty("GroupTarget")?.GetValue(sb) is object mask)
                sbd.Mask = Convert.ToUInt16(mask);
            softBodyDatas.Add(sbd);
        }
        data.SoftBodies = softBodyDatas;

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
        GenerateSubMeshes(data, verts, faces, mats, texList, textureDir);
        data.Transform = System.Numerics.Matrix4x4.CreateScale(Scale);
        return data;
    }
    // 現在は PMX モデルのみに対応しています
}
