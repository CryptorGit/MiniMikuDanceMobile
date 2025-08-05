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
    public List<(string Name, float Weight)> GroupChildren { get; set; } = new();
}

public class MorphOffset
{
    public int Index { get; set; }
    public System.Numerics.Vector3 Offset { get; set; }
}

public class ModelImporter
{
    private readonly AssimpContext _context = new();
    private readonly PmxLoader _pmxLoader = new();
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
        var pmxFile = _pmxLoader.Load(stream);
        var pmx = pmxFile.Model;
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
                Translation = localPos,
                TwistWeight = (name.Contains("arm", StringComparison.OrdinalIgnoreCase) || name.Contains("leg", StringComparison.OrdinalIgnoreCase)) ? 0.5f : 0f
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

        // モデルの向きを推定して Z+ 前方に正規化する
        System.Numerics.Matrix4x4 rotation = System.Numerics.Matrix4x4.Identity;

        int FindBone(params string[][] keywords)
        {
            for (int i = 0; i < bones.Length; i++)
            {
                string name = string.IsNullOrEmpty(bones[i].NameEnglish) ? bones[i].Name : bones[i].NameEnglish;
                foreach (var set in keywords)
                {
                    bool match = true;
                    foreach (var kw in set)
                    {
                        if (!name.Contains(kw, StringComparison.OrdinalIgnoreCase))
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match)
                        return i;
                }
            }
            return -1;
        }

        int head = FindBone(new[] { "頭" }, new[] { "首" }, new[] { "head" }, new[] { "neck" });
        int left = FindBone(new[] { "左", "肩" }, new[] { "左", "腕" }, new[] { "left", "shoulder" }, new[] { "left", "arm" });
        int right = FindBone(new[] { "右", "肩" }, new[] { "右", "腕" }, new[] { "right", "shoulder" }, new[] { "right", "arm" });
        if (head >= 0 && left >= 0 && right >= 0)
        {
            var center = (absPositions[left] + absPositions[right]) * 0.5f;
            var up = System.Numerics.Vector3.Normalize(absPositions[head] - center);
            var rightDir = System.Numerics.Vector3.Normalize(absPositions[right] - absPositions[left]);
            var forward = System.Numerics.Vector3.Normalize(System.Numerics.Vector3.Cross(rightDir, up));
            if (forward.Z < 0)
            {
                rotation = System.Numerics.Matrix4x4.CreateRotationY((float)Math.PI);
            }
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
        var morphDatas = new List<MorphData>();
        foreach (var m in morphs)
        {
            if (m.MorphType != MorphType.Vertex && m.MorphType != MorphType.Group)
                continue;

            string name = string.IsNullOrEmpty(m.NameEnglish) ? m.Name : m.NameEnglish;
            name = name.Trim();

            // 同名モーフが存在する場合は日本語名やインデックスで一意化する
            string uniqueName = name;
            if (morphDatas.Any(md => md.Name.Equals(uniqueName, StringComparison.OrdinalIgnoreCase)))
            {
                string jp = m.Name.Trim();
                if (!string.IsNullOrEmpty(jp) && !jp.Equals(uniqueName, StringComparison.OrdinalIgnoreCase))
                {
                    uniqueName = $"{uniqueName}_{jp}";
                }
                else
                {
                    uniqueName = $"{uniqueName}_{morphDatas.Count}";
                }

                int idx = 1;
                while (morphDatas.Any(md => md.Name.Equals(uniqueName, StringComparison.OrdinalIgnoreCase)))
                {
                    uniqueName = $"{name}_{morphDatas.Count + idx}";
                    idx++;
                }
            }

            var md = new MorphData { Name = uniqueName, Type = m.MorphType };

            if (m.MorphType == MorphType.Vertex)
            {
                foreach (var vm in m.VertexMorphElements.ToArray())
                {
                    md.Offsets.Add(new MorphOffset
                    {
                        Index = vm.TargetVertex,
                        Offset = new System.Numerics.Vector3(vm.PosOffset.X, vm.PosOffset.Y, vm.PosOffset.Z) * Scale
                    });
                }
            }
            else if (m.MorphType == MorphType.Group)
            {
                foreach (var gm in m.GroupMorphElements.ToArray())
                {
                    if (gm.TargetMorph >= 0 && gm.TargetMorph < morphs.Length)
                    {
                        var child = morphs[gm.TargetMorph];
                        string childName = string.IsNullOrEmpty(child.NameEnglish) ? child.Name : child.NameEnglish;
                        childName = childName.Trim();
                        md.GroupChildren.Add((childName, gm.MorphRatio));
                    }
                }
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

        // 11点IKボーンの追加
        int GetIndex(string n) => data.HumanoidBones.TryGetValue(n, out var idx) ? idx : -1;
        var ikDefs = new (string Name, int Target, int[] Chain)[]
        {
            ("ik_head", GetIndex("head"), new[]{ GetIndex("neck"), GetIndex("chest"), GetIndex("spine"), GetIndex("hips") }),
            ("ik_left_wrist", GetIndex("leftHand"), new[]{ GetIndex("leftLowerArm"), GetIndex("leftUpperArm") }),
            ("ik_right_wrist", GetIndex("rightHand"), new[]{ GetIndex("rightLowerArm"), GetIndex("rightUpperArm") }),
            ("ik_left_elbow", GetIndex("leftLowerArm"), new[]{ GetIndex("leftUpperArm") }),
            ("ik_right_elbow", GetIndex("rightLowerArm"), new[]{ GetIndex("rightUpperArm") }),
            ("ik_left_knee", GetIndex("leftLowerLeg"), new[]{ GetIndex("leftUpperLeg") }),
            ("ik_right_knee", GetIndex("rightLowerLeg"), new[]{ GetIndex("rightUpperLeg") }),
            ("ik_left_foot", GetIndex("leftFoot"), new[]{ GetIndex("leftLowerLeg"), GetIndex("leftUpperLeg") }),
            ("ik_right_foot", GetIndex("rightFoot"), new[]{ GetIndex("rightLowerLeg"), GetIndex("rightUpperLeg") }),
            ("ik_chest", GetIndex("chest"), new[]{ GetIndex("spine"), GetIndex("hips") }),
            ("ik_hip", GetIndex("hips"), Array.Empty<int>())
        };

        foreach (var (name, target, chain) in ikDefs)
        {
            if (target < 0) continue;

            // 既存IKボーンがターゲットを参照しているか確認
            int existingIndex = boneDatas.FindIndex(b => b.IsIk && b.IkTargetIndex == target);
            if (existingIndex >= 0)
            {
                if (!data.IkBoneIndices.Contains(existingIndex))
                    data.IkBoneIndices.Add(existingIndex);
                continue;
            }

            var validChain = chain.Where(c => c >= 0).ToList();
            var ik = new BoneData
            {
                Name = name,
                Parent = -1,
                IsIk = true,
                IkTargetIndex = target,
                IkChainIndices = validChain,
                Rotation = System.Numerics.Quaternion.Identity,
                Translation = boneDatas[target].BindMatrix.Translation,
                TwistWeight = 0f
            };
            ik.BindMatrix = System.Numerics.Matrix4x4.CreateTranslation(ik.Translation);
            System.Numerics.Matrix4x4.Invert(ik.BindMatrix, out var invIk);
            ik.InverseBindMatrix = invIk;
            data.IkBoneIndices.Add(boneDatas.Count);
            boneDatas.Add(ik);
        }
        data.Bones = boneDatas;
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

            smd.SphereMode = (SphereMapMode)mat.SphereTextureMode;

            // ライブラリによってはプロパティ名が "SphereTexture" と "SphereTextre" で異なるため、両方に対応する
            int sphereIndex = -1;
            var sphereProp = mat.GetType().GetProperty("SphereTexture") ??
                            mat.GetType().GetProperty("SphereTextre");
            if (sphereProp?.GetValue(mat) is int idx)
                sphereIndex = idx;

            if (!string.IsNullOrEmpty(dir) && sphereIndex >= 0 && sphereIndex < texList.Length)
            {
                var sphereName = texList[sphereIndex]
                    .Replace('\\', Path.DirectorySeparatorChar);
                var spherePath = Path.Combine(dir, sphereName);
                smd.SphereTextureFilePath = sphereName;
                if (File.Exists(spherePath))
                {
                    using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(spherePath);
                    smd.SphereTextureWidth = image.Width;
                    smd.SphereTextureHeight = image.Height;
                    smd.SphereTextureBytes = new byte[image.Width * image.Height * 4];
                    image.CopyPixelDataTo(smd.SphereTextureBytes);
                }
            }

            if (!string.IsNullOrEmpty(dir))
            {
                string? toonPath = null;
                if (mat.SharedToonMode == SharedToonMode.SharedToon)
                {
                    int toonIndex = mat.ToonTexture;
                    var toonName = $"toon{toonIndex + 1:00}.bmp";
                    var relPath = Path.Combine("toon", toonName);
                    toonPath = Path.Combine(dir, relPath);
                    smd.ToonTextureFilePath = relPath;
                }
                else if (mat.ToonTexture >= 0 && mat.ToonTexture < texList.Length)
                {
                    var toonName = texList[mat.ToonTexture]
                        .Replace('\\', Path.DirectorySeparatorChar);
                    toonPath = Path.Combine(dir, toonName);
                    smd.ToonTextureFilePath = toonName;
                }
                if (toonPath != null && File.Exists(toonPath))
                {
                    using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(toonPath);
                    smd.ToonTextureWidth = image.Width;
                    smd.ToonTextureHeight = image.Height;
                    smd.ToonTextureBytes = new byte[image.Width * image.Height * 4];
                    image.CopyPixelDataTo(smd.ToonTextureBytes);
                }
            }

            data.SubMeshes.Add(smd);
            faceOffset += faceCount;
        }
        data.Transform = rotation * System.Numerics.Matrix4x4.CreateScale(Scale);
        return data;
    }

    // 現在は PMX モデルのみに対応しています
}
