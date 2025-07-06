using Assimp;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Numerics;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Vector3D = Assimp.Vector3D;
using SharpGLTF.Schema2;
using SNode = SharpGLTF.Schema2.Node;

namespace MiniMikuDance.Import;

public class ModelData
{
    public List<SubMeshData> SubMeshes { get; } = new();
    public Assimp.Mesh Mesh { get; set; } = null!;
    public System.Numerics.Matrix4x4 Transform { get; set; } = System.Numerics.Matrix4x4.Identity;
    public List<BoneData> Bones { get; } = new();
    public Dictionary<string, int> HumanoidBones { get; } = new();
    public List<(string Name, int Index)> HumanoidBoneList { get; } = new();
    public float ShadeShift { get; set; } = -0.1f;
    public float ShadeToony { get; set; } = 0.9f;
    public float RimIntensity { get; set; } = 0.5f;
    public VrmInfo Info { get; set; } = new();
}

public class ModelImporter
{
    private readonly AssimpContext _context = new();
    private static readonly string[] s_boneOrder = new[]
    {
        "hips",
        "leftUpperLeg",
        "rightUpperLeg",
        "leftLowerLeg",
        "rightLowerLeg",
        "leftFoot",
        "rightFoot",
        "spine",
        "chest",
        "upperChest",
        "neck",
        "head",
        "leftShoulder",
        "rightShoulder",
        "leftUpperArm",
        "rightUpperArm",
        "leftLowerArm",
        "rightLowerArm",
        "leftHand",
        "rightHand",
        "leftToes",
        "rightToes",
        "leftEye",
        "rightEye",
        "jaw",
        "leftThumbProximal",
        "leftThumbIntermediate",
        "leftThumbDistal",
        "leftIndexProximal",
        "leftIndexIntermediate",
        "leftIndexDistal",
        "leftMiddleProximal",
        "leftMiddleIntermediate",
        "leftMiddleDistal",
        "leftRingProximal",
        "leftRingIntermediate",
        "leftRingDistal",
        "leftLittleProximal",
        "leftLittleIntermediate",
        "leftLittleDistal",
        "rightThumbProximal",
        "rightThumbIntermediate",
        "rightThumbDistal",
        "rightIndexProximal",
        "rightIndexIntermediate",
        "rightIndexDistal",
        "rightMiddleProximal",
        "rightMiddleIntermediate",
        "rightMiddleDistal",
        "rightRingProximal",
        "rightRingIntermediate",
        "rightRingDistal",
        "rightLittleProximal",
        "rightLittleIntermediate",
        "rightLittleDistal"
    };

    public ModelData ImportModel(Stream stream)
    {
        Debug.WriteLine("[ModelImporter] Loading model from stream");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var bytes = ms.ToArray();
        ms.Position = 0;
        var textureMap = ReadMainTextureIndices(bytes);
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
        Debug.WriteLine($"[ModelImporter] Loading model: {path}");

        if (!File.Exists(path))
        {
            Debug.WriteLine($"[ModelImporter] File not found: {path}");
            throw new FileNotFoundException("Model file not found", path);
        }

        string ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext == ".vrm" || ext == ".gltf" || ext == ".glb")
        {
            return ImportVrm(path);
        }

        var scene = _context.ImportFile(path, PostProcessSteps.Triangulate | PostProcessSteps.GenerateNormals);
        Debug.WriteLine($"[ModelImporter] Imported generic model: {scene.Meshes[0].VertexCount} vertices");
        return new ModelData { Mesh = scene.Meshes[0] };
    }

    private ModelData ImportVrm(string path)
    {
        Debug.WriteLine($"[ModelImporter] Importing VRM: {path}");
        var bytes = File.ReadAllBytes(path);
        using var ms = new MemoryStream(bytes);
        var info = ReadVrmInfo(bytes);
        var textureMap = ReadMainTextureIndices(bytes);
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
        var bones = ReadBones(model, out var nodeMap);
        data.Bones.AddRange(bones);

        foreach (var logical in model.LogicalMeshes)
        {
            foreach (var prim in logical.Primitives)
            {
                var sub = new Assimp.Mesh("mesh", Assimp.PrimitiveType.Triangle);
                var subUvs = new List<System.Numerics.Vector2>();
                var positions = prim.GetVertexAccessor("POSITION").AsVector3Array();
                var normals = prim.GetVertexAccessor("NORMAL")?.AsVector3Array();
                var uvs = prim.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();
                var jointAcc = prim.GetVertexAccessor("JOINTS_0")?.AsVector4Array();
                var weightAcc = prim.GetVertexAccessor("WEIGHTS_0")?.AsVector4Array();
                var jointList = new List<System.Numerics.Vector4>();
                var weightList = new List<System.Numerics.Vector4>();
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

                    if (jointAcc != null && weightAcc != null && i < jointAcc.Count && i < weightAcc.Count)
                    {
                        var jv = jointAcc[i];
                        var wv = weightAcc[i];
                        jointList.Add(new System.Numerics.Vector4(jv.X, jv.Y, jv.Z, jv.W));
                        weightList.Add(new System.Numerics.Vector4(wv.X, wv.Y, wv.Z, wv.W));
                    }
                    else
                    {
                        jointList.Add(System.Numerics.Vector4.Zero);
                        weightList.Add(System.Numerics.Vector4.Zero);
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
                smd.Joints.AddRange(jointList);
                smd.Weights.AddRange(weightList);

                if (matIndex >= 0 && texMap.TryGetValue(matIndex, out var texIdx))
                {
                    var tex = model.LogicalTextures[texIdx];
                    var imgSeg = tex.PrimaryImage?.GetImageContent();
                    if (imgSeg.HasValue)
                    {
                        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(imgSeg.Value.ToArray());
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

        Debug.WriteLine($"[ModelImporter] VRM loaded: {combined.VertexCount} vertices");

        data.Mesh = combined;
        data.Transform = transform;

        // Humanoid に定義されているが Skin の Joint として登録されていないノードを追加
        foreach (var kv in humanMap)
        {
            if (!nodeMap.ContainsKey(kv.Value))
            {
                var bNode = model.LogicalNodes[kv.Value];
                int parent = -1;
                if (bNode.VisualParent != null && nodeMap.TryGetValue(bNode.VisualParent.LogicalIndex, out var p))
                {
                    parent = p;
                }
                var r = bNode.LocalTransform.Rotation;
                var q = new System.Numerics.Quaternion(r.X, r.Y, -r.Z, r.W);
                var t = bNode.LocalTransform.Translation;
                var pos = new System.Numerics.Vector3(t.X, t.Y, -t.Z);
                nodeMap[kv.Value] = data.Bones.Count;
                data.Bones.Add(new BoneData { Name = bNode.Name ?? kv.Key, Parent = parent, Rotation = q, Translation = pos });
            }
        }

        foreach (var name in s_boneOrder)
        {
            if (humanMap.TryGetValue(name, out var nodeIdx) && nodeMap.TryGetValue(nodeIdx, out var idx))
            {
                data.HumanoidBones[name] = idx;
                data.HumanoidBoneList.Add((name, idx));
                data.Bones[idx].Name = name;
            }
        }

        foreach (var sm in data.SubMeshes)
        {
            for (int i = 0; i < sm.Joints.Count; i++)
            {
                var j = sm.Joints[i];
                int j0 = nodeMap.TryGetValue((int)j.X, out var b0) ? b0 : 0;
                int j1 = nodeMap.TryGetValue((int)j.Y, out var b1) ? b1 : 0;
                int j2 = nodeMap.TryGetValue((int)j.Z, out var b2) ? b2 : 0;
                int j3 = nodeMap.TryGetValue((int)j.W, out var b3) ? b3 : 0;
                sm.Joints[i] = new System.Numerics.Vector4(j0, j1, j2, j3);
            }
        }

        ComputeBindMatrices(data);
        data.ShadeShift = mtoon.shadeShift;
        data.ShadeToony = mtoon.shadeToony;
        data.RimIntensity = mtoon.rimIntensity;
        return data;
    }

    private static void ComputeBindMatrices(ModelData data)
    {
        var world = new System.Numerics.Matrix4x4[data.Bones.Count];
        for (int i = 0; i < data.Bones.Count; i++)
        {
            var b = data.Bones[i];
            var rot = System.Numerics.Matrix4x4.CreateFromQuaternion(b.Rotation);
            var trs = System.Numerics.Matrix4x4.CreateTranslation(b.Translation) * rot;
            if (b.Parent >= 0)
            {
                world[i] = world[b.Parent] * trs;
            }
            else
            {
                world[i] = data.Transform * trs;
            }
            b.BindMatrix = world[i];
            if (b.InverseBindMatrix == System.Numerics.Matrix4x4.Identity)
            {
                System.Numerics.Matrix4x4.Invert(world[i], out var inv);
                b.InverseBindMatrix = inv;
            }
        }
    }


    private static Dictionary<int, int> ReadMainTextureIndices(byte[] glb)
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
            string json = System.Text.Encoding.UTF8.GetString(jsonBytes);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("extensions", out var exts) &&
                exts.TryGetProperty("VRM", out var vrmExt) &&
                vrmExt.TryGetProperty("materialProperties", out var matProps) &&
                matProps.ValueKind == System.Text.Json.JsonValueKind.Array &&
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

            if (root.TryGetProperty("materials", out var mats))
            {
                for (int i = 0; i < mats.GetArrayLength(); i++)
                {
                    if (map.ContainsKey(i)) continue;
                    var mat = mats[i];
                    if (mat.TryGetProperty("pbrMetallicRoughness", out var pbr) &&
                        pbr.TryGetProperty("baseColorTexture", out var tex) &&
                        tex.TryGetProperty("index", out var idxEl))
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

    private static List<BoneData> ReadBones(SharpGLTF.Schema2.ModelRoot model, out Dictionary<int, int> nodeToIndex)
    {
        var bones = new List<BoneData>();
        var nodes = new List<SNode>();
        var visited = new HashSet<SNode>();

        foreach (var skin in model.LogicalSkins)
        {
            foreach (var joint in EnumerateJoints(skin))
            {
                if (!visited.Add(joint)) continue;
                nodes.Add(joint);
            }
        }

        nodeToIndex = new Dictionary<int, int>();
        for (int i = 0; i < nodes.Count; i++)
        {
            var joint = nodes[i];
            nodeToIndex[joint.LogicalIndex] = i;
        }

        var invMap = new Dictionary<int, System.Numerics.Matrix4x4>();
        foreach (var skin in model.LogicalSkins)
        {
            for (int i = 0; i < skin.JointsCount; i++)
            {
                var (joint, m) = skin.GetJoint(i);
                if (joint == null) continue;
                if (!nodeToIndex.TryGetValue(joint.LogicalIndex, out var idx)) continue;
                var mat = new System.Numerics.Matrix4x4(
                    m.M11, m.M12, m.M13, m.M14,
                    m.M21, m.M22, m.M23, m.M24,
                    m.M31, m.M32, m.M33, m.M34,
                    m.M41, m.M42, m.M43, m.M44);
                var flip = System.Numerics.Matrix4x4.CreateScale(1f, 1f, -1f);
                mat = flip * mat * flip;
                invMap[idx] = mat;
            }
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            var joint = nodes[i];
            int parent = -1;
            if (joint.VisualParent != null && nodeToIndex.TryGetValue(joint.VisualParent.LogicalIndex, out var p))
            {
                parent = p;
            }
            var r = joint.LocalTransform.Rotation;
            var q = new System.Numerics.Quaternion(r.X, r.Y, -r.Z, r.W);
            var t = joint.LocalTransform.Translation;
            var pos = new System.Numerics.Vector3(t.X, t.Y, -t.Z);
            var bone = new BoneData { Name = joint.Name ?? string.Empty, Parent = parent, Rotation = q, Translation = pos };
            if (invMap.TryGetValue(i, out var ibm))
            {
                bone.InverseBindMatrix = ibm;
            }
            bones.Add(bone);
        }

        return bones;
    }

    private static IEnumerable<SNode> EnumerateJoints(Skin skin)
    {
        for (int i = 0; i < skin.JointsCount; i++)
        {
            var (joint, _) = skin.GetJoint(i);
            if (joint != null)
            {
                yield return joint;
            }
        }
    }
}
