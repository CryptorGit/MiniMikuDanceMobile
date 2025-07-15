using Assimp;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Numerics;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Vector3D = Assimp.Vector3D;
using SharpGLTF.Schema2;
using SNode = SharpGLTF.Schema2.Node;
using ViewerApp;
using MiniMikuDance.Util;

namespace MiniMikuDance.Import;

public class ModelData
{
    public List<SubMeshData> SubMeshes { get; } = new();
    public Assimp.Mesh Mesh { get; set; } = null!;
    public System.Numerics.Matrix4x4 Transform { get; set; } = System.Numerics.Matrix4x4.Identity;
    public List<BoneData> Bones { get; } = new();
    public Dictionary<string, int> HumanoidBones { get; } = new();
    public List<(string Name, int Index)> HumanoidBoneList { get; } = new();
    public List<MiniMikuDance.IK.IkChain> IkChains { get; } = new();
    public float ShadeShift { get; set; } = -0.1f;
    public float ShadeToony { get; set; } = 0.9f;
    public float RimIntensity { get; set; } = 0.5f;
    public VrmInfo Info { get; set; } = new();
}

public class ModelImporter
{
    private readonly AssimpContext _context = new();
    private static readonly System.Numerics.Matrix4x4 FlipZ = System.Numerics.Matrix4x4.CreateScale(1f, 1f, -1f);

    private static string NormalizeLimitName(string name)
    {
        if (name.StartsWith("left"))
        {
            var rest = name.Substring(4);
            rest = char.ToLowerInvariant(rest[0]) + rest.Substring(1);
            return rest + ".L";
        }
        if (name.StartsWith("right"))
        {
            var rest = name.Substring(5);
            rest = char.ToLowerInvariant(rest[0]) + rest.Substring(1);
            return rest + ".R";
        }
        return name;
    }

    private static readonly Dictionary<string,
        ((float Min, float Max) X, (float Min, float Max) Y, (float Min, float Max) Z)> DefaultRotationRanges;

    static ModelImporter()
    {
        try
        {
            var config = Util.JSONUtil.Load<JointLimitConfig>("Configs/JointLimits.json");
            DefaultRotationRanges = config.Limits.ToDictionary(
                kv => kv.Key,
                kv => ((kv.Value.X.Min, kv.Value.X.Max),
                        (kv.Value.Y.Min, kv.Value.Y.Max),
                        (kv.Value.Z.Min, kv.Value.Z.Max)));
        }
        catch
        {
            DefaultRotationRanges = new();
        }
    }

    public ModelData ImportModel(Stream stream)
    {

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var bytes = ms.ToArray();
        ms.Position = 0;
        var textureMap = VrmUtil.ReadMainTextureIndices(bytes);
        var humanMap = ReadHumanoidMap(bytes);
        var mtoon = ReadMToonParameters(bytes);
        var info = ReadVrmInfo(bytes);
        var model = SharpGLTF.Schema2.ModelRoot.ReadGLB(ms);
        var data = ImportVrm(model, textureMap, humanMap, mtoon);
        data.Info = info;
        return data;
    }

    public ModelData ImportModel(string path)
    {


        if (!File.Exists(path))
        {

            throw new FileNotFoundException("Model file not found", path);
        }

        string ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext == ".vrm" || ext == ".gltf" || ext == ".glb")
        {
            return ImportVrm(path);
        }

        var scene = _context.ImportFile(path, PostProcessSteps.Triangulate | PostProcessSteps.GenerateNormals);

        return new ModelData { Mesh = scene.Meshes[0] };
    }

    private ModelData ImportVrm(string path)
    {

        var bytes = File.ReadAllBytes(path);
        using var ms = new MemoryStream(bytes);
        var info = ReadVrmInfo(bytes);
        var textureMap = VrmUtil.ReadMainTextureIndices(bytes);
        var humanMap = ReadHumanoidMap(bytes);
        var mtoon = ReadMToonParameters(bytes);
        var model = SharpGLTF.Schema2.ModelRoot.ReadGLB(ms);
        var data = ImportVrm(model, textureMap, humanMap, mtoon);
        data.Info = info;
        return data;
    }

    private ModelData ImportVrm(SharpGLTF.Schema2.ModelRoot model, Dictionary<int, int> texMap, Dictionary<string, int> humanMap, (float shadeShift, float shadeToony, float rimIntensity) mtoon)
    {
        var combined = new Assimp.Mesh("mesh", Assimp.PrimitiveType.Triangle);
        int combinedIndexOffset = 0;
        var data = new ModelData();

        foreach (var node in model.LogicalNodes)
        {
            if (node.Mesh == null) continue;
            var skin = node.Skin;
            int[] jointMap = Array.Empty<int>();
            if (skin != null)
            {
                jointMap = new int[skin.JointsCount];
                for (int j = 0; j < jointMap.Length; j++) jointMap[j] = skin.GetJoint(j).Joint.LogicalIndex;
            }

            foreach (var prim in node.Mesh.Primitives)
            {
                var sub = new Assimp.Mesh("mesh", Assimp.PrimitiveType.Triangle);
                var subUvs = new List<System.Numerics.Vector2>();
                var subJoints = new List<System.Numerics.Vector4>();
                var subWeights = new List<System.Numerics.Vector4>();
                var positions = prim.GetVertexAccessor("POSITION").AsVector3Array();
                var normals = prim.GetVertexAccessor("NORMAL")?.AsVector3Array();
                var uvs = prim.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();
                var joints = prim.GetVertexAccessor("JOINTS_0")?.AsVector4Array();
                var weights = prim.GetVertexAccessor("WEIGHTS_0")?.AsVector4Array();
                var channel = prim.Material?.FindChannel("BaseColor");
                int matIndex = prim.Material?.LogicalIndex ?? -1;

                for (int i = 0; i < positions.Count; i++)
                {
                    var v = positions[i];
                    // Z 軸を反転
                    var pos = new Vector3D(v.X, v.Y, -v.Z);
                    sub.Vertices.Add(pos);
                    combined.Vertices.Add(pos);

                    if (normals != null && i < normals.Count)
                    {
                        var n = new Vector3D(normals[i].X, normals[i].Y, -normals[i].Z);
                        sub.Normals.Add(n);
                        combined.Normals.Add(n);
                    }
                    else
                    {
                        sub.Normals.Add(new Vector3D(0, 0, 1));
                        combined.Normals.Add(new Vector3D(0, 0, 1));
                    }
                    if (uvs != null && i < uvs.Count)
                    {
                        var uv = uvs[i];
                        sub.TextureCoordinateChannels[0].Add(new Vector3D(uv.X, uv.Y, 0));
                        combined.TextureCoordinateChannels[0].Add(new Vector3D(uv.X, uv.Y, 0));
                        subUvs.Add(new System.Numerics.Vector2(uv.X, uv.Y));
                    }
                    else
                    {
                        sub.TextureCoordinateChannels[0].Add(new Vector3D(0, 0, 0));
                        combined.TextureCoordinateChannels[0].Add(new Vector3D(0, 0, 0));
                        subUvs.Add(System.Numerics.Vector2.Zero);
                    }

                    if (joints != null && i < joints.Count)
                    {
                        var j = joints[i];
                        System.Numerics.Vector4 idx = System.Numerics.Vector4.Zero;
                        if (jointMap.Length > 0)
                        {
                            idx.X = j.X < jointMap.Length ? jointMap[(int)j.X] : 0;
                            idx.Y = j.Y < jointMap.Length ? jointMap[(int)j.Y] : 0;
                            idx.Z = j.Z < jointMap.Length ? jointMap[(int)j.Z] : 0;
                            idx.W = j.W < jointMap.Length ? jointMap[(int)j.W] : 0;
                        }
                        subJoints.Add(idx);
                    }
                    else
                    {
                        subJoints.Add(System.Numerics.Vector4.Zero);
                    }
                    if (weights != null && i < weights.Count)
                    {
                        var w = weights[i];
                        subWeights.Add(new System.Numerics.Vector4(w.X, w.Y, w.Z, w.W));
                    }
                    else
                    {
                        subWeights.Add(System.Numerics.Vector4.Zero);
                    }

                }

                var indices = prim.IndexAccessor.AsIndicesArray();
                for (int i = 0; i < indices.Count; i += 3)
                {
                    // Z 軸反転に合わせてインデックス順序を逆転
                    var face = new Face();
                    face.Indices.Add((int)indices[i]);
                    face.Indices.Add((int)indices[i + 2]);
                    face.Indices.Add((int)indices[i + 1]);
                    sub.Faces.Add(face);

                    var cFace = new Face();
                    cFace.Indices.Add((int)indices[i] + combinedIndexOffset);
                    cFace.Indices.Add((int)indices[i + 2] + combinedIndexOffset);
                    cFace.Indices.Add((int)indices[i + 1] + combinedIndexOffset);
                    combined.Faces.Add(cFace);
                }

                combinedIndexOffset += positions.Count;

                var material = prim.Material;
                var colorParam = channel?.Parameter ?? Vector4.One;
                // VRM の alpha 値は利用せず常に不透明で描画
                colorParam.W = 1.0f;
                var colorFactor = colorParam;

                var smd = new SubMeshData
                {
                    Mesh = sub,
                    ColorFactor = colorFactor
                };
                smd.TexCoords.AddRange(subUvs);
                smd.JointIndices.AddRange(subJoints);
                smd.JointWeights.AddRange(subWeights);

                if (matIndex >= 0 && texMap.TryGetValue(matIndex, out var texIdx))
                {
                    var tex = model.LogicalTextures[texIdx];
                    var imgSeg = tex.PrimaryImage?.GetImageContent();
                    if (imgSeg.HasValue)
                    {
                        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(imgSeg.Value);

                        int maxWidth = 2048;
                        int maxHeight = 2048;
                        if (image.Width > maxWidth || image.Height > maxHeight)
                        {
                            image.Mutate(x => x.Resize(new ResizeOptions
                            {
                                Size = new SixLabors.ImageSharp.Size(maxWidth, maxHeight),
                                Mode = ResizeMode.Max
                            }));

                        }
                        smd.TextureWidth = image.Width;
                        smd.TextureHeight = image.Height;
                        smd.TextureBytes = new byte[image.Width * image.Height * 4];
                        image.CopyPixelDataTo(smd.TextureBytes);
                    }
                }

                data.SubMeshes.Add(smd);
            }
        }

        var transform = System.Numerics.Matrix4x4.Identity;
        var rootNode = model.DefaultScene?.VisualChildren.FirstOrDefault();
        if (rootNode != null)
        {
            transform = rootNode.WorldMatrix;
            var flip = System.Numerics.Matrix4x4.CreateScale(1f, 1f, -1f);
            transform = flip * transform * flip;
        }



        data.Mesh = combined;
        data.Transform = transform;

        // Humanoid ボーン情報を読み込み
        data.Bones.Clear();
        data.HumanoidBones.Clear();
        data.HumanoidBoneList.Clear();
        data.Bones.AddRange(ReadBones(model));
        foreach (var skin in model.LogicalSkins)
        {
            var acc = skin.GetInverseBindMatricesAccessor();
            if (acc == null) continue;
            var invs = acc.AsMatrix4x4Array();

            int jointCount = skin.JointsCount;
            for (int i = 0; i < jointCount && i < invs.Count; i++)
            {
                var jnode = skin.GetJoint(i).Joint;
                int bi = jnode.LogicalIndex;
                if (bi >= 0 && bi < data.Bones.Count)
                {
                    data.Bones[bi].InverseBindMatrix = invs[i];
                    System.Numerics.Matrix4x4.Invert(invs[i], out var bind);
                    data.Bones[bi].BindMatrix = bind;
                }
            }
        }
        foreach (var kv in humanMap)
        {
            data.HumanoidBones[kv.Key] = kv.Value;
            data.HumanoidBoneList.Add((kv.Key, kv.Value));
            var limitName = NormalizeLimitName(kv.Key);
            if (DefaultRotationRanges.TryGetValue(limitName, out var range))
            {
                int idx = kv.Value;
                if (idx >= 0 && idx < data.Bones.Count)
                {
                    data.Bones[idx].RotationXRange = range.X;
                    data.Bones[idx].RotationYRange = range.Y;
                    data.Bones[idx].RotationZRange = range.Z;
                }
            }
        }

        data.IkChains.Clear();
        foreach (var hb in data.HumanoidBoneList)
        {
            var chainIndices = new List<int>();
            int i = hb.Index;
            while (i >= 0 && i < data.Bones.Count)
            {
                chainIndices.Insert(0, i);
                i = data.Bones[i].Parent;
            }
            var chain = new MiniMikuDance.IK.IkChain { EndBoneName = hb.Name };
            chain.Indices.AddRange(chainIndices);
            data.IkChains.Add(chain);
        }

        data.ShadeShift = mtoon.shadeShift;
        data.ShadeToony = mtoon.shadeToony;
        data.RimIntensity = mtoon.rimIntensity;
        return data;
    }




    private static Dictionary<string, int> ReadHumanoidMap(byte[] glb)
    {
        var map = new Dictionary<string, int>();
        try
        {
            using var ms = new MemoryStream(glb);
            using var br = new BinaryReader(ms);
            if (br.ReadUInt32() != 0x46546C67) return map; // magic 'glTF'
            br.ReadUInt32(); // version
            br.ReadUInt32(); // length
            uint jsonLen = br.ReadUInt32();
            if (br.ReadUInt32() != 0x4E4F534A) return map; // JSON
            var jsonBytes = br.ReadBytes((int)jsonLen);
            var doc = System.Text.Json.JsonDocument.Parse(jsonBytes);
            var root = doc.RootElement;
            if (root.TryGetProperty("extensions", out var exts))
            {
                if (exts.TryGetProperty("VRM", out var vrm) &&
                    vrm.TryGetProperty("humanoid", out var humanoid) &&
                    humanoid.TryGetProperty("humanBones", out var bones) &&
                    bones.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var b in bones.EnumerateArray())
                    {
                        if (b.TryGetProperty("bone", out var boneEl) &&
                            b.TryGetProperty("node", out var nodeEl))
                        {
                            var bn = boneEl.GetString();
                            if (bn != null) map[bn] = nodeEl.GetInt32();
                        }
                    }
                }
                if (exts.TryGetProperty("VRMC_humanoid", out var h1) &&
                    h1.TryGetProperty("humanBones", out var hb) &&
                    hb.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    foreach (var prop in hb.EnumerateObject())
                    {
                        var obj = prop.Value;
                        if (obj.TryGetProperty("node", out var nodeEl))
                        {
                            map[prop.Name] = nodeEl.GetInt32();
                        }
                    }
                }
            }
        }
        catch
        {
        }
        return map;
    }

    private static List<BoneData> ReadBones(SharpGLTF.Schema2.ModelRoot model)
    {
        var list = new List<BoneData>();
        for (int i = 0; i < model.LogicalNodes.Count; i++)
        {
            var node = model.LogicalNodes[i];

            var trans = node.LocalTransform.Translation;
            trans.Z = -trans.Z;

            var rot = node.LocalTransform.Rotation;
            var m = System.Numerics.Matrix4x4.CreateFromQuaternion(rot);
            m = FlipZ * m * FlipZ;
            System.Numerics.Matrix4x4.Decompose(m, out var _, out var rotF, out var _);

            var bind = FlipZ * node.WorldMatrix * FlipZ;
            System.Numerics.Matrix4x4.Invert(bind, out var inv);

            var bd = new BoneData
            {
                Name = node.Name ?? $"node{i}",
                Parent = node.VisualParent?.LogicalIndex ?? -1,
                Rotation = rotF,
                Translation = trans,
                BindMatrix = bind,
                InverseBindMatrix = inv,
            };
            list.Add(bd);
        }
        return list;
    }

    private static (float shadeShift, float shadeToony, float rimIntensity) ReadMToonParameters(byte[] glb)
    {
        float shadeShift = -0.1f;
        float shadeToony = 0.9f;
        float rimIntensity = 0.5f;
        try
        {
            using var ms = new MemoryStream(glb);
            using var br = new BinaryReader(ms);
            if (br.ReadUInt32() != 0x46546C67) return (shadeShift, shadeToony, rimIntensity);
            br.ReadUInt32();
            br.ReadUInt32();
            uint jsonLen = br.ReadUInt32();
            if (br.ReadUInt32() != 0x4E4F534A) return (shadeShift, shadeToony, rimIntensity);
            var jsonBytes = br.ReadBytes((int)jsonLen);
            var doc = System.Text.Json.JsonDocument.Parse(jsonBytes);
            var root = doc.RootElement;
            if (root.TryGetProperty("extensions", out var exts) &&
                exts.TryGetProperty("VRM", out var vrmExt) &&
                vrmExt.TryGetProperty("materialProperties", out var matProps) &&
                matProps.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var m in matProps.EnumerateArray())
                {
                    if (m.TryGetProperty("floatProperties", out var floats))
                    {
                        if (floats.TryGetProperty("_ShadeShift", out var ss)) shadeShift = (float)ss.GetDouble();
                        if (floats.TryGetProperty("_ShadeToony", out var st)) shadeToony = (float)st.GetDouble();
                        if (floats.TryGetProperty("_RimIntensity", out var ri)) rimIntensity = (float)ri.GetDouble();
                        break;
                    }
                }
            }

            if (root.TryGetProperty("materials", out var mats))
            {
                foreach (var mat in mats.EnumerateArray())
                {
                    if (mat.TryGetProperty("extensions", out var mext) &&
                        mext.TryGetProperty("VRMC_materials_mtoon", out var mtoon))
                    {
                        if (mtoon.TryGetProperty("shadingShiftFactor", out var ss)) shadeShift = (float)ss.GetDouble();
                        if (mtoon.TryGetProperty("shadingToonyFactor", out var st)) shadeToony = (float)st.GetDouble();
                        if (mtoon.TryGetProperty("rimIntensityFactor", out var ri)) rimIntensity = (float)ri.GetDouble();
                        break;
                    }
                }
            }
        }
        catch
        {
        }
        return (shadeShift, shadeToony, rimIntensity);
    }

    private static VrmInfo ReadVrmInfo(byte[] glb)
    {
        var info = new VrmInfo();
        try
        {
            using var ms = new MemoryStream(glb);
            using var br = new BinaryReader(ms);
            if (br.ReadUInt32() != 0x46546C67) return info; // magic 'glTF'
            br.ReadUInt32(); // version
            br.ReadUInt32(); // length
            uint jsonLen = br.ReadUInt32();
            if (br.ReadUInt32() != 0x4E4F534A) return info; // JSON
            var jsonBytes = br.ReadBytes((int)jsonLen);
            var doc = System.Text.Json.JsonDocument.Parse(jsonBytes);
            var root = doc.RootElement;

            info.SpecVersion = "glTF";
            if (root.TryGetProperty("extensionsUsed", out var used) &&
                used.ValueKind == System.Text.Json.JsonValueKind.Array &&
                used.EnumerateArray().Any(e => e.GetString() == "VRM"))
            {
                info.SpecVersion = "VRM 0.x";
            }
            if (root.TryGetProperty("extensions", out var exts) &&
                exts.TryGetProperty("VRMC_vrm", out _))
            {
                info.SpecVersion = "VRM 1.0";
            }

            System.Text.Json.JsonElement meta;
            if (root.TryGetProperty("extensions", out var exts2))
            {
                if (exts2.TryGetProperty("VRM", out var vrm) &&
                    vrm.TryGetProperty("meta", out var m))
                {
                    meta = m;
                }
                else if (exts2.TryGetProperty("VRMC_vrm", out var vrm1) &&
                         vrm1.TryGetProperty("meta", out var m1))
                {
                    meta = m1;
                }
                else
                {
                    meta = default;
                }
            }
            else
            {
                meta = default;
            }

            if (meta.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                if (meta.TryGetProperty("title", out var t)) info.Title = t.GetString() ?? string.Empty;
                if (meta.TryGetProperty("author", out var a)) info.Author = a.GetString() ?? string.Empty;
                if (meta.TryGetProperty("commercialUssageName", out var cu)) info.License = cu.GetString() ?? string.Empty;
                else if (meta.TryGetProperty("licenseName", out var ln)) info.License = ln.GetString() ?? string.Empty;
            }

            if (root.TryGetProperty("nodes", out var nodes)) info.NodeCount = nodes.GetArrayLength();
            if (root.TryGetProperty("meshes", out var meshes)) info.MeshCount = meshes.GetArrayLength();
            if (root.TryGetProperty("skins", out var skins)) info.SkinCount = skins.GetArrayLength();
            if (root.TryGetProperty("materials", out var mats)) info.MaterialCount = mats.GetArrayLength();
            if (root.TryGetProperty("images", out var imgs)) info.TextureCount = imgs.GetArrayLength();

            var model = SharpGLTF.Schema2.ModelRoot.ReadGLB(new MemoryStream(glb));
            int vtx = 0;
            int tri = 0;
            foreach (var mesh in model.LogicalMeshes)
            {
                foreach (var prim in mesh.Primitives)
                {
                    vtx += prim.GetVertexAccessor("POSITION").Count;
                    tri += prim.IndexAccessor.Count / 3;
                }
            }
            info.VertexCount = vtx;
            info.TriangleCount = tri;

            var humanMap = ReadHumanoidMap(glb);
            info.HumanoidBoneCount = humanMap.Count;
        }
        catch
        {
        }

        return info;
    }

}
