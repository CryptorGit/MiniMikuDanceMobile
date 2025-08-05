using System;
using OpenTK.Graphics.ES30;
using GL = OpenTK.Graphics.ES30.GL;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using MiniMikuDance.Util;
using MiniMikuDance.App;
using MiniMikuDance.Import;
using MiniMikuDance.Motion;
using MMDTools;
using Vector2 = OpenTK.Mathematics.Vector2;
using Vector3 = OpenTK.Mathematics.Vector3;
using Vector4 = OpenTK.Mathematics.Vector4;
using Matrix4 = OpenTK.Mathematics.Matrix4;
using Quaternion = OpenTK.Mathematics.Quaternion;
using MathHelper = OpenTK.Mathematics.MathHelper;

namespace MiniMikuDanceMaui;

public class PmxRenderer : IDisposable
{
    private int _program;
    private class RenderMesh
    {
        public int Vao;
        public int Vbo;
        public int Ebo;
        public int IndexCount;
        public Vector4 Color = Vector4.One;
        public int Texture;
        public bool HasTexture;
        public Vector3[] Vertices = Array.Empty<Vector3>();
        public Vector3[] Normals = Array.Empty<Vector3>();
        public Vector2[] TexCoords = Array.Empty<Vector2>();
        public Vector4[] JointIndices = Array.Empty<Vector4>();
        public Vector4[] JointWeights = Array.Empty<Vector4>();
        public int[] BaseVertexIndices = Array.Empty<int>();
    }
    private readonly System.Collections.Generic.List<RenderMesh> _meshes = new();
    private int _gridVao;
    private int _gridVbo;
    private int _gridVertexCount;
    private int _modelLoc;
    private int _viewLoc;
    private int _projLoc;
    private int _colorLoc;
    private float _orbitX;
    // 初期カメラ位置はモデルの正面が表示されるようY軸回転を180度に設定
    private float _orbitY = MathF.PI;
    private float _distance = 4f;
    public float Distance
    {
        get => _distance;
        set => _distance = Math.Clamp(value, 0f, _stageSize);
    }
    // モデル中心より少し高い位置を基準にカメラを配置する
    private Vector3 _target = new Vector3(0f, 0.5f, 0f);
    private int _groundVao;
    private int _groundVbo;
    private int _groundVertexCount;
    private int _boneVao;
    private int _boneVbo;
    private int _boneVertexCount;
    private int _ikBoneVao;
    private int _ikBoneVbo;
    private readonly List<int> _ikBoneOffsets = new();
    private readonly List<int> _ikBoneCounts = new();
    private readonly List<int> _ikBoneIndices = new();
    private readonly List<Vector3> _ikTargets = new();
    private readonly List<bool> _ikGoalEnabled = new();
    private readonly List<int> _activeIkGoals = new();
    private const int IkBoneSegments = 16;
    private static readonly Vector4 IkBoneColor = new(1f, 1f, 0f, 1f);
    private static readonly Vector4 IkBoneSelectedColor = new(0f, 1f, 0f, 1f);
    private int _selectedIkBone = -1;
    private float _ikBoneRadius = 0.05f;
    public float IkBoneRadius
    {
        get => _ikBoneRadius;
        set
        {
            if (value > 0f && MathF.Abs(_ikBoneRadius - value) > float.Epsilon)
            {
                _ikBoneRadius = value;
                RebuildIkBoneMesh();
            }
        }
    }
    private int _modelProgram;
    private int _modelViewLoc;
    private int _modelProjLoc;
    private int _modelMatrixLoc;
    private int _modelColorLoc;
    private int _modelTexLoc;
    private int _modelUseTexLoc;
    private int _modelLightDirLoc;
    private int _modelViewDirLoc;
    private int _modelShadeShiftLoc;
    private int _modelShadeToonyLoc;
    private int _modelRimIntensityLoc;
    private int _modelAmbientLoc;
    private Matrix4 _modelTransform = Matrix4.Identity;
    public Matrix4 ModelTransform
    {
        get => _modelTransform;
        set => _modelTransform = value;
    }
    private int _width;
    private int _height;
    private readonly List<Vector3> _boneRotations = new();
    private readonly List<Vector3> _boneTranslations = new();
    private List<BoneData> _bones = new();
    private readonly Dictionary<int, string> _indexToHumanoidName = new();
    private readonly Dictionary<string, float> _morphWeights = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<(int Index, Vector3 Offset)>> _morphOffsets = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<(string Name, float Weight)>> _groupMorphs = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _morphOrder = new();
    private readonly HashSet<string> _morphOrderSet = new(StringComparer.OrdinalIgnoreCase);
    private Vector3[] _baseVertices = Array.Empty<Vector3>();
    private Vector3[] _morphedVertices = Array.Empty<Vector3>();
    private System.Numerics.Matrix4x4[] _worldMats = Array.Empty<System.Numerics.Matrix4x4>();
    public BonesConfig? BonesConfig { get; set; }
    private Quaternion _externalRotation = Quaternion.Identity;
    // 回転感度のデフォルトはスライダーの最小値に合わせる
    public float RotateSensitivity { get; set; } = 0.1f;
    public float PanSensitivity { get; set; } = 1f;
    private const float ZoomSensitivity = 0.1f;
    private const float MinStageSize = 0.1f;
    public bool CameraLocked { get; set; }
    public float ShadeShift { get; set; } = -0.1f;
    public float ShadeToony { get; set; } = 0.9f;
    public float RimIntensity { get; set; } = 0.5f;
    public float Ambient { get; set; } = 0.3f;
    public bool ShowBoneOutline { get; set; }
    public bool ShowIkBones { get; set; }

    private float _stageSize = AppSettings.DefaultStageSize;
    public float StageSize
    {
        get => _stageSize;
        set
        {
            value = MathF.Max(value, MinStageSize);
            if (_stageSize != value)
            {
                _stageSize = value;
                _distance = Math.Min(_distance, _stageSize);
                _defaultCameraDistance = Math.Min(_defaultCameraDistance, _stageSize);
                if (_program != 0)
                    GenerateGrid();
            }
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

    public void Initialize()
    {
        const string vert = @"#version 300 es
layout(location = 0) in vec3 aPosition;
uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProj;
void main(){
    gl_Position = uProj * uView * uModel * vec4(aPosition,1.0);
}";
        const string frag = @"#version 300 es
precision mediump float;
uniform vec4 uColor;
out vec4 FragColor;
void main(){
    FragColor = uColor;
}";
        int vs = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vs, vert);
        GL.CompileShader(vs);
        int fs = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fs, frag);
        GL.CompileShader(fs);
        _program = GL.CreateProgram();
        GL.AttachShader(_program, vs);
        GL.AttachShader(_program, fs);
        GL.LinkProgram(_program);
        GL.DeleteShader(vs);
        GL.DeleteShader(fs);
        _modelLoc = GL.GetUniformLocation(_program, "uModel");
        _viewLoc = GL.GetUniformLocation(_program, "uView");
        _projLoc = GL.GetUniformLocation(_program, "uProj");
        _colorLoc = GL.GetUniformLocation(_program, "uColor");

        // 透過描画設定（デフォルトでは無効）
        GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

        const string modelVert = @"#version 300 es
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aTex;
uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProj;
out vec3 vNormal;
out vec2 vTex;
void main(){
    vec4 pos = uModel * vec4(aPosition,1.0);
    vNormal = mat3(uModel) * aNormal;
    vTex = aTex;
    gl_Position = uProj * uView * pos;
}";
        const string modelFrag = @"#version 300 es
precision mediump float;
in vec3 vNormal;
in vec2 vTex;
uniform vec4 uColor;
uniform sampler2D uTex;
uniform bool uUseTex;
uniform vec3 uLightDir;
uniform vec3 uViewDir;
uniform float uShadeShift;
uniform float uShadeToony;
uniform float uRimIntensity;
uniform float uAmbient;
out vec4 FragColor;
void main(){
    vec4 base = uUseTex ? texture(uTex, vTex) : uColor;
    float ndotl = max(dot(normalize(vNormal), normalize(uLightDir)), 0.0);
    float light = clamp((ndotl + uShadeShift) * uShadeToony, 0.0, 1.0);
    float rim = pow(1.0 - max(dot(normalize(vNormal), normalize(uViewDir)), 0.0), 3.0) * uRimIntensity;
    vec3 color = base.rgb * (light + uAmbient) + base.rgb * rim;
    FragColor = vec4(color, base.a);
}";
        int mvs = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(mvs, modelVert);
        GL.CompileShader(mvs);
        int mfs = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(mfs, modelFrag);
        GL.CompileShader(mfs);
        _modelProgram = GL.CreateProgram();
        GL.AttachShader(_modelProgram, mvs);
        GL.AttachShader(_modelProgram, mfs);
        GL.LinkProgram(_modelProgram);
        GL.DeleteShader(mvs);
        GL.DeleteShader(mfs);
        _modelViewLoc = GL.GetUniformLocation(_modelProgram, "uView");
        _modelProjLoc = GL.GetUniformLocation(_modelProgram, "uProj");
        _modelMatrixLoc = GL.GetUniformLocation(_modelProgram, "uModel");
        _modelColorLoc = GL.GetUniformLocation(_modelProgram, "uColor");
        _modelTexLoc = GL.GetUniformLocation(_modelProgram, "uTex");
        _modelUseTexLoc = GL.GetUniformLocation(_modelProgram, "uUseTex");
        _modelLightDirLoc = GL.GetUniformLocation(_modelProgram, "uLightDir");
        _modelViewDirLoc = GL.GetUniformLocation(_modelProgram, "uViewDir");
        _modelShadeShiftLoc = GL.GetUniformLocation(_modelProgram, "uShadeShift");
        _modelShadeToonyLoc = GL.GetUniformLocation(_modelProgram, "uShadeToony");
        _modelRimIntensityLoc = GL.GetUniformLocation(_modelProgram, "uRimIntensity");
        _modelAmbientLoc = GL.GetUniformLocation(_modelProgram, "uAmbient");

        GenerateGrid();
    }

    private void GenerateGrid()
    {
        float range = _stageSize;
        int divisions = (int)(range * 2f) + 1;
        _gridVertexCount = divisions * 4;
        float[] grid = new float[_gridVertexCount * 3];
        int idx = 0;
        for (int i = 0; i < divisions; i++)
        {
            float pos = -range + i;
            grid[idx++] = pos; grid[idx++] = 0f; grid[idx++] = -range;
            grid[idx++] = pos; grid[idx++] = 0f; grid[idx++] = range;
            grid[idx++] = -range; grid[idx++] = 0f; grid[idx++] = pos;
            grid[idx++] = range; grid[idx++] = 0f; grid[idx++] = pos;
        }
        if (_gridVao == 0) _gridVao = GL.GenVertexArray();
        if (_gridVbo == 0) _gridVbo = GL.GenBuffer();
        GL.BindVertexArray(_gridVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _gridVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, grid.Length * sizeof(float), grid, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);

        float[] plane =
        {
            -range, 0f, -range,
             range, 0f, -range,
            -range, 0f,  range,
             range, 0f, -range,
             range, 0f,  range,
            -range, 0f,  range
        };
        _groundVertexCount = 6;
        if (_groundVao == 0) _groundVao = GL.GenVertexArray();
        if (_groundVbo == 0) _groundVbo = GL.GenBuffer();
        GL.BindVertexArray(_groundVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _groundVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, plane.Length * sizeof(float), plane, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
        GL.Viewport(0, 0, width, height);
    }

    public void Orbit(float dx, float dy)
    {
        if (CameraLocked) return;
        _orbitY -= dx * 0.01f * RotateSensitivity;
        _orbitX -= dy * 0.01f * RotateSensitivity;
    }

    public void Pan(float dx, float dy)
    {
        if (CameraLocked) return;
        Matrix4 rot = Matrix4.CreateFromQuaternion(_externalRotation) *
                      Matrix4.CreateRotationX(_orbitX) *
                      Matrix4.CreateRotationY(_orbitY);
        Vector3 right = Vector3.TransformNormal(Vector3.UnitX, rot);
        Vector3 up = Vector3.TransformNormal(Vector3.UnitY, rot);
        _target += (-right * dx + up * dy) * 0.01f * PanSensitivity;
    }

    public void Dolly(float delta)
    {
        if (CameraLocked) return;
        var newDist = _distance * (1f + delta * 0.01f * ZoomSensitivity);
        Distance = newDist;
    }

    public void ResetCamera()
    {
        _orbitX = 0f;
        _orbitY = MathF.PI;
        _target = new Vector3(0f, _defaultCameraTargetY, 0f);
        Distance = _defaultCameraDistance;
        _externalRotation = Quaternion.Identity;
    }

    public void SetExternalRotation(Quaternion q)
    {
        _externalRotation = q;
    }

    public void ClearBoneRotations()
    {
        _boneRotations.Clear();
        _boneTranslations.Clear();
    }

    public void SetIkGoalEnabled(int index, bool enable)
    {
        while (_ikGoalEnabled.Count <= index)
            _ikGoalEnabled.Add(true);
        _ikGoalEnabled[index] = enable;
        if (ShowIkBones)
            RebuildIkBoneMesh();
    }

    public bool GetIkGoalEnabled(int index)
        => index < _ikGoalEnabled.Count ? _ikGoalEnabled[index] : true;

    public IList<(int Index, string Name, bool Enabled)> GetIkGoals()
    {
        var list = new List<(int, string, bool)>();
        foreach (var i in _activeIkGoals)
        {
            if (i < 0 || i >= _bones.Count || i >= _ikTargets.Count)
                continue;
            var bone = _bones[i];
            var name = _indexToHumanoidName.TryGetValue(bone.IkTargetIndex, out var n) ? n : bone.Name;
            bool enabled = i < _ikGoalEnabled.Count ? _ikGoalEnabled[i] : true;
            list.Add((i, name, enabled));
        }
        return list;
    }

    public IList<(int Index, string Name)> GetAvailableIkGoals()
    {
        var list = new List<(int, string)>();
        for (int i = 0; i < _bones.Count && i < _ikTargets.Count; i++)
        {
            var bone = _bones[i];
            if (!bone.IsIk || _activeIkGoals.Contains(i))
                continue;
            var name = _indexToHumanoidName.TryGetValue(bone.IkTargetIndex, out var n) ? n : bone.Name;
            list.Add((i, name));
        }
        return list;
    }

    public void AddIkGoal(int index)
    {
        if (index < 0 || index >= _bones.Count)
            return;
        if (!_bones[index].IsIk || _activeIkGoals.Contains(index))
            return;
        _activeIkGoals.Add(index);
        if (ShowIkBones)
            RebuildIkBoneMesh();
    }

    public void RemoveIkGoal(int index)
    {
        if (_activeIkGoals.Remove(index) && ShowIkBones)
            RebuildIkBoneMesh();
    }

    public PoseSnapshot SavePose()
    {
        var snap = new PoseSnapshot
        {
            IkTargets = _ikTargets.Select(v => v.ToNumerics()).ToList(),
            IkEnabled = _ikGoalEnabled.ToList(),
            IkGoalIndices = _activeIkGoals.ToList(),
            BoneRotations = _boneRotations.Select(v => v.ToNumerics()).ToList(),
            BoneTranslations = _boneTranslations.Select(v => v.ToNumerics()).ToList()
        };
        return snap;
    }

    public void LoadPose(PoseSnapshot snap)
    {
        _ikTargets.Clear();
        _ikTargets.AddRange(snap.IkTargets.Select(v => v.ToOpenTK()));
        _ikGoalEnabled.Clear();
        _ikGoalEnabled.AddRange(snap.IkEnabled);
        _activeIkGoals.Clear();
        if (snap.IkGoalIndices.Count > 0)
            _activeIkGoals.AddRange(snap.IkGoalIndices);
        else
        {
            for (int i = 0; i < _bones.Count; i++)
                if (_bones[i].IsIk)
                    _activeIkGoals.Add(i);
        }
        while (_ikGoalEnabled.Count < _ikTargets.Count)
            _ikGoalEnabled.Add(true);
        _boneRotations.Clear();
        _boneRotations.AddRange(snap.BoneRotations.Select(v => v.ToOpenTK()));
        _boneTranslations.Clear();
        _boneTranslations.AddRange(snap.BoneTranslations.Select(v => v.ToOpenTK()));
        if (ShowIkBones)
            RebuildIkBoneMesh();
    }

    public void SetBoneRotation(int index, Vector3 degrees)
    {
        if (index < 0)
            return;
        if (BonesConfig != null && index < _bones.Count)
        {
            var name = _indexToHumanoidName.TryGetValue(index, out var n) ? n : _bones[index].Name;
            var clamped = BonesConfig.Clamp(name, degrees.ToNumerics());
            degrees = clamped.ToOpenTK();
        }
        while (_boneRotations.Count <= index)
            _boneRotations.Add(Vector3.Zero);
        _boneRotations[index] = degrees;
    }

    public void SetBoneTranslation(int index, Vector3 translation)
    {
        if (index < 0)
            return;
        while (_boneTranslations.Count <= index)
            _boneTranslations.Add(Vector3.Zero);
        _boneTranslations[index] = translation;
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

    public void SetMorphWeight(string name, float weight)
    {
        _morphWeights[name] = weight;
        if (_baseVertices.Length == 0)
            return;

        Array.Copy(_baseVertices, _morphedVertices, _baseVertices.Length);

        var effective = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in _morphWeights)
        {
            if (!_groupMorphs.ContainsKey(kv.Key))
                effective[kv.Key] = kv.Value;
        }
        foreach (var kv in _morphWeights)
        {
            if (_groupMorphs.ContainsKey(kv.Key) && MathF.Abs(kv.Value) > 1e-6f)
                AddGroupWeight(kv.Key, kv.Value, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        }

        void AddGroupWeight(string groupName, float weight, HashSet<string> visited)
        {
            if (!visited.Add(groupName))
                return;
            if (!_groupMorphs.TryGetValue(groupName, out var children))
            {
                visited.Remove(groupName);
                return;
            }
            foreach (var (childName, childWeight) in children)
            {
                float w = weight * childWeight;
                if (_groupMorphs.ContainsKey(childName))
                    AddGroupWeight(childName, w, visited);
                else
                {
                    if (effective.TryGetValue(childName, out var ex))
                        effective[childName] = ex + w;
                    else
                        effective[childName] = w;
                }
            }
            visited.Remove(groupName);
        }

        foreach (var morphName in _morphOrder)
        {
            float w = effective.TryGetValue(morphName, out var val) ? val : 0f;
            if (MathF.Abs(w) < 1e-6f)
                continue;
            if (_morphOffsets.TryGetValue(morphName, out var offsets))
            {
                foreach (var (idx, offset) in offsets)
                {
                    if (idx >= 0 && idx < _morphedVertices.Length)
                        _morphedVertices[idx] += offset * w;
                }
            }
        }

        foreach (var rm in _meshes)
        {
            if (rm.BaseVertexIndices.Length != rm.Vertices.Length)
                continue;
            for (int i = 0; i < rm.Vertices.Length; i++)
            {
                int bi = rm.BaseVertexIndices[i];
                if (bi >= 0 && bi < _morphedVertices.Length)
                    rm.Vertices[i] = _morphedVertices[bi];
            }
        }
        SolveIk();
        UpdateSkinningBuffers();
    }

    public float GetMorphWeight(string name)
    {
        return _morphWeights.TryGetValue(name, out var weight) ? weight : 0f;
    }

    public IList<Vector3> GetAllBoneRotations() => _boneRotations.ToList();

    public IList<Vector3> GetAllBoneTranslations() => _boneTranslations.ToList();

    public void SetAllBoneRotations(IList<Vector3> list)
    {
        _boneRotations.Clear();
        _boneRotations.AddRange(list);
    }

    public void SetAllBoneTranslations(IList<Vector3> list)
    {
        _boneTranslations.Clear();
        _boneTranslations.AddRange(list);
    }

    private void SolveIk()
    {
        if (_bones.Count == 0 || _ikTargets.Count == 0)
            return;

        var tempBones = new List<BoneData>(_bones.Count);
        for (int i = 0; i < _bones.Count; i++)
        {
            var b = _bones[i];
            tempBones.Add(new BoneData
            {
                Name = b.Name,
                Parent = b.Parent,
                Rotation = b.Rotation,
                Translation = b.Translation,
                BindMatrix = b.BindMatrix,
                InverseBindMatrix = b.InverseBindMatrix,
                IsIk = b.IsIk,
                IkTargetIndex = b.IkTargetIndex,
                IkChainIndices = new List<int>(b.IkChainIndices),
                IkLoopCount = b.IkLoopCount,
                IkAngleLimit = b.IkAngleLimit,
                TwistWeight = b.TwistWeight
            });
        }

        for (int i = 0; i < tempBones.Count; i++)
        {
            if (i < _boneRotations.Count)
            {
                var e = _boneRotations[i].ToNumerics();
                tempBones[i].Rotation *= e.FromEulerDegrees();
            }
            if (i < _boneTranslations.Count)
            {
                tempBones[i].Translation += _boneTranslations[i].ToNumerics();
            }
        }

        List<int> torso = new();
        List<int> head = new();
        List<int> legs = new();
        List<int> arms = new();
        int hipIkIndex = -1;
        foreach (var i in _activeIkGoals)
        {
            if (i >= tempBones.Count || i >= _ikTargets.Count)
                continue;
            if (!GetIkGoalEnabled(i))
                continue;
            var b = tempBones[i];

            // ik_hip は CCD ではなく直接平行移動させる
            if (string.Equals(b.Name, "ik_hip", StringComparison.OrdinalIgnoreCase))
            {
                hipIkIndex = i;
                continue;
            }

            string name = _indexToHumanoidName.TryGetValue(b.IkTargetIndex, out var n) ? n.ToLower() : string.Empty;
            if (name.Contains("head") || name.Contains("neck"))
                head.Add(i);
            else if (name.Contains("leg") || name.Contains("foot"))
                legs.Add(i);
            else if (name.Contains("arm") || name.Contains("hand"))
                arms.Add(i);
            else
                torso.Add(i);
        }

        void SolveList(List<int> list)
        {
            foreach (var idx in list)
            {
                var b = tempBones[idx];
                var chain = new List<int>(b.IkChainIndices);
                chain.Reverse();
                chain.Add(b.IkTargetIndex);
                var target = _ikTargets[idx].ToNumerics();
                IKSolver.SolveChain(tempBones, chain, target);
            }
        }

        SolveList(torso);
        SolveList(head);
        SolveList(legs);
        SolveList(arms);

        if (hipIkIndex >= 0)
        {
            var hipTarget = _ikTargets[hipIkIndex].ToNumerics();
            int hipBoneIdx = tempBones[hipIkIndex].IkTargetIndex;
            if (hipBoneIdx >= 0 && hipBoneIdx < tempBones.Count)
                tempBones[hipBoneIdx].Translation = hipTarget;
        }

        _boneRotations.Clear();
        _boneTranslations.Clear();
        for (int i = 0; i < tempBones.Count; i++)
        {
            var deltaQuat = tempBones[i].Rotation * System.Numerics.Quaternion.Inverse(_bones[i].Rotation);
            var euler = deltaQuat.ToEulerDegrees().ToOpenTK();
            _boneRotations.Add(euler);
            var trans = (tempBones[i].Translation - _bones[i].Translation).ToOpenTK();
            _boneTranslations.Add(trans);
        }

        TwistCorrection.DistributeSpineTwist(_boneRotations, _indexToHumanoidName);
        TwistCorrection.DistributeLimbTwist(_boneRotations, _indexToHumanoidName);

        if (ShowIkBones)
            RebuildIkBoneMesh();
    }

    private void UpdateSkinningBuffers()
    {
        if (_bones.Count == 0)
            return;

        var worldMats = new System.Numerics.Matrix4x4[_bones.Count];
        for (int i = 0; i < _bones.Count; i++)
        {
            var bone = _bones[i];
            System.Numerics.Vector3 euler = i < _boneRotations.Count ? _boneRotations[i].ToNumerics() : System.Numerics.Vector3.Zero;
            var delta = euler.FromEulerDegrees();
            System.Numerics.Vector3 trans = bone.Translation;
            if (i < _boneTranslations.Count)
                trans += _boneTranslations[i].ToNumerics();
            var local = System.Numerics.Matrix4x4.CreateFromQuaternion(bone.Rotation * delta) *
                        System.Numerics.Matrix4x4.CreateTranslation(trans);
            if (bone.Parent >= 0)
                worldMats[i] = local * worldMats[bone.Parent];
            else
                worldMats[i] = local;
        }
        _worldMats = worldMats;

        var skinMats = new System.Numerics.Matrix4x4[_bones.Count];
        for (int i = 0; i < _bones.Count; i++)
            skinMats[i] = _bones[i].InverseBindMatrix * worldMats[i];

        foreach (var rm in _meshes)
        {
            if (rm.JointIndices.Length != rm.Vertices.Length)
                continue;
            float[] buf = new float[rm.Vertices.Length * 8];
            for (int vi = 0; vi < rm.Vertices.Length; vi++)
            {
                var pos = System.Numerics.Vector3.Zero;
                var norm = System.Numerics.Vector3.Zero;
                var jp = rm.JointIndices[vi];
                var jw = rm.JointWeights[vi];
                for (int k = 0; k < 4; k++)
                {
                    int bi = (int)jp[k];
                    float w = jw[k];
                    if (bi >= 0 && bi < skinMats.Length && w > 0f)
                    {
                        var m = skinMats[bi];
                        pos += System.Numerics.Vector3.Transform(rm.Vertices[vi].ToNumerics(), m) * w;
                        norm += System.Numerics.Vector3.TransformNormal(rm.Normals[vi].ToNumerics(), m) * w;
                    }
                }
                if (norm.LengthSquared() > 0)
                    norm = System.Numerics.Vector3.Normalize(norm);

                buf[vi * 8 + 0] = pos.X;
                buf[vi * 8 + 1] = pos.Y;
                buf[vi * 8 + 2] = pos.Z;
                buf[vi * 8 + 3] = norm.X;
                buf[vi * 8 + 4] = norm.Y;
                buf[vi * 8 + 5] = norm.Z;
                if (vi < rm.TexCoords.Length)
                {
                    buf[vi * 8 + 6] = rm.TexCoords[vi].X;
                    buf[vi * 8 + 7] = rm.TexCoords[vi].Y;
                }
                else
                {
                    buf[vi * 8 + 6] = 0f;
                    buf[vi * 8 + 7] = 0f;
                }
            }
            GL.BindBuffer(BufferTarget.ArrayBuffer, rm.Vbo);
            var handle = GCHandle.Alloc(buf, GCHandleType.Pinned);
            try
            {
                GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, buf.Length * sizeof(float), handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }
    }

    public void RebuildIkBoneMesh()
    {
        if (!ShowIkBones)
        {
            _ikBoneOffsets.Clear();
            _ikBoneCounts.Clear();
            return;
        }

        var modelMat = _modelTransform;
        var verts = new List<float>();
        _ikBoneOffsets.Clear();
        _ikBoneCounts.Clear();
        _ikBoneIndices.Clear();
        float ikRadius = IkBoneRadius;
        if (_defaultCameraDistance > 0f)
            ikRadius *= _distance / _defaultCameraDistance;
        foreach (var i in _activeIkGoals)
        {
            if (i < 0 || i >= _bones.Count || i >= _ikTargets.Count)
                continue;
            if (!GetIkGoalEnabled(i))
                continue;
            var cp = _ikTargets[i];
            var c4 = Vector4.TransformRow(new Vector4(cp.X, cp.Y, cp.Z, 1f), modelMat);
            _ikBoneOffsets.Add(verts.Count / 3);
            verts.Add(c4.X); verts.Add(c4.Y); verts.Add(c4.Z);
            for (int s = 0; s <= IkBoneSegments; s++)
            {
                float ang = 2f * MathF.PI * s / IkBoneSegments;
                float x = c4.X + ikRadius * MathF.Cos(ang);
                float y = c4.Y + ikRadius * MathF.Sin(ang);
                float z = c4.Z;
                verts.Add(x); verts.Add(y); verts.Add(z);
            }
            _ikBoneCounts.Add(IkBoneSegments + 2);
            _ikBoneIndices.Add(i);
        }
        if (verts.Count > 0)
        {
            if (_ikBoneVao == 0) _ikBoneVao = GL.GenVertexArray();
            if (_ikBoneVbo == 0) _ikBoneVbo = GL.GenBuffer();
            GL.BindVertexArray(_ikBoneVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _ikBoneVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, verts.Count * sizeof(float), verts.ToArray(), BufferUsageHint.DynamicDraw);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }
    }

    public IList<(int Index, Vector3 World, Vector2 Screen)> GetIkBonePositions()
    {
        var list = new List<(int, Vector3, Vector2)>();
        if (_bones.Count == 0)
            return list;

        Matrix4 rot = Matrix4.CreateFromQuaternion(_externalRotation) *
                      Matrix4.CreateRotationX(_orbitX) *
                      Matrix4.CreateRotationY(_orbitY);
        Vector3 cam = Vector3.TransformPosition(new Vector3(0, 0, _distance), rot) + _target;
        Matrix4 view = Matrix4.LookAt(cam, _target, Vector3.UnitY);
        float aspect = _width == 0 || _height == 0 ? 1f : _width / (float)_height;
        Matrix4 proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspect, 0.1f, 1000f);
        var modelMat = _modelTransform;

        foreach (var i in _activeIkGoals)
        {
            if (i < 0 || i >= _bones.Count || i >= _ikTargets.Count)
                continue;
            if (!GetIkGoalEnabled(i))
                continue;
            var cp = _ikTargets[i];
            var p = new Vector4(cp.X, cp.Y, cp.Z, 1f);
            p = Vector4.TransformRow(p, modelMat);
            p = Vector4.TransformRow(p, view);
            p = Vector4.TransformRow(p, proj);
            if (p.W != 0f)
            {
                float ndcX = p.X / p.W;
                float ndcY = p.Y / p.W;
                float sx = (ndcX * 0.5f + 0.5f) * _width;
                float sy = (1f - (ndcY * 0.5f + 0.5f)) * _height;
                list.Add((i, cp, new Vector2(sx, sy)));
            }
        }
        return list;
    }

    public Vector3 ProjectScreenPointToViewPlane(float sx, float sy, Vector3 planePoint)
    {
        Matrix4 rot = Matrix4.CreateFromQuaternion(_externalRotation) *
                      Matrix4.CreateRotationX(_orbitX) *
                      Matrix4.CreateRotationY(_orbitY);
        Vector3 cam = Vector3.TransformPosition(new Vector3(0, 0, _distance), rot) + _target;
        Matrix4 view = Matrix4.LookAt(cam, _target, Vector3.UnitY);
        float aspect = _width == 0 || _height == 0 ? 1f : _width / (float)_height;
        Matrix4 proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspect, 0.1f, 1000f);
        var modelMat = _modelTransform;
        Matrix4 mvp = modelMat * view * proj;
        Matrix4.Invert(mvp, out var invMvp);

        float ndcX = (2f * sx / _width) - 1f;
        float ndcY = 1f - (2f * sy / _height);
        var near4 = new Vector4(ndcX, ndcY, -1f, 1f);
        var far4 = new Vector4(ndcX, ndcY, 1f, 1f);
        near4 = Vector4.TransformRow(near4, invMvp);
        far4 = Vector4.TransformRow(far4, invMvp);
        if (near4.W != 0f) near4 /= near4.W;
        if (far4.W != 0f) far4 /= far4.W;
        var rayOrigin = new Vector3(near4.X, near4.Y, near4.Z);
        var rayDir = Vector3.Normalize(new Vector3(far4.X, far4.Y, far4.Z) - rayOrigin);
        Vector3 planeNormal = Vector3.Normalize(planePoint - cam);
        float denom = Vector3.Dot(rayDir, planeNormal);
        if (MathF.Abs(denom) < 1e-5f)
            return planePoint;
        float t = Vector3.Dot(planePoint - rayOrigin, planeNormal) / denom;
        return rayOrigin + rayDir * t;
    }

    public void SetIkTargetPosition(int index, Vector3 pos)
    {
        if (index < 0 || index >= _bones.Count)
            return;
        var bone = _bones[index];
        if (!bone.IsIk || !_activeIkGoals.Contains(index))
            return;

        while (_ikTargets.Count <= index)
        {
            _ikTargets.Add(Vector3.Zero);
            _ikGoalEnabled.Add(true);
        }
        _ikTargets[index] = pos;
    }

    public void SetSelectedIkBone(int index)
    {
        _selectedIkBone = index;
    }

    public void LoadModel(ModelData data)
    {
        foreach (var rm in _meshes)
        {
            if (rm.Vao != 0) GL.DeleteVertexArray(rm.Vao);
            if (rm.Vbo != 0) GL.DeleteBuffer(rm.Vbo);
            if (rm.Ebo != 0) GL.DeleteBuffer(rm.Ebo);
            if (rm.Texture != 0) GL.DeleteTexture(rm.Texture);
        }
        _meshes.Clear();
        _indexToHumanoidName.Clear();
        _morphWeights.Clear();
        _morphOffsets.Clear();
        _groupMorphs.Clear();
        _morphOrder.Clear();
        _morphOrderSet.Clear();
        _baseVertices = data.Mesh.Vertices.Select(v => new Vector3(v.X, v.Y, v.Z)).ToArray();
        _morphedVertices = (Vector3[])_baseVertices.Clone();
        foreach (var morph in data.Morphs)
        {
            if (morph == null)
                continue;

            if (!_morphOrderSet.Contains(morph.Name))
            {
                _morphOrder.Add(morph.Name);
                _morphOrderSet.Add(morph.Name);
            }

            if (morph.Type == MorphType.Group)
            {
                _groupMorphs[morph.Name] = morph.GroupChildren?.ToList() ?? new List<(string Name, float Weight)>();
                if (!_morphWeights.ContainsKey(morph.Name))
                    _morphWeights[morph.Name] = 0f;
                continue;
            }

            if (_morphOffsets.TryGetValue(morph.Name, out var existing))
            {
                var dict = existing.ToDictionary(e => e.Index, e => e.Offset);
                foreach (var mo in morph.Offsets)
                {
                    var offset = new Vector3(mo.Offset.X, mo.Offset.Y, mo.Offset.Z);
                    if (dict.TryGetValue(mo.Index, out var ex))
                        dict[mo.Index] = ex + offset;
                    else
                        dict[mo.Index] = offset;
                }
                _morphOffsets[morph.Name] = dict.Select(kv => (kv.Key, kv.Value)).ToList();
            }
            else
            {
                var list = new List<(int, Vector3)>();
                foreach (var mo in morph.Offsets)
                    list.Add((mo.Index, new Vector3(mo.Offset.X, mo.Offset.Y, mo.Offset.Z)));
                _morphOffsets[morph.Name] = list;
            }

            if (!_morphWeights.ContainsKey(morph.Name))
                _morphWeights[morph.Name] = 0f;
        }
        _bones = data.Bones.ToList();
        foreach (var (name, idx) in data.HumanoidBoneList)
        {
            _indexToHumanoidName[idx] = name;
        }

        _ikTargets.Clear();
        _ikGoalEnabled.Clear();
        _activeIkGoals.Clear();
        var initWorld = new System.Numerics.Matrix4x4[_bones.Count];
        for (int i = 0; i < _bones.Count; i++)
        {
            var b = _bones[i];
            var local = System.Numerics.Matrix4x4.CreateFromQuaternion(b.Rotation) *
                        System.Numerics.Matrix4x4.CreateTranslation(b.Translation);
            if (b.Parent >= 0)
                initWorld[i] = local * initWorld[b.Parent];
            else
                initWorld[i] = local;
            _ikTargets.Add(new Vector3(initWorld[i].Translation.X, initWorld[i].Translation.Y, initWorld[i].Translation.Z));
            _ikGoalEnabled.Add(true);
            if (b.IsIk)
                _activeIkGoals.Add(i);
        }
        _worldMats = initWorld;

        _modelTransform = data.Transform.ToMatrix4();

        if (data.SubMeshes.Count == 0)
        {
            data.SubMeshes.Add(new SubMeshData
            {
                Mesh = data.Mesh
            });
        }

        foreach (var sm in data.SubMeshes)
        {
            int vcount = sm.Mesh.VertexCount;
            float[] verts = new float[vcount * 8];
            for (int i = 0; i < vcount; i++)
            {
                var v = sm.Mesh.Vertices[i];
                verts[i * 8 + 0] = v.X;
                verts[i * 8 + 1] = v.Y;
                verts[i * 8 + 2] = v.Z;
                if (i < sm.Mesh.Normals.Count)
                {
                    var n = sm.Mesh.Normals[i];
                    verts[i * 8 + 3] = n.X;
                    verts[i * 8 + 4] = n.Y;
                    verts[i * 8 + 5] = n.Z;
                }
                else
                {
                    verts[i * 8 + 3] = 0f;
                    verts[i * 8 + 4] = 0f;
                    verts[i * 8 + 5] = 1f;
                }
                if (i < sm.TexCoords.Count)
                {
                    var uv = sm.TexCoords[i];
                    verts[i * 8 + 6] = uv.X;
                    verts[i * 8 + 7] = uv.Y;
                }
                else
                {
                    verts[i * 8 + 6] = 0f;
                    verts[i * 8 + 7] = 0f;
                }
            }

            var indices = new System.Collections.Generic.List<uint>();
            foreach (var f in sm.Mesh.Faces)
            {
                foreach (var idx in f.Indices)
                    indices.Add((uint)idx);
            }

            var rm = new RenderMesh();
            rm.IndexCount = indices.Count;
            rm.Vertices = sm.Mesh.Vertices.Select(v => new Vector3(v.X, v.Y, v.Z)).ToArray();
            rm.Normals = sm.Mesh.Normals.Select(n => new Vector3(n.X, n.Y, n.Z)).ToArray();
            rm.TexCoords = sm.TexCoords.Select(t => new Vector2(t.X, t.Y)).ToArray();
            rm.JointIndices = sm.JointIndices.Select(j => new Vector4(j.X, j.Y, j.Z, j.W)).ToArray();
            rm.JointWeights = sm.JointWeights.Select(w => new Vector4(w.X, w.Y, w.Z, w.W)).ToArray();
            rm.BaseVertexIndices = sm.BaseVertexIndices.ToArray();
            rm.Vao = GL.GenVertexArray();
            rm.Vbo = GL.GenBuffer();
            rm.Ebo = GL.GenBuffer();
            rm.Color = sm.ColorFactor.ToVector4();

            GL.BindVertexArray(rm.Vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, rm.Vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, verts.Length * sizeof(float), verts, BufferUsageHint.StaticDraw);
            int stride = 8 * sizeof(float);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));
            GL.EnableVertexAttribArray(2);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, rm.Ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Count * sizeof(uint), indices.ToArray(), BufferUsageHint.StaticDraw);
            GL.BindVertexArray(0);

            if (sm.TextureBytes != null)
            {
                rm.Texture = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, rm.Texture);
                var handle = System.Runtime.InteropServices.GCHandle.Alloc(sm.TextureBytes, System.Runtime.InteropServices.GCHandleType.Pinned);
                try
                {
#pragma warning disable CS0618
                    GL.TexImage2D(
                        (All)TextureTarget.Texture2D,
                        0,
                        (All)PixelInternalFormat.Rgba,
                        sm.TextureWidth,
                        sm.TextureHeight,
                        0,
                        (All)PixelFormat.Rgba,
                        (All)PixelType.UnsignedByte,
                        handle.AddrOfPinnedObject());
#pragma warning restore CS0618
                }
                finally
                {
                    handle.Free();
                }
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                rm.HasTexture = true;
            }

            _meshes.Add(rm);
        }
    }

    public void Render()
    {
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
        GL.Enable(EnableCap.Blend); // 半透明描画のためブレンドを有効化
        GL.FrontFace(FrontFaceDirection.Ccw);
        GL.ClearColor(1f, 1f, 1f, 1f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        Matrix4 rot = Matrix4.CreateFromQuaternion(_externalRotation) *
                      Matrix4.CreateRotationX(_orbitX) *
                      Matrix4.CreateRotationY(_orbitY);
        Vector3 cam = Vector3.TransformPosition(new Vector3(0, 0, _distance), rot) + _target;
        Matrix4 view = Matrix4.LookAt(cam, _target, Vector3.UnitY);
        float aspect = _width == 0 || _height == 0 ? 1f : _width / (float)_height;
        Matrix4 proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspect, 0.1f, 1000f);
        var modelMat = ModelTransform;

        // CPU skinning: update vertex buffers based on current bone rotations
        if (_bones.Count > 0)
        {
            SolveIk();
            UpdateSkinningBuffers();

            if (ShowBoneOutline)
            {
                var lines = new List<float>();
                for (int i = 0; i < _bones.Count; i++)
                {
                    var bone = _bones[i];
                    if (bone.Parent >= 0)
                    {
                        var pp = _worldMats[bone.Parent].Translation;
                        var cp = _worldMats[i].Translation;
                        var p4 = Vector4.TransformRow(new Vector4(pp.X, pp.Y, pp.Z, 1f), modelMat);
                        var c4 = Vector4.TransformRow(new Vector4(cp.X, cp.Y, cp.Z, 1f), modelMat);
                        lines.Add(p4.X); lines.Add(p4.Y); lines.Add(p4.Z);
                        lines.Add(c4.X); lines.Add(c4.Y); lines.Add(c4.Z);
                    }
                }
                _boneVertexCount = lines.Count / 3;
                if (_boneVao == 0) _boneVao = GL.GenVertexArray();
                if (_boneVbo == 0) _boneVbo = GL.GenBuffer();
                GL.BindVertexArray(_boneVao);
                GL.BindBuffer(BufferTarget.ArrayBuffer, _boneVbo);
                GL.BufferData(BufferTarget.ArrayBuffer, lines.Count * sizeof(float), lines.ToArray(), BufferUsageHint.DynamicDraw);
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
                GL.EnableVertexAttribArray(0);
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
                GL.BindVertexArray(0);
            }
            else
            {
                _boneVertexCount = 0;
            }
        }

        GL.UseProgram(_modelProgram);
        GL.UniformMatrix4(_modelViewLoc, false, ref view);
        GL.UniformMatrix4(_modelProjLoc, false, ref proj);
        // ライトと視線方向をカメラ角度に合わせて更新
        Vector3 light = Vector3.Normalize(new Vector3(0.3f, 0.6f, -0.7f));
        light = Vector3.TransformNormal(light, rot);
        GL.Uniform3(_modelLightDirLoc, ref light);
        Vector3 viewDir = Vector3.Normalize(_target - cam);
        GL.Uniform3(_modelViewDirLoc, ref viewDir);
        GL.Uniform1(_modelShadeShiftLoc, ShadeShift);
        GL.Uniform1(_modelShadeToonyLoc, ShadeToony);
        GL.Uniform1(_modelRimIntensityLoc, RimIntensity);
        GL.Uniform1(_modelAmbientLoc, Ambient);
        GL.UniformMatrix4(_modelMatrixLoc, false, ref modelMat);

        void DrawMesh(RenderMesh rm)
        {
            GL.Uniform4(_modelColorLoc, rm.Color);
            if (rm.HasTexture)
            {
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, rm.Texture);
                GL.Uniform1(_modelTexLoc, 0);
                GL.Uniform1(_modelUseTexLoc, 1);
            }
            else
            {
                GL.Uniform1(_modelUseTexLoc, 0);
            }
            GL.BindVertexArray(rm.Vao);
            GL.DrawElements(PrimitiveType.Triangles, rm.IndexCount, DrawElementsType.UnsignedInt, IntPtr.Zero);
            GL.BindVertexArray(0);
            if (rm.HasTexture)
            {
                GL.BindTexture(TextureTarget.Texture2D, 0);
            }
        }

        var opaqueMeshes = new List<RenderMesh>();
        var transparentMeshes = new List<(RenderMesh Mesh, float Depth)>();
        foreach (var rm in _meshes)
        {
            if (rm.Color.W < 0.999f)
            {
                float depth = 0f;
                if (rm.Vertices.Length > 0)
                {
                    Vector3 center = Vector3.Zero;
                    foreach (var v in rm.Vertices)
                        center += v;
                    center /= rm.Vertices.Length;
                    Vector3 worldPos = Vector3.TransformPosition(center, modelMat);
                    depth = (worldPos - cam).LengthSquared;
                }
                transparentMeshes.Add((rm, depth));
            }
            else
            {
                opaqueMeshes.Add(rm);
            }
        }

        foreach (var rm in opaqueMeshes)
            DrawMesh(rm);

        GL.DepthMask(false);
        foreach (var (mesh, _) in transparentMeshes.OrderByDescending(t => t.Depth))
            DrawMesh(mesh);
        GL.DepthMask(true);

        GL.UseProgram(_program);
        GL.UniformMatrix4(_viewLoc, false, ref view);
        GL.UniformMatrix4(_projLoc, false, ref proj);

        if (ShowBoneOutline && _boneVertexCount > 0)
        {
            GL.DepthMask(false);
            GL.Disable(EnableCap.DepthTest);
            GL.UniformMatrix4(_modelLoc, false, ref modelMat);
            GL.Uniform4(_colorLoc, new Vector4(1f, 0f, 0f, 1f));
            GL.BindVertexArray(_boneVao);
            GL.DrawArrays(PrimitiveType.Lines, 0, _boneVertexCount);
            GL.Enable(EnableCap.DepthTest);
            GL.BindVertexArray(0);
            GL.DepthMask(true);
        }

        if (ShowIkBones && _ikBoneCounts.Count > 0)
        {
            GL.DepthMask(false);
            GL.Disable(EnableCap.DepthTest);
            GL.UniformMatrix4(_modelLoc, false, ref modelMat);
            GL.BindVertexArray(_ikBoneVao);
            for (int i = 0; i < _ikBoneCounts.Count; i++)
            {
                var color = _ikBoneIndices[i] == _selectedIkBone ? IkBoneSelectedColor : IkBoneColor;
                GL.Uniform4(_colorLoc, color);
                GL.DrawArrays(PrimitiveType.TriangleFan, _ikBoneOffsets[i], _ikBoneCounts[i]);
                GL.DrawArrays(PrimitiveType.LineLoop, _ikBoneOffsets[i] + 1, _ikBoneCounts[i] - 1);
            }
            GL.Enable(EnableCap.DepthTest);
            GL.BindVertexArray(0);
            GL.DepthMask(true);
        }

        Matrix4 gridModel = Matrix4.CreateTranslation(Vector3.Zero);
        GL.DepthMask(false);
        GL.UniformMatrix4(_modelLoc, false, ref gridModel);
        GL.Uniform4(_colorLoc, new Vector4(1f, 1f, 1f, 0.3f));
        GL.BindVertexArray(_groundVao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, _groundVertexCount);
        GL.BindVertexArray(0);

        GL.UniformMatrix4(_modelLoc, false, ref gridModel);
        GL.Uniform4(_colorLoc, new Vector4(0.8f, 0.8f, 0.8f, 0.5f));
        GL.BindVertexArray(_gridVao);
        GL.DrawArrays(PrimitiveType.Lines, 0, _gridVertexCount);
        GL.BindVertexArray(0);
        GL.DepthMask(true);
    }

    public void Dispose()
    {
        foreach (var rm in _meshes)
        {
            if (rm.Vao != 0) GL.DeleteVertexArray(rm.Vao);
            if (rm.Vbo != 0) GL.DeleteBuffer(rm.Vbo);
            if (rm.Ebo != 0) GL.DeleteBuffer(rm.Ebo);
        }
        _meshes.Clear();
        _indexToHumanoidName.Clear();
        GL.DeleteBuffer(_gridVbo);
        GL.DeleteBuffer(_groundVbo);
        GL.DeleteBuffer(_boneVbo);
        GL.DeleteBuffer(_ikBoneVbo);
        GL.DeleteVertexArray(_gridVao);
        GL.DeleteVertexArray(_groundVao);
        GL.DeleteVertexArray(_boneVao);
        GL.DeleteVertexArray(_ikBoneVao);
        GL.DeleteProgram(_program);
        GL.DeleteProgram(_modelProgram);
    }
}
