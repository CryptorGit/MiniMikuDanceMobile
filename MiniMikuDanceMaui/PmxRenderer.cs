using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using MiniMikuDance.Util;
using MiniMikuDance.Import;
using MiniMikuDance.App;
using MiniMikuDance.IK;
using SharpBgfx;
using VertexBuffer = SharpBgfx.DynamicVertexBuffer;
using IndexBuffer = SharpBgfx.DynamicIndexBuffer;
using Matrix4 = System.Numerics.Matrix4x4;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;
using Quaternion = System.Numerics.Quaternion;

namespace MiniMikuDanceMaui;

public partial class PmxRenderer : IRenderer, IDisposable
{
    private Program? _program;
    private class RenderMesh
    {
        public VertexBuffer? VertexBuffer;
        public IndexBuffer? IndexBuffer;
        public int IndexCount;
        public ushort[] Indices16 = Array.Empty<ushort>();
        public uint[] Indices32 = Array.Empty<uint>();
        public bool IndicesDirty;
        public Vector4 Color = Vector4.One;
        public Vector4 BaseColor = Vector4.One;
        public Vector3 Specular = Vector3.Zero;
        public Vector3 BaseSpecular = Vector3.Zero;
        public float SpecularPower;
        public float BaseSpecularPower;
        public Vector4 EdgeColor = Vector4.Zero;
        public Vector4 BaseEdgeColor = Vector4.Zero;
        public float EdgeSize;
        public float BaseEdgeSize;
        public Vector3 ToonColor = Vector3.Zero;
        public Vector3 BaseToonColor = Vector3.Zero;
        public Vector4 TextureTint = Vector4.One;
        public Vector4 BaseTextureTint = Vector4.One;
        public Texture? Texture;
        public bool HasTexture;
        public Uniform? ColorUniform;
        public Uniform? SpecularUniform;
        public Uniform? EdgeUniform;
        public Uniform? ToonColorUniform;
        public Uniform? TextureTintUniform;
        public Uniform? TextureUniform;
        public Vector3[] BaseVertices = Array.Empty<Vector3>();
        public Vector3[] VertexOffsets = Array.Empty<Vector3>();
        public Vector3[] Normals = Array.Empty<Vector3>();
        public Vector2[] TexCoords = Array.Empty<Vector2>();
        public Vector2[] UvOffsets = Array.Empty<Vector2>();
        public Vector4[] JointIndices = Array.Empty<Vector4>();
        public Vector4[] JointWeights = Array.Empty<Vector4>();
        public Vector3[] SdefC = Array.Empty<Vector3>();
        public Vector3[] SdefR0 = Array.Empty<Vector3>();
        public Vector3[] SdefR1 = Array.Empty<Vector3>();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PmxVertex
    {
        public float Px;
        public float Py;
        public float Pz;
        public float Nx;
        public float Ny;
        public float Nz;
        public float U;
        public float V;
        public static readonly VertexLayout Layout;
        static PmxVertex()
        {
            Layout = new VertexLayout();
            Layout.Begin()
                .Add(VertexAttributeUsage.Position, 3, VertexAttributeType.Float)
                .Add(VertexAttributeUsage.Normal, 3, VertexAttributeType.Float)
                .Add(VertexAttributeUsage.TexCoord0, 2, VertexAttributeType.Float)
                .End();
        }
    }
    private readonly List<RenderMesh> _meshes = new();
    private readonly Dictionary<string, MorphData> _morphs = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<MorphCategory, List<MorphData>> _morphsByCategory = new();
    private readonly Dictionary<string, float> _morphValues = new(StringComparer.OrdinalIgnoreCase);
    private List<(RenderMesh Mesh, int Index)>?[] _morphVertexMap = Array.Empty<List<(RenderMesh Mesh, int Index)>?>();
    private readonly HashSet<int> _changedOriginalVertices = new();
    private readonly object _changedVerticesLock = new();

    private void MarkVertexChanged(int vid)
    {
        lock (_changedVerticesLock)
            _changedOriginalVertices.Add(vid);
    }

    private void CollectChangedVertices(Dictionary<RenderMesh, List<int>> changed)
    {
        lock (_changedVerticesLock)
        {
            if (_changedOriginalVertices.Count == 0)
                return;

            foreach (var vid in _changedOriginalVertices)
            {
                var list = _morphVertexMap[vid];
                if (list == null)
                    continue;
                foreach (var (mesh, idx) in list)
                {
                    if (!changed.TryGetValue(mesh, out var idxList))
                    {
                        idxList = new List<int>();
                        changed[mesh] = idxList;
                    }
                    idxList.Add(idx);
                }
            }
            _changedOriginalVertices.Clear();
        }

        foreach (var kv in changed)
        {
            var list = kv.Value;
            list.Sort();
            int last = -1;
            for (int i = 0; i < list.Count;)
            {
                if (list[i] == last)
                    list.RemoveAt(i);
                else
                {
                    last = list[i];
                    i++;
                }
            }
        }
    }

    private Vector3[] _vertexTotalOffsets = Array.Empty<Vector3>();
    private List<(string MorphName, Vector3 Offset)>?[] _vertexMorphOffsets = Array.Empty<List<(string MorphName, Vector3 Offset)>?>();
    private List<(string MorphName, System.Numerics.Vector4 Offset)>?[] _uvMorphOffsets = Array.Empty<List<(string MorphName, System.Numerics.Vector4 Offset)>?>();
    private string[] _morphIndexToName = Array.Empty<string>();
    private Vector3[] _boneMorphTranslations = Array.Empty<Vector3>();
    private Quaternion[] _boneMorphRotations = Array.Empty<Quaternion>();
    public IViewer? Viewer { get; set; }
    private float _orbitX;
    // 初期カメラ位置: 水平回転はπ（モデル正面を向く）
    private float _orbitY = MathF.PI;
    private float _distance = 4f;
    // モデル中心より少し高い位置を基準にカメラを配置する
    private Vector3 _target = new Vector3(0f, 0.5f, 0f);
    private int _boneVertexCount;
    private System.Numerics.Matrix4x4[] _worldMats = Array.Empty<System.Numerics.Matrix4x4>();
    private System.Numerics.Matrix4x4[] _skinMats = Array.Empty<System.Numerics.Matrix4x4>();
    private float[] _boneLines = Array.Empty<float>();
    private int _boneCapacity;
    private Program? _modelProgram;
    private Matrix4 _modelTransform = Matrix4.Identity;
    private Uniform? _lightDirUniform;
    private Uniform? _lightColorUniform;
    private Uniform? _shadeParamUniform;
    public Matrix4 ModelTransform
    {
        get => _modelTransform;
        set
        {
            if (!_modelTransform.Equals(value))
            {
                _modelTransform = value;
                Viewer?.Invalidate();
            }
        }
    }
    private int _width;
    private int _height;
    private Matrix4 _viewMatrix = Matrix4.Identity;
    private Matrix4 _projMatrix = Matrix4.Identity;
    private Matrix4 _cameraRot = Matrix4.Identity;
    private Vector3 _cameraPos;
    private bool _viewProjDirty = true;
    private readonly List<Vector3> _boneRotations = new();
    private readonly List<Vector3> _boneTranslations = new();
    private bool _bonesDirty;
    private bool _morphDirty;
    private bool _uvMorphDirty;
    private List<MiniMikuDance.Import.BoneData> _bones = new();
    private readonly List<IkBone> _ikBones = new();
    private readonly object _ikBonesLock = new();
    private readonly Dictionary<int, string> _indexToHumanoidName = new();
    public BonesConfig? BonesConfig { get; set; }
    private Quaternion _externalRotation = Quaternion.Identity;
    // デフォルトのカメラ感度をスライダーの最小値に合わせる
    public float RotateSensitivity { get; set; } = 0.1f;
    public float PanSensitivity { get; set; } = 1f;
    public float ZoomSensitivity { get; set; } = 1f;
    public float ShadeShift { get; set; } = -0.1f;
    public float ShadeToony { get; set; } = 0.9f;
    public float RimIntensity { get; set; } = 0.5f;
    public float Ambient { get; set; } = 0.3f;
    public Vector3 LightDirection { get; set; } = new Vector3(0f, -1f, 0f);
    public Vector3 LightColor { get; set; } = Vector3.One;
    private bool _showBoneOutline;
    public bool ShowBoneOutline
    {
        get => _showBoneOutline;
        set
        {
            if (_showBoneOutline != value)
            {
                _showBoneOutline = value;
                // Ensure bones buffer gets (re)built on next frame
                _bonesDirty = true;
                Viewer?.Invalidate();
            }
        }
    }

    private bool _showIkBones;
    public bool ShowIkBones
    {
        get => _showIkBones;
        set
        {
            if (_showIkBones != value)
            {
                _showIkBones = value;
                Viewer?.Invalidate();
            }
        }
    }

    private float _ikBoneScale = AppSettings.DefaultIkBoneScale;
    public float IkBoneScale
    {
        get => _ikBoneScale;
        set
        {
            if (_ikBoneScale != value)
            {
                _ikBoneScale = value;
                Viewer?.Invalidate();
            }
        }
    }

    private float _stageSize = AppSettings.DefaultStageSize;
    public float StageSize
    {
        get => _stageSize;
        set
        {
            if (_stageSize != value)
                _stageSize = value;
        }
    }

    private float _defaultCameraDistance = AppSettings.DefaultCameraDistance;
    public float DefaultCameraDistance
    {
        get => _defaultCameraDistance;
        set => _defaultCameraDistance = value;
    }

    private float _defaultCameraTargetY = AppSettings.DefaultCameraTargetY;
    public float DefaultCameraTargetY
    {
        get => _defaultCameraTargetY;
        set => _defaultCameraTargetY = value;
    }

    private float _bonePickPixels = AppSettings.DefaultBonePickPixels;
    public float BonePickPixels
    {
        get => _bonePickPixels;
        set => _bonePickPixels = value;
    }

    // EnsureBoneCapacity は PmxRenderer.Render.cs へ移動

    public void Initialize()
    {
        _program = LoadProgram("pmx");
        _modelProgram = _program;
        _lightDirUniform = new Uniform("u_lightDir", UniformType.Vector4);
        _lightColorUniform = new Uniform("u_lightColor", UniformType.Vector4);
        _shadeParamUniform = new Uniform("u_shadeParam", UniformType.Vector4);
    }

    private static Shader LoadShader(string name)
    {
        var assembly = typeof(PmxRenderer).Assembly;
        using var stream = assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException($"Shader resource '{name}' not found.");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return new Shader(MemoryBlock.FromArray(ms.ToArray()));
    }

    private static Program LoadProgram(string baseName)
    {
#if __ANDROID__
        var suffix = "gles3";
#elif __IOS__
        var suffix = "metal";
#else
        var suffix = "spirv";
#endif
        var vs = LoadShader($"MiniMikuDanceMaui.Resources.Shaders.{baseName}.vs.{suffix}.sc");
        var fs = LoadShader($"MiniMikuDanceMaui.Resources.Shaders.{baseName}.fs.{suffix}.sc");
        return new Program(vs, fs, true);
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
        Bgfx.Reset(width, height, ResetFlags.Vsync);
        _viewProjDirty = true;
    }

    public void Orbit(float dx, float dy)
    {
        _orbitY -= dx * 0.01f * RotateSensitivity;
        _orbitX -= dy * 0.01f * RotateSensitivity;
        // Clamp pitch to [-90°, 90°]
        float limit = 89.9f * (MathF.PI / 180f);
        if (_orbitX < -limit) _orbitX = -limit;
        if (_orbitX > limit) _orbitX = limit;
        _viewProjDirty = true;
    }

    public void Pan(float dx, float dy)
    {
        Matrix4 rot = Matrix4.CreateFromQuaternion(_externalRotation) *
                     Matrix4.CreateRotationX(_orbitX) *
                     Matrix4.CreateRotationY(_orbitY);
        Vector3 right = Vector3.TransformNormal(Vector3.UnitX, rot);
        Vector3 up = Vector3.TransformNormal(Vector3.UnitY, rot);
        _target += (-right * dx + up * dy) * 0.01f * PanSensitivity;
        _viewProjDirty = true;
    }

    public void Dolly(float delta)
    {
        _distance -= delta * 0.01f * MathF.Max(0.01f, ZoomSensitivity);
        if (_distance < 1f) _distance = 1f;
        if (_distance > 100f) _distance = 100f;
        _viewProjDirty = true;
    }

    public void ResetCamera()
    {
        _orbitX = 0f;
        _orbitY = MathF.PI;
        _target = new Vector3(0f, _defaultCameraTargetY, 0f);
        _distance = _defaultCameraDistance;
        if (_distance < 1f) _distance = 1f;
        if (_distance > 100f) _distance = 100f;
        _externalRotation = Quaternion.Identity;
        _viewProjDirty = true;
    }

    public void SetExternalRotation(Quaternion q)
    {
        _externalRotation = q;
        _viewProjDirty = true;
    }

    public void ClearBoneRotations()
    {
        _boneRotations.Clear();
        _boneTranslations.Clear();
        _bonesDirty = true;
    }

    public void SetIkBones(IEnumerable<IkBone> bones)
    {
        lock (_ikBonesLock)
        {
            _ikBones.Clear();
            _ikBones.AddRange(bones);
            foreach (var ik in _ikBones)
            {
                ik.Position = GetBoneWorldPosition(ik.PmxBoneIndex);
            }
        }
    }

    public void ClearIkBones()
    {
        lock (_ikBonesLock)
        {
            _ikBones.Clear();
        }
    }

    private void UpdateIkBoneWorldPositions()
    {
        lock (_ikBonesLock)
        {
            if (_ikBones.Count == 0 || _worldMats.Length == 0)
                return;

            foreach (var ik in _ikBones)
            {
                ik.Position = GetBoneWorldPosition(ik.PmxBoneIndex);
            }
        }
    }

    // DrawIkBones は PmxRenderer.Render.cs へ移動

    // スクリーン座標から最も近いボーンを選択
    public int PickBone(float screenX, float screenY)
    {
        if (_bones.Count == 0 || _width == 0 || _height == 0)
            return -1;

        int result = -1;
        float scale = _defaultCameraDistance != 0 ? _distance / _defaultCameraDistance : 1f;
        float best = _bonePickPixels * scale; // ピクセル閾値

        if (_ikBones.Count > 0)
        {
            best *= 1.5f; // IKボーンは選択しやすくする
            foreach (var bone in _ikBones)
            {
                int i = bone.PmxBoneIndex;
                var pos = _worldMats[i].Translation;
                var v4 = new Vector4(pos, 1f);
                var clip = Vector4.Transform(v4, _viewMatrix);
                clip = Vector4.Transform(clip, _projMatrix);
                if (clip.W <= 0)
                    continue;
                var ndc = new Vector3(clip.X, clip.Y, clip.Z) / clip.W;
                var sx = (ndc.X * 0.5f + 0.5f) * _width;
                var sy = (-ndc.Y * 0.5f + 0.5f) * _height;
                var dx = sx - screenX;
                var dy = sy - screenY;
                var dist = MathF.Sqrt(dx * dx + dy * dy);
                if (dist < best)
                {
                    best = dist;
                    result = i;
                }
            }
        }
        else
        {
            int limit = Math.Min(_worldMats.Length, _bones.Count);
            for (int i = 0; i < limit; i++)
            {
                var pos = _worldMats[i].Translation;
                var v4 = new Vector4(pos, 1f);
                var clip = Vector4.Transform(v4, _viewMatrix);
                clip = Vector4.Transform(clip, _projMatrix);
                if (clip.W <= 0)
                    continue;
                var ndc = new Vector3(clip.X, clip.Y, clip.Z) / clip.W;
                var sx = (ndc.X * 0.5f + 0.5f) * _width;
                var sy = (-ndc.Y * 0.5f + 0.5f) * _height;
                var dx = sx - screenX;
                var dy = sy - screenY;
                var dist = MathF.Sqrt(dx * dx + dy * dy);
                if (dist < best)
                {
                    best = dist;
                    result = i;
                }
            }
        }
        return result;
    }

    public System.Numerics.Vector3 GetBoneWorldPosition(int index)
    {
        if (index < 0 || index >= _worldMats.Length)
            return System.Numerics.Vector3.Zero;
        var pos = _worldMats[index].Translation;
        pos = Vector3.Transform(pos, _modelTransform);
        return pos;
    }

    public System.Numerics.Vector3 GetCameraPosition()
    {
        return _cameraPos;
    }

    public System.Numerics.Vector3 WorldToModel(System.Numerics.Vector3 worldPos)
    {
        Matrix4.Invert(_modelTransform, out var inv);
        return Vector3.Transform(worldPos, inv);
    }

    public System.Numerics.Vector3 ModelToWorld(System.Numerics.Vector3 modelPos)
    {
        var pos = Vector3.Transform(modelPos, _modelTransform);
        return pos;
    }

    public (System.Numerics.Vector3 Origin, System.Numerics.Vector3 Direction) ScreenPointToRay(float screenX, float screenY)
    {
        if (_width == 0 || _height == 0)
            return (System.Numerics.Vector3.Zero, System.Numerics.Vector3.UnitZ);

        float x = (2f * screenX / _width) - 1f; // X軸はそのまま
        float y = 1f - (2f * screenY / _height); // スクリーン座標ではY軸が下向きのため反転
        Matrix4.Invert(_projMatrix, out var invProj);
        Matrix4.Invert(_viewMatrix, out var invView);
        var rayClip = new Vector4(x, y, -1f, 1f);
        var rayEye = Vector4.Transform(rayClip, invProj);
        // ビュー空間では前方を -Z とするため、Z軸を反転する
        rayEye.Z = -1f; rayEye.W = 0f;
        var rayWorld = Vector4.Transform(rayEye, invView);
        var dir = Vector3.Normalize(new Vector3(rayWorld.X, rayWorld.Y, rayWorld.Z));
        return (_cameraPos, dir);
    }

    public void SetBoneRotation(int index, Vector3 degrees)
    {
        if (index < 0)
            return;
        if (BonesConfig != null && index < _bones.Count)
        {
            var name = _indexToHumanoidName.TryGetValue(index, out var n) ? n : _bones[index].Name;
            var clamped = BonesConfig.Clamp(name, degrees);
            degrees = clamped;
        }
        while (_boneRotations.Count <= index)
            _boneRotations.Add(Vector3.Zero);
        _boneRotations[index] = degrees;
        _bonesDirty = true;
        Viewer?.Invalidate();
    }

    // RecalculateBoneMorphs は PmxRenderer.Morph.cs へ移動

    // RecalculateMaterialMorphs は PmxRenderer.Morph.cs へ移動

    private System.Numerics.Matrix4x4[] CalculateWorldMatrices()
    {
        var worldMats = new System.Numerics.Matrix4x4[_bones.Count];
        for (int i = 0; i < _bones.Count; i++)
        {
            var bone = _bones[i];
            Vector3 euler = i < _boneRotations.Count ? _boneRotations[i] : Vector3.Zero;
            var rot = euler.FromEulerDegrees();
            if (bone.HasFixedAxis)
                rot = ProjectRotation(rot, bone.FixedAxis);
            System.Numerics.Vector3 trans = bone.Translation;
            if (i < _boneTranslations.Count)
                trans += _boneTranslations[i];
            var rotMat = System.Numerics.Matrix4x4.CreateFromQuaternion(rot);
            if (bone.HasLocalAxis)
            {
                var x = System.Numerics.Vector3.Normalize(bone.LocalAxisX);
                var z = System.Numerics.Vector3.Normalize(bone.LocalAxisZ);
                var y = System.Numerics.Vector3.Normalize(System.Numerics.Vector3.Cross(z, x));
                var basis = new System.Numerics.Matrix4x4(
                    x.X, x.Y, x.Z, 0f,
                    y.X, y.Y, y.Z, 0f,
                    z.X, z.Y, z.Z, 0f,
                    0f, 0f, 0f, 1f);
                rotMat = basis * rotMat * System.Numerics.Matrix4x4.Transpose(basis);
            }
            var local = System.Numerics.Matrix4x4.CreateFromQuaternion(bone.Rotation) *
                        rotMat *
                        System.Numerics.Matrix4x4.CreateTranslation(trans);
            worldMats[i] = bone.Parent >= 0 ? local * worldMats[bone.Parent] : local;
        }
        return worldMats;
    }

    private static Quaternion ProjectRotation(Quaternion q, Vector3 axis)
    {
        axis = Vector3.Normalize(axis);
        if (axis == Vector3.Zero)
            return Quaternion.Identity;
        q = Quaternion.Normalize(q);
        var w = Math.Clamp(q.W, -1f, 1f);
        float angle = 2f * MathF.Acos(w);
        float s = MathF.Sqrt(MathF.Max(0f, 1f - w * w));
        Vector3 qAxis = s < 1e-6f ? axis : new Vector3(q.X / s, q.Y / s, q.Z / s);
        float proj = Vector3.Dot(qAxis, axis);
        return Quaternion.CreateFromAxisAngle(axis, angle * proj);
    }

    public void SetBoneTranslation(int index, Vector3 worldPos)
    {
        if (index < 0 || index >= _bones.Count)
            return;

        var bone = _bones[index];
        var worldMats = CalculateWorldMatrices();
        var parentWorld = bone.Parent >= 0 && bone.Parent < _bones.Count
            ? worldMats[bone.Parent]
            : System.Numerics.Matrix4x4.Identity;
        System.Numerics.Matrix4x4.Invert(parentWorld, out var invParent);
        var localPos = Vector3.Transform(worldPos, invParent);
        var delta = localPos - bone.Translation;

        while (_boneTranslations.Count <= index)
            _boneTranslations.Add(Vector3.Zero);
        _boneTranslations[index] = delta;
        _bonesDirty = true;
        Viewer?.Invalidate();
    }

    public Vector3 GetBoneRotation(int index)
    {
        if (index < 0 || index >= _boneRotations.Count)
            return Vector3.Zero;
        return _boneRotations[index];
    }

    public Vector3 GetBoneTranslation(int index)
    {
        if (index < 0 || index >= _boneTranslations.Count)
            return Vector3.Zero;
        return _boneTranslations[index];
    }

    // Morph関連メソッドは PmxRenderer.Morph.cs へ移動

    public void LoadModel(MiniMikuDance.Import.ModelData data)
    {
        foreach (var rm in _meshes)
        {
            rm.VertexBuffer?.Dispose();
            rm.VertexBuffer = null;
            rm.IndexBuffer?.Dispose();
            rm.IndexBuffer = null;
            rm.Texture?.Dispose();
            rm.Texture = null;
            rm.ColorUniform?.Dispose();
            rm.ColorUniform = null;
            rm.SpecularUniform?.Dispose();
            rm.SpecularUniform = null;
            rm.EdgeUniform?.Dispose();
            rm.EdgeUniform = null;
            rm.ToonColorUniform?.Dispose();
            rm.ToonColorUniform = null;
            rm.TextureTintUniform?.Dispose();
            rm.TextureTintUniform = null;
            rm.TextureUniform?.Dispose();
            rm.TextureUniform = null;
        }
        _meshes.Clear();
        _indexToHumanoidName.Clear();
        _bones = data.Bones;
        _worldMats = new System.Numerics.Matrix4x4[_bones.Count];
        _skinMats = new System.Numerics.Matrix4x4[_bones.Count];
        foreach (var (name, idx) in data.HumanoidBoneList)
        {
            _indexToHumanoidName[idx] = name;
        }

        _modelTransform = data.Transform.ToMatrix4();

        foreach (var smd in data.SubMeshes)
        {
            var rm = new RenderMesh();
            var mesh = smd.Mesh;
            int vCount = mesh.VertexCount;
            rm.BaseVertices = new Vector3[vCount];
            rm.VertexOffsets = new Vector3[vCount];
            rm.Normals = new Vector3[vCount];
            rm.TexCoords = smd.TexCoords.ToArray();
            rm.UvOffsets = new Vector2[vCount];
            rm.JointIndices = smd.JointIndices.ToArray();
            rm.JointWeights = smd.JointWeights.ToArray();
            rm.SdefC = smd.SdefC.ToArray();
            rm.SdefR0 = smd.SdefR0.ToArray();
            rm.SdefR1 = smd.SdefR1.ToArray();
            rm.Color = rm.BaseColor = smd.ColorFactor;
            rm.Specular = rm.BaseSpecular = smd.Specular;
            rm.SpecularPower = rm.BaseSpecularPower = smd.SpecularPower;
            rm.EdgeColor = rm.BaseEdgeColor = smd.EdgeColor;
            rm.EdgeSize = rm.BaseEdgeSize = smd.EdgeSize;
            rm.ToonColor = rm.BaseToonColor = smd.ToonColor;
            rm.TextureTint = rm.BaseTextureTint = smd.TextureTint;

            for (int i = 0; i < vCount; i++)
            {
                var v = mesh.Vertices[i];
                rm.BaseVertices[i] = new Vector3(v.X, v.Y, v.Z);
                if (i < mesh.Normals.Count)
                {
                    var n = mesh.Normals[i];
                    rm.Normals[i] = new Vector3(n.X, n.Y, n.Z);
                }
            }

            var verts = new PmxVertex[vCount];
            for (int i = 0; i < vCount; i++)
            {
                verts[i] = new PmxVertex
                {
                    Px = rm.BaseVertices[i].X,
                    Py = rm.BaseVertices[i].Y,
                    Pz = rm.BaseVertices[i].Z,
                    Nx = rm.Normals.Length > i ? rm.Normals[i].X : 0f,
                    Ny = rm.Normals.Length > i ? rm.Normals[i].Y : 0f,
                    Nz = rm.Normals.Length > i ? rm.Normals[i].Z : 0f,
                    U = rm.TexCoords.Length > i ? rm.TexCoords[i].X : 0f,
                    V = rm.TexCoords.Length > i ? rm.TexCoords[i].Y : 0f
                };
            }
            rm.VertexBuffer = new VertexBuffer(MemoryBlock.FromArray(verts), PmxVertex.Layout);

            int indexCount = mesh.FaceCount * 3;
            rm.IndexCount = indexCount;
            if (vCount >= ushort.MaxValue)
            {
                var idx = new uint[indexCount];
                int k = 0;
                foreach (var f in mesh.Faces)
                {
                    idx[k++] = (uint)f.Indices[0];
                    idx[k++] = (uint)f.Indices[1];
                    idx[k++] = (uint)f.Indices[2];
                }
                rm.Indices32 = idx;
                rm.Indices16 = Array.Empty<ushort>();
                rm.IndexBuffer = new IndexBuffer(MemoryBlock.FromArray(idx), BufferFlags.Index32);
            }
            else
            {
                var idx = new ushort[indexCount];
                int k = 0;
                foreach (var f in mesh.Faces)
                {
                    idx[k++] = (ushort)f.Indices[0];
                    idx[k++] = (ushort)f.Indices[1];
                    idx[k++] = (ushort)f.Indices[2];
                }
                rm.Indices16 = idx;
                rm.Indices32 = Array.Empty<uint>();
                rm.IndexBuffer = new IndexBuffer(MemoryBlock.FromArray(idx));
            }
            rm.IndicesDirty = false;

            if (smd.TextureBytes != null && smd.TextureWidth > 0 && smd.TextureHeight > 0)
            {
                rm.Texture = Texture.Create2D((int)smd.TextureWidth, (int)smd.TextureHeight, false, 1,
                    TextureFormat.RGBA8, TextureFlags.None,
                    MemoryBlock.FromArray(smd.TextureBytes));
                rm.HasTexture = true;
            }
            else
            {
                rm.HasTexture = false;
            }

            rm.ColorUniform = new Uniform("u_color", UniformType.Vector4);
            rm.SpecularUniform = new Uniform("u_specular", UniformType.Vector4);
            rm.EdgeUniform = new Uniform("u_edge", UniformType.Vector4);
            rm.ToonColorUniform = new Uniform("u_toonColor", UniformType.Vector4);
            rm.TextureTintUniform = new Uniform("u_textureTint", UniformType.Vector4);
            rm.TextureUniform = new Uniform("s_texColor", UniformType.Sampler);

            _meshes.Add(rm);
        }

        _morphs.Clear();
        _morphsByCategory.Clear();
        _morphValues.Clear();
        int totalVertices = data.Mesh.VertexCount;
        _morphVertexMap = new List<(RenderMesh Mesh, int Index)>?[totalVertices];
        _vertexTotalOffsets = new Vector3[totalVertices];
        _vertexMorphOffsets = new List<(string MorphName, Vector3 Offset)>?[totalVertices];
        _uvMorphOffsets = new List<(string MorphName, System.Numerics.Vector4 Offset)>?[totalVertices];
        _morphIndexToName = new string[data.Morphs.Count];
        foreach (var morph in data.Morphs)
        {
            var name = MorphNameUtil.EnsureUniqueName(morph.Name, _morphs.ContainsKey);
            morph.Name = name;
            _morphs[name] = morph;
            if (!_morphsByCategory.TryGetValue(morph.Category, out var list))
            {
                list = new List<MorphData>();
                _morphsByCategory[morph.Category] = list;
            }
            list.Add(morph);
            if (morph.Index >= 0 && morph.Index < _morphIndexToName.Length)
                _morphIndexToName[morph.Index] = name;
        }

        foreach (var (mName, mData) in _morphs)
        {
            if (mData.Type != MorphType.Vertex) continue;
            foreach (var off in mData.Offsets)
            {
                var vec = new Vector3(off.Vertex.X, off.Vertex.Y, off.Vertex.Z);
                var list = _vertexMorphOffsets[off.Index];
                if (list == null)
                {
                    list = new List<(string MorphName, Vector3 Offset)>();
                    _vertexMorphOffsets[off.Index] = list;
                }
                list.Add((mName, vec));
            }
        }

        foreach (var (mName, mData) in _morphs)
        {
            if (mData.Type != MorphType.UV) continue;
            foreach (var off in mData.Offsets)
            {
                var vec = off.Uv.Offset;
                var list = _uvMorphOffsets[off.Index];
                if (list == null)
                {
                    list = new List<(string MorphName, System.Numerics.Vector4 Offset)>();
                    _uvMorphOffsets[off.Index] = list;
                }
                list.Add((mName, vec));
            }
        }

        static ulong MakeVertexKey(Vector3 pos, Vector3 nor, Vector2 uv)
        {
            const ulong basis = 1469598103934665603UL;
            const ulong prime = 1099511628211UL;
            ulong hash = basis;
            ulong Add(ulong h, float f)
            {
                h ^= (uint)BitConverter.SingleToInt32Bits(f);
                return h * prime;
            }
            hash = Add(hash, pos.X);
            hash = Add(hash, pos.Y);
            hash = Add(hash, pos.Z);
            hash = Add(hash, nor.X);
            hash = Add(hash, nor.Y);
            hash = Add(hash, nor.Z);
            hash = Add(hash, uv.X);
            hash = Add(hash, uv.Y);
            return hash;
        }

        var lookup = new Dictionary<ulong, List<int>>();
        var meshVerts = data.Mesh.Vertices;
        var meshNorms = data.Mesh.Normals;
        var meshUVs = data.Mesh.TextureCoordinateChannels[0];
        for (int i = 0; i < data.Mesh.VertexCount; i++)
        {
            var pos = new Vector3(meshVerts[i].X, meshVerts[i].Y, meshVerts[i].Z);
            var nor = i < meshNorms.Count ? new Vector3(meshNorms[i].X, meshNorms[i].Y, meshNorms[i].Z) : Vector3.Zero;
            var uv = i < meshUVs.Count ? new Vector2(meshUVs[i].X, meshUVs[i].Y) : Vector2.Zero;
            var key = MakeVertexKey(pos, nor, uv);
            if (!lookup.TryGetValue(key, out var list))
            {
                list = new List<int>();
                lookup[key] = list;
            }
            list.Add(i);
        }

        foreach (var rm in _meshes)
        {
            for (int i = 0; i < rm.BaseVertices.Length; i++)
            {
                var pos = rm.BaseVertices[i];
                var nor = i < rm.Normals.Length ? rm.Normals[i] : Vector3.Zero;
                var uv = i < rm.TexCoords.Length ? rm.TexCoords[i] : Vector2.Zero;
                var key = MakeVertexKey(pos, nor, uv);
                if (lookup.TryGetValue(key, out var idxList))
                {
                    foreach (var idx in idxList)
                    {
                        var l = _morphVertexMap[idx];
                        if (l == null)
                        {
                            l = new List<(RenderMesh Mesh, int Index)>();
                            _morphVertexMap[idx] = l;
                        }
                        l.Add((rm, i));
                    }
                }
            }
        }

        RecalculateMaterialMorphs();
        RecalculateBoneMorphs();
        _bonesDirty = true;

        // Auto-fit camera using bone parents' bind positions (preferred),
        // falling back to mesh bounds if unavailable.
        try
        {
            bool fitDone = false;
            if (_bones.Count > 0)
            {
                var isParent = new bool[_bones.Count];
                foreach (var b in _bones)
                {
                    if (b.Parent >= 0 && b.Parent < _bones.Count) isParent[b.Parent] = true;
                }

                System.Numerics.Vector3? minN = null;
                System.Numerics.Vector3? maxN = null;
                for (int i = 0; i < _bones.Count; i++)
                {
                    if (!isParent[i]) continue;
                    var p = _bones[i].BindMatrix.Translation;
                    if (minN == null || maxN == null)
                    {
                        minN = p; maxN = p;
                    }
                    else
                    {
                        minN = System.Numerics.Vector3.Min(minN.Value, p);
                        maxN = System.Numerics.Vector3.Max(maxN.Value, p);
                    }
                }

                if (minN.HasValue && maxN.HasValue)
                {
                    var center = (minN.Value + maxN.Value) * 0.5f;
                    var ext = maxN.Value - minN.Value;
                    float radius = 0.5f * ext.Length();
                    if (radius < 0.01f) radius = 1f;
                    float fov = MathF.PI / 4f; // must match projection
                    float dist = (radius / MathF.Tan(fov * 0.5f)) * 1.2f; // margin
                    // Only respect height (Y). X and Z centered to 0 per request.
                    _target = new Vector3(0f, center.Y, 0f);
                    _distance = Math.Clamp(dist, 1f, 100f);
                    _orbitX = 0f;
                    _orbitY = MathF.PI;
                    _viewProjDirty = true;
                    fitDone = true;
                }
            }

            if (!fitDone && data.Mesh != null && data.Mesh.Vertices.Count > 0)
            {
                var v0 = data.Mesh.Vertices[0];
                System.Numerics.Vector3 min = new(v0.X, v0.Y, v0.Z);
                System.Numerics.Vector3 max = min;
                foreach (var v in data.Mesh.Vertices)
                {
                    var p = new System.Numerics.Vector3(v.X, v.Y, v.Z);
                    min = System.Numerics.Vector3.Min(min, p);
                    max = System.Numerics.Vector3.Max(max, p);
                }
                var center = (min + max) * 0.5f;
                var ext = max - min;
                float radius = 0.5f * ext.Length();
                if (radius < 0.01f) radius = 1f;
                float fov = MathF.PI / 4f;
                float dist = (radius / MathF.Tan(fov * 0.5f)) * 1.2f;
                _target = new Vector3(0f, center.Y, 0f);
                _distance = Math.Clamp(dist, 1f, 100f);
                _orbitX = 0f;
                _orbitY = MathF.PI;
                _viewProjDirty = true;
            }
        }
        catch { /* ignore fit errors */ }
    }

    private static ReadOnlySpan<float> AsSpan(in Matrix4 m) =>
        MemoryMarshal.Cast<byte, float>(MemoryMarshal.AsBytes(
            MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in m), 1)));

    private static ReadOnlySpan<float> AsSpan(in Vector4 v) =>
        MemoryMarshal.Cast<byte, float>(MemoryMarshal.AsBytes(
            MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in v), 1)));

    private static unsafe void SetTransform(in Matrix4 m)
    {
        var span = AsSpan(in m);
        fixed (float* ptr = span)
            Bgfx.SetTransform(ptr, 1);
    }

    private static unsafe void SetViewTransform(ushort id, in Matrix4 view, in Matrix4 proj)
    {
        var vSpan = AsSpan(in view);
        var pSpan = AsSpan(in proj);
        fixed (float* vPtr = vSpan)
        fixed (float* pPtr = pSpan)
            Bgfx.SetViewTransform(id, vPtr, pPtr);
    }

    private static unsafe void SetUniform(Uniform uniform, in Vector4 v)
    {
        var span = AsSpan(in v);
        fixed (float* ptr = span)
            Bgfx.SetUniform(uniform, ptr, 1);
    }

    // Render メソッドは PmxRenderer.Render.cs へ移動

    public void Dispose()
    {
        foreach (var rm in _meshes)
        {
            rm.VertexBuffer?.Dispose();
            rm.VertexBuffer = null;
            rm.IndexBuffer?.Dispose();
            rm.IndexBuffer = null;
            rm.Texture?.Dispose();
            rm.Texture = null;
            rm.ColorUniform?.Dispose();
            rm.ColorUniform = null;
            rm.SpecularUniform?.Dispose();
            rm.SpecularUniform = null;
            rm.EdgeUniform?.Dispose();
            rm.EdgeUniform = null;
            rm.ToonColorUniform?.Dispose();
            rm.ToonColorUniform = null;
            rm.TextureTintUniform?.Dispose();
            rm.TextureTintUniform = null;
            rm.TextureUniform?.Dispose();
            rm.TextureUniform = null;
        }
        _meshes.Clear();
        _indexToHumanoidName.Clear();
        _lightDirUniform?.Dispose();
        _lightDirUniform = null;
        _lightColorUniform?.Dispose();
        _lightColorUniform = null;
        _shadeParamUniform?.Dispose();
        _shadeParamUniform = null;
        _program?.Dispose();
        _modelProgram?.Dispose();
    }
}
