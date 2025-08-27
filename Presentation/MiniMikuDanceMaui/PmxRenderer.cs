using System;
using System.Buffers;
using OpenTK.Mathematics;
using OpenTK.Graphics.ES30;
using GL = OpenTK.Graphics.ES30.GL;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using MiniMikuDance.Util;
using MiniMikuDance.Import;
using MiniMikuDance.App;
using MiniMikuDance.IK;
using MMDTools;
using SkiaSharp.Views.Maui.Controls;
using Vector2 = OpenTK.Mathematics.Vector2;
using Vector3 = OpenTK.Mathematics.Vector3;
using Vector4 = OpenTK.Mathematics.Vector4;

namespace MiniMikuDanceMaui;

public partial class PmxRenderer : IDisposable
{
    private int _program;
    private class RenderMesh
    {
        public int Vao;
        public int Vbo;
        public int Ebo;
        public int IndexCount;
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
        public int Texture;
        public bool HasTexture;
        public int SphereTexture;
        public bool HasSphereTexture;
        public int ToonTexture;
        public bool HasToonTexture;
        public SphereMode SphereMode;
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
    private readonly System.Collections.Generic.List<RenderMesh> _meshes = new();
    private readonly object _meshesLock = new();
    private RenderMesh[] _meshesArray = Array.Empty<RenderMesh>();
    private readonly Dictionary<string, MorphData> _morphs = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<MorphCategory, List<MorphData>> _morphsByCategory = new();
    private readonly Dictionary<string, float> _morphValues = new(StringComparer.OrdinalIgnoreCase);
    private List<(RenderMesh Mesh, int Index)>?[] _morphVertexMap = Array.Empty<List<(RenderMesh Mesh, int Index)>?>();
    private readonly HashSet<int> _changedOriginalVertices = new();
    private readonly object _changedVerticesLock = new();
    private readonly List<int> _changedVerticesList = new();
    private Vector3[] _vertexTotalOffsets = Array.Empty<Vector3>();
    private List<(string MorphName, Vector3 Offset)>?[] _vertexMorphOffsets = Array.Empty<List<(string MorphName, Vector3 Offset)>?>();
    private List<(string MorphName, System.Numerics.Vector4 Offset)>?[] _uvMorphOffsets = Array.Empty<List<(string MorphName, System.Numerics.Vector4 Offset)>?>();
    private string[] _morphIndexToName = Array.Empty<string>();
    private System.Numerics.Vector3[] _boneMorphTranslations = Array.Empty<System.Numerics.Vector3>();
    private System.Numerics.Quaternion[] _boneMorphRotations = Array.Empty<System.Numerics.Quaternion>();
    public SKGLView? Viewer { get; set; }
    private int _gridVao;
    private int _gridVbo;
    private int _gridVertexCount;
    private int _axesVao;
    private int _axesVbo;
    private int _axesVertexCount;
    private int _modelLoc;
    private int _viewLoc;
    private int _projLoc;
    private int _colorLoc;
    private int _pointSizeLoc;
    private float _orbitX;
    // 初期カメラ位置: モデル正面を向くよう水平回転は0
    private float _orbitY = 0f;
    private float _distance = 4f;
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
    private int _ikBoneEbo;
    private int _ikBoneIndexCount;
    private int _cubeVao;
    private int _cubeVbo;
    private int _cubeEbo;
    private int _cubeIndexCount;
    private System.Numerics.Matrix4x4[] _worldMats = Array.Empty<System.Numerics.Matrix4x4>();
    private System.Numerics.Matrix4x4[] _skinMats = Array.Empty<System.Numerics.Matrix4x4>();
    private float[] _boneLines = Array.Empty<float>();
    private int _boneCapacity;
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
    private int _modelSpecularLoc;
    private int _modelSpecularPowerLoc;
    private int _modelEdgeColorLoc;
    private int _modelEdgeSizeLoc;
    private int _modelToonColorLoc;
    private int _modelTexTintLoc;
    private int _modelSphereTexLoc;
    private int _modelUseSphereTexLoc;
    private int _modelSphereModeLoc;
    private int _modelToonTexLoc;
    private int _modelUseToonTexLoc;
    private int _modelSphereStrengthLoc;
    private int _modelToonStrengthLoc;
    private Matrix4 _modelTransform = Matrix4.Identity;
    public Matrix4 ModelTransform
    {
        get => _modelTransform;
        set
        {
            if (!_modelTransform.Equals(value))
            {
                _modelTransform = value;
                Viewer?.InvalidateSurface();
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
    private bool _bonesDirty;
    private bool _morphDirty;
    private bool _uvMorphDirty;
    private List<MiniMikuDance.Import.BoneData> _bones = new();
    private readonly List<IkBone> _ikBones = new();
    private readonly object _ikBonesLock = new();
    private readonly HashSet<int> _ikBoneIndices = new();
    private readonly HashSet<int> _physicsBones = new();
    private int _selectedBoneIndex = -1;
    public event Action<int>? BoneSelectionChanged;
    private Quaternion _externalRotation = Quaternion.Identity;
    // デフォルトのカメラ感度をスライダーの最小値に合わせる
    public float RotateSensitivity { get; set; } = 0.1f;
    public float PanSensitivity { get; set; } = 1f;
    public float ZoomSensitivity { get; set; } = 1f;
    public float ShadeShift { get; set; } = -0.1f;
    public float ShadeToony { get; set; } = 0.9f;
    public float RimIntensity { get; set; } = 0.5f;
    public float SphereStrength { get; set; } = 1f;
    public float ToonStrength { get; set; } = 0f;
    public float Ambient { get; set; } = 0.3f;
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
                Viewer?.InvalidateSurface();
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
                Viewer?.InvalidateSurface();
            }
        }
    }

    private bool _distinguishBoneTypes;
    public bool DistinguishBoneTypes
    {
        get => _distinguishBoneTypes;
        set
        {
            if (_distinguishBoneTypes != value)
            {
                _distinguishBoneTypes = value;
                Viewer?.InvalidateSurface();
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
                Viewer?.InvalidateSurface();
            }
        }
    }

    public int SelectedBoneIndex
    {
        get => _selectedBoneIndex;
        set
        {
            if (_selectedBoneIndex != value)
            {
                _selectedBoneIndex = value;
                BoneSelectionChanged?.Invoke(value);
                Viewer?.InvalidateSurface();
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
            {
                _stageSize = value;
                if (_program != 0)
                {
                    GenerateGrid();
                    GenerateAxes();
                }
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

    private float _bonePickPixels = AppSettings.DefaultBonePickPixels;
    public float BonePickPixels
    {
        get => _bonePickPixels;
        set => _bonePickPixels = value;
    }

    // EnsureBoneCapacity は PmxRenderer.Render.cs へ移動

    public void Initialize()
    {
const string vert = @"#version 300 es
layout(location = 0) in vec3 aPosition;
uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProj;
uniform float uPointSize;
void main(){
    gl_Position = uProj * uView * uModel * vec4(aPosition,1.0);
    gl_PointSize = uPointSize;
}";
        const string frag = @"#version 300 es
precision mediump float;
uniform vec4 uColor;
out vec4 FragColor;
void main(){
    FragColor = uColor;
}";
        int vs = GL.CreateShader(ShaderType.VertexShader);
        #if DEBUG
        CheckGLError("GL.CreateShader");
        #endif
        GL.ShaderSource(vs, vert);
        #if DEBUG
        CheckGLError("GL.ShaderSource");
        #endif
        GL.CompileShader(vs);
        #if DEBUG
        CheckGLError("GL.CompileShader");
        #endif
        int fs = GL.CreateShader(ShaderType.FragmentShader);
        #if DEBUG
        CheckGLError("GL.CreateShader");
        #endif
        GL.ShaderSource(fs, frag);
        #if DEBUG
        CheckGLError("GL.ShaderSource");
        #endif
        GL.CompileShader(fs);
        #if DEBUG
        CheckGLError("GL.CompileShader");
        #endif
        _program = GL.CreateProgram();
        #if DEBUG
        CheckGLError("GL.CreateProgram");
        #endif
        GL.AttachShader(_program, vs);
        #if DEBUG
        CheckGLError("GL.AttachShader");
        #endif
        GL.AttachShader(_program, fs);
        #if DEBUG
        CheckGLError("GL.AttachShader");
        #endif
        GL.LinkProgram(_program);
        #if DEBUG
        CheckGLError("GL.LinkProgram");
        #endif
        GL.DeleteShader(vs);
        #if DEBUG
        CheckGLError("GL.DeleteShader");
        #endif
        GL.DeleteShader(fs);
        #if DEBUG
        CheckGLError("GL.DeleteShader");
        #endif
        _modelLoc = GL.GetUniformLocation(_program, "uModel");
        #if DEBUG
        CheckGLError("GL.GetUniformLocation");
        #endif
        _viewLoc = GL.GetUniformLocation(_program, "uView");
        #if DEBUG
        CheckGLError("GL.GetUniformLocation");
        #endif
        _projLoc = GL.GetUniformLocation(_program, "uProj");
        #if DEBUG
        CheckGLError("GL.GetUniformLocation");
        #endif
        _colorLoc = GL.GetUniformLocation(_program, "uColor");
        #if DEBUG
        CheckGLError("GL.GetUniformLocation");
        #endif
        _pointSizeLoc = GL.GetUniformLocation(_program, "uPointSize");
        #if DEBUG
        CheckGLError("GL.GetUniformLocation");
        #endif

        // 透過描画設定（デフォルトでは無効）
        GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
        #if DEBUG
        CheckGLError("GL.BlendFunc");
        #endif

const string modelVert = @"#version 300 es
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aTex;
uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProj;
uniform float uPointSize;
out vec3 vNormal;
out vec2 vTex;
void main(){
    vec4 pos = uModel * vec4(aPosition,1.0);
    vNormal = mat3(uModel) * aNormal;
    vTex = aTex;
    gl_Position = uProj * uView * pos;
    gl_PointSize = uPointSize;
}";
const string modelFrag = @"#version 300 es
precision mediump float;
in vec3 vNormal;
in vec2 vTex;
uniform vec4 uColor;
uniform sampler2D uTex;
uniform bool uUseTex;
uniform sampler2D uSphereTex;
uniform bool uUseSphereTex;
uniform float uSphereStrength;
uniform int uSphereMode;
uniform sampler2D uToonTex;
uniform bool uUseToonTex;
uniform float uToonStrength;
uniform vec3 uLightDir;
uniform vec3 uViewDir;
uniform float uShadeShift;
uniform float uShadeToony;
uniform float uRimIntensity;
uniform float uAmbient;
uniform vec3 uSpecular;
uniform float uSpecularPower;
uniform vec4 uEdgeColor;
uniform float uEdgeSize;
uniform vec3 uToonColor;
uniform vec4 uTextureTint;
out vec4 FragColor;
const float PI = 3.14159265358979323846;
vec2 sphereUVFromNormal(vec3 n){
    float u = 0.5 + atan(n.z, n.x) / (2.0 * PI);
    float v = 0.5 - asin(n.y) / PI;
    return vec2(u, v);
}
void main(){
    vec4 base = (uUseTex ? texture(uTex, vTex) : uColor) * uTextureTint;
    if(uUseSphereTex){
        vec4 s = texture(uSphereTex, sphereUVFromNormal(normalize(vNormal)));
        vec4 combined = base;
        if(uSphereMode == 1) combined *= s;
        else if(uSphereMode == 2){
            combined.rgb = clamp(base.rgb + s.rgb, 0.0, 1.0);
            combined.a   = clamp(base.a + s.a, 0.0, 1.0);
        }
        base = mix(base, combined, uSphereStrength);
    }
    vec3 t = uUseToonTex ? texture(uToonTex, vTex).rgb : vec3(1.0);
    vec3 combined = base.rgb * t;
    base.rgb = mix(base.rgb, combined, uToonStrength);
    float ndotl = max(dot(normalize(vNormal), normalize(uLightDir)), 0.0);
    float light = clamp((ndotl + uShadeShift) * uShadeToony, 0.0, 1.0);
    float rim = pow(1.0 - max(dot(normalize(vNormal), normalize(uViewDir)), 0.0), 3.0) * uRimIntensity;
    vec3 color = base.rgb * (light + uAmbient) + base.rgb * rim;
    vec3 reflectDir = reflect(-normalize(uLightDir), normalize(vNormal));
    float spec = pow(max(dot(normalize(uViewDir), reflectDir), 0.0), uSpecularPower);
    color += uSpecular * spec;
    color *= uToonColor;
    FragColor = vec4(color, base.a);
}";
        int mvs = GL.CreateShader(ShaderType.VertexShader);
        #if DEBUG
        CheckGLError("GL.CreateShader");
        #endif
        GL.ShaderSource(mvs, modelVert);
        #if DEBUG
        CheckGLError("GL.ShaderSource");
        #endif
        GL.CompileShader(mvs);
        #if DEBUG
        CheckGLError("GL.CompileShader");
        #endif
        int mfs = GL.CreateShader(ShaderType.FragmentShader);
        #if DEBUG
        CheckGLError("GL.CreateShader");
        #endif
        GL.ShaderSource(mfs, modelFrag);
        #if DEBUG
        CheckGLError("GL.ShaderSource");
        #endif
        GL.CompileShader(mfs);
        #if DEBUG
        CheckGLError("GL.CompileShader");
        #endif
        _modelProgram = GL.CreateProgram();
        #if DEBUG
        CheckGLError("GL.CreateProgram");
        #endif
        GL.AttachShader(_modelProgram, mvs);
        #if DEBUG
        CheckGLError("GL.AttachShader");
        #endif
        GL.AttachShader(_modelProgram, mfs);
        #if DEBUG
        CheckGLError("GL.AttachShader");
        #endif
        GL.LinkProgram(_modelProgram);
        #if DEBUG
        CheckGLError("GL.LinkProgram");
        #endif
        GL.DeleteShader(mvs);
        #if DEBUG
        CheckGLError("GL.DeleteShader");
        #endif
        GL.DeleteShader(mfs);
        #if DEBUG
        CheckGLError("GL.DeleteShader");
        #endif
        _modelViewLoc = GL.GetUniformLocation(_modelProgram, "uView");
        #if DEBUG
        CheckGLError("GL.GetUniformLocation");
        #endif
        _modelProjLoc = GL.GetUniformLocation(_modelProgram, "uProj");
        #if DEBUG
        CheckGLError("GL.GetUniformLocation");
        #endif
        _modelMatrixLoc = GL.GetUniformLocation(_modelProgram, "uModel");
        #if DEBUG
        CheckGLError("GL.GetUniformLocation");
        #endif
        _modelColorLoc = GL.GetUniformLocation(_modelProgram, "uColor");
        #if DEBUG
        CheckGLError("GL.GetUniformLocation");
        #endif
        _modelTexLoc = GL.GetUniformLocation(_modelProgram, "uTex");
        #if DEBUG
        CheckGLError("GL.GetUniformLocation");
        #endif
        _modelUseTexLoc = GL.GetUniformLocation(_modelProgram, "uUseTex");
        #if DEBUG
        CheckGLError("GL.GetUniformLocation");
        #endif
        _modelLightDirLoc = GL.GetUniformLocation(_modelProgram, "uLightDir");
        #if DEBUG
        CheckGLError("GL.GetUniformLocation");
        #endif
        _modelViewDirLoc = GL.GetUniformLocation(_modelProgram, "uViewDir");
        #if DEBUG
        CheckGLError("GL.GetUniformLocation");
        #endif
        _modelShadeShiftLoc = GL.GetUniformLocation(_modelProgram, "uShadeShift");
        #if DEBUG
        CheckGLError("GL.GetUniformLocation");
        #endif
        _modelShadeToonyLoc = GL.GetUniformLocation(_modelProgram, "uShadeToony");
        #if DEBUG
        CheckGLError("GL.GetUniformLocation");
        #endif
        _modelRimIntensityLoc = GL.GetUniformLocation(_modelProgram, "uRimIntensity");
        #if DEBUG
        CheckGLError("GL.GetUniformLocation");
        #endif
        _modelAmbientLoc = GL.GetUniformLocation(_modelProgram, "uAmbient");
        #if DEBUG
        CheckGLError("GL.GetUniformLocation");
        #endif
        _modelSpecularLoc = GL.GetUniformLocation(_modelProgram, "uSpecular");
        #if DEBUG
        CheckGLError("GL.GetUniformLocation");
        #endif
        _modelSpecularPowerLoc = GL.GetUniformLocation(_modelProgram, "uSpecularPower");
        #if DEBUG
        CheckGLError("GL.GetUniformLocation");
        #endif
        _modelEdgeColorLoc = GL.GetUniformLocation(_modelProgram, "uEdgeColor");
        #if DEBUG
        CheckGLError("GL.GetUniformLocation");
        #endif
        _modelEdgeSizeLoc = GL.GetUniformLocation(_modelProgram, "uEdgeSize");
        #if DEBUG
        CheckGLError("GL.GetUniformLocation");
        #endif
        _modelToonColorLoc = GL.GetUniformLocation(_modelProgram, "uToonColor");
        #if DEBUG
        CheckGLError("GL.GetUniformLocation");
        #endif
        _modelTexTintLoc = GL.GetUniformLocation(_modelProgram, "uTextureTint");
        #if DEBUG
        CheckGLError("GL.GetUniformLocation");
        #endif
        _modelSphereTexLoc = GL.GetUniformLocation(_modelProgram, "uSphereTex");
        #if DEBUG
        CheckGLError("GL.GetUniformLocation");
        #endif
        _modelUseSphereTexLoc = GL.GetUniformLocation(_modelProgram, "uUseSphereTex");
        #if DEBUG
        CheckGLError("GL.GetUniformLocation");
        #endif
        _modelSphereModeLoc = GL.GetUniformLocation(_modelProgram, "uSphereMode");
        #if DEBUG
        CheckGLError("GL.GetUniformLocation");
        #endif
        _modelToonTexLoc = GL.GetUniformLocation(_modelProgram, "uToonTex");
        #if DEBUG
        CheckGLError("GL.GetUniformLocation");
        #endif
        _modelUseToonTexLoc = GL.GetUniformLocation(_modelProgram, "uUseToonTex");
        #if DEBUG
        CheckGLError("GL.GetUniformLocation");
        #endif
        _modelSphereStrengthLoc = GL.GetUniformLocation(_modelProgram, "uSphereStrength");
        #if DEBUG
        CheckGLError("GL.GetUniformLocation");
        #endif
        _modelToonStrengthLoc = GL.GetUniformLocation(_modelProgram, "uToonStrength");
        #if DEBUG
        CheckGLError("GL.GetUniformLocation");
        #endif

        GenerateGrid();
        GenerateAxes();
    }

    private void GenerateGrid()
    {
        int range = (int)_stageSize;
        _gridVertexCount = (range * 2 + 1) * 4;
        int gridSize = _gridVertexCount * 3;
        float[] grid = ArrayPool<float>.Shared.Rent(gridSize);
        int idx = 0;
        for (int i = -range; i <= range; i++)
        {
            grid[idx++] = i; grid[idx++] = 0f; grid[idx++] = -range;
            grid[idx++] = i; grid[idx++] = 0f; grid[idx++] = range;
            grid[idx++] = -range; grid[idx++] = 0f; grid[idx++] = i;
            grid[idx++] = range; grid[idx++] = 0f; grid[idx++] = i;
        }
        if (_gridVao == 0)
        {
            _gridVao = GL.GenVertexArray();
            #if DEBUG
            CheckGLError("GL.GenVertexArray");
            #endif
        }
        if (_gridVbo == 0)
        {
            _gridVbo = GL.GenBuffer();
            #if DEBUG
            CheckGLError("GL.GenBuffer");
            #endif
        }
        GL.BindVertexArray(_gridVao);
        #if DEBUG
        CheckGLError("GL.BindVertexArray");
        #endif
        GL.BindBuffer(BufferTarget.ArrayBuffer, _gridVbo);
        #if DEBUG
        CheckGLError("GL.BindBuffer");
        #endif
        GL.BufferData(BufferTarget.ArrayBuffer, gridSize * sizeof(float), grid, BufferUsageHint.StaticDraw);
        #if DEBUG
        CheckGLError("GL.BufferData");
        #endif
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        #if DEBUG
        CheckGLError("GL.VertexAttribPointer");
        #endif
        GL.EnableVertexAttribArray(0);
        #if DEBUG
        CheckGLError("GL.EnableVertexAttribArray");
        #endif
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        #if DEBUG
        CheckGLError("GL.BindBuffer");
        #endif
        GL.BindVertexArray(0);
        #if DEBUG
        CheckGLError("GL.BindVertexArray");
        #endif
        ArrayPool<float>.Shared.Return(grid);

        float r = _stageSize;
        float[] plane =
        {
            -r, 0f, -r,
             r, 0f, -r,
            -r, 0f,  r,
             r, 0f, -r,
             r, 0f,  r,
            -r, 0f,  r
        };
        _groundVertexCount = 6;
        if (_groundVao == 0)
        {
            _groundVao = GL.GenVertexArray();
            #if DEBUG
            CheckGLError("GL.GenVertexArray");
            #endif
        }
        if (_groundVbo == 0)
        {
            _groundVbo = GL.GenBuffer();
            #if DEBUG
            CheckGLError("GL.GenBuffer");
            #endif
        }
        GL.BindVertexArray(_groundVao);
        #if DEBUG
        CheckGLError("GL.BindVertexArray");
        #endif
        GL.BindBuffer(BufferTarget.ArrayBuffer, _groundVbo);
        #if DEBUG
        CheckGLError("GL.BindBuffer");
        #endif
        GL.BufferData(BufferTarget.ArrayBuffer, plane.Length * sizeof(float), plane, BufferUsageHint.StaticDraw);
        #if DEBUG
        CheckGLError("GL.BufferData");
        #endif
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        #if DEBUG
        CheckGLError("GL.VertexAttribPointer");
        #endif
        GL.EnableVertexAttribArray(0);
        #if DEBUG
        CheckGLError("GL.EnableVertexAttribArray");
        #endif
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        #if DEBUG
        CheckGLError("GL.BindBuffer");
        #endif
        GL.BindVertexArray(0);
        #if DEBUG
        CheckGLError("GL.BindVertexArray");
        #endif
    }

    private void GenerateAxes()
    {
        _axesVertexCount = 6;
        float length = _stageSize;
        float arrow = length * 0.1f;
        float cap = arrow * 0.5f;
        float[] axes = new float[_axesVertexCount * 3 * 3];
        int i = 0;
        // X axis
        axes[i++] = 0f; axes[i++] = 0f; axes[i++] = 0f;
        axes[i++] = length; axes[i++] = 0f; axes[i++] = 0f;
        axes[i++] = length; axes[i++] = 0f; axes[i++] = 0f;
        axes[i++] = length - arrow; axes[i++] = cap; axes[i++] = 0f;
        axes[i++] = length; axes[i++] = 0f; axes[i++] = 0f;
        axes[i++] = length - arrow; axes[i++] = -cap; axes[i++] = 0f;
        // Y axis
        axes[i++] = 0f; axes[i++] = 0f; axes[i++] = 0f;
        axes[i++] = 0f; axes[i++] = length; axes[i++] = 0f;
        axes[i++] = 0f; axes[i++] = length; axes[i++] = 0f;
        axes[i++] = cap; axes[i++] = length - arrow; axes[i++] = 0f;
        axes[i++] = 0f; axes[i++] = length; axes[i++] = 0f;
        axes[i++] = -cap; axes[i++] = length - arrow; axes[i++] = 0f;
        // Z axis
        axes[i++] = 0f; axes[i++] = 0f; axes[i++] = 0f;
        axes[i++] = 0f; axes[i++] = 0f; axes[i++] = length;
        axes[i++] = 0f; axes[i++] = 0f; axes[i++] = length;
        axes[i++] = cap; axes[i++] = 0f; axes[i++] = length - arrow;
        axes[i++] = 0f; axes[i++] = 0f; axes[i++] = length;
        axes[i++] = -cap; axes[i++] = 0f; axes[i++] = length - arrow;

        if (_axesVao == 0)
        {
            _axesVao = GL.GenVertexArray();
            #if DEBUG
            CheckGLError("GL.GenVertexArray");
            #endif
        }
        if (_axesVbo == 0)
        {
            _axesVbo = GL.GenBuffer();
            #if DEBUG
            CheckGLError("GL.GenBuffer");
            #endif
        }
        GL.BindVertexArray(_axesVao);
        #if DEBUG
        CheckGLError("GL.BindVertexArray");
        #endif
        GL.BindBuffer(BufferTarget.ArrayBuffer, _axesVbo);
        #if DEBUG
        CheckGLError("GL.BindBuffer");
        #endif
        GL.BufferData(BufferTarget.ArrayBuffer, axes.Length * sizeof(float), axes, BufferUsageHint.StaticDraw);
        #if DEBUG
        CheckGLError("GL.BufferData");
        #endif
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        #if DEBUG
        CheckGLError("GL.VertexAttribPointer");
        #endif
        GL.EnableVertexAttribArray(0);
        #if DEBUG
        CheckGLError("GL.EnableVertexAttribArray");
        #endif
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        #if DEBUG
        CheckGLError("GL.BindBuffer");
        #endif
        GL.BindVertexArray(0);
        #if DEBUG
        CheckGLError("GL.BindVertexArray");
        #endif
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
        GL.Viewport(0, 0, width, height);
        #if DEBUG
        CheckGLError("GL.Viewport");
        #endif
        _viewProjDirty = true;
    }

    public void Orbit(float dx, float dy)
    {
        _orbitY -= dx * 0.01f * RotateSensitivity;
        _orbitX -= dy * 0.01f * RotateSensitivity;
        // Clamp pitch to [-90°, 90°]
        float limit = MathHelper.DegreesToRadians(89.9f);
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
        _orbitY = 0f;
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
        _bonesDirty = true;
    }

    public void MarkBonesDirty()
    {
        _bonesDirty = true;
    }

    public void SetIkBones(IEnumerable<IkBone> bones)
    {
        lock (_ikBonesLock)
        {
            _ikBones.Clear();
            _ikBoneIndices.Clear();
            foreach (var ik in bones)
            {
                _ikBones.Add(ik);
                _ikBoneIndices.Add(ik.PmxBoneIndex);
                ik.Position = GetBoneWorldPosition(ik.PmxBoneIndex);
            }
        }
    }

    public void ClearIkBones()
    {
        lock (_ikBonesLock)
        {
            _ikBones.Clear();
            _ikBoneIndices.Clear();
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

    private void EnsureIkBoneMesh()
    {
        if (_ikBoneVao != 0)
            return;

        const int lat = 8;
        const int lon = 8;
        var vertices = new List<float>();
        var indices = new List<ushort>();

        for (int y = 0; y <= lat; y++)
        {
            float v = (float)y / lat;
            float theta = v * MathF.PI;
            float sinTheta = MathF.Sin(theta);
            float cosTheta = MathF.Cos(theta);
            for (int x = 0; x <= lon; x++)
            {
                float u = (float)x / lon;
                float phi = u * MathF.PI * 2f;
                float sinPhi = MathF.Sin(phi);
                float cosPhi = MathF.Cos(phi);
                vertices.Add(cosPhi * sinTheta);
                vertices.Add(cosTheta);
                vertices.Add(sinPhi * sinTheta);
            }
        }

        for (int y = 0; y < lat; y++)
        {
            for (int x = 0; x < lon; x++)
            {
                int first = y * (lon + 1) + x;
                int second = first + lon + 1;
                indices.Add((ushort)first);
                indices.Add((ushort)second);
                indices.Add((ushort)(first + 1));
                indices.Add((ushort)second);
                indices.Add((ushort)(second + 1));
                indices.Add((ushort)(first + 1));
            }
        }

        _ikBoneIndexCount = indices.Count;
        _ikBoneVao = GL.GenVertexArray();
        #if DEBUG
        CheckGLError("GL.GenVertexArray");
        #endif
        _ikBoneVbo = GL.GenBuffer();
        #if DEBUG
        CheckGLError("GL.GenBuffer");
        #endif
        _ikBoneEbo = GL.GenBuffer();
        #if DEBUG
        CheckGLError("GL.GenBuffer");
        #endif

        GL.BindVertexArray(_ikBoneVao);
        #if DEBUG
        CheckGLError("GL.BindVertexArray");
        #endif
        GL.BindBuffer(BufferTarget.ArrayBuffer, _ikBoneVbo);
        #if DEBUG
        CheckGLError("GL.BindBuffer");
        #endif
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * sizeof(float), vertices.ToArray(), BufferUsageHint.StaticDraw);
        #if DEBUG
        CheckGLError("GL.BufferData");
        #endif
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        #if DEBUG
        CheckGLError("GL.VertexAttribPointer");
        #endif
        GL.EnableVertexAttribArray(0);
        #if DEBUG
        CheckGLError("GL.EnableVertexAttribArray");
        #endif
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ikBoneEbo);
        #if DEBUG
        CheckGLError("GL.BindBuffer");
        #endif
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Count * sizeof(ushort), indices.ToArray(), BufferUsageHint.StaticDraw);
        #if DEBUG
        CheckGLError("GL.BufferData");
        #endif
        GL.BindVertexArray(0);
        #if DEBUG
        CheckGLError("GL.BindVertexArray");
        #endif
    }

    private void EnsureCubeMesh()
    {
        if (_cubeVao != 0)
            return;

        float[] vertices =
        {
            -1f, -1f, -1f,
             1f, -1f, -1f,
             1f,  1f, -1f,
            -1f,  1f, -1f,
            -1f, -1f,  1f,
             1f, -1f,  1f,
             1f,  1f,  1f,
            -1f,  1f,  1f
        };

        ushort[] indices =
        {
            0,1,2, 2,3,0,
            4,5,6, 6,7,4,
            0,4,7, 7,3,0,
            1,5,6, 6,2,1,
            3,2,6, 6,7,3,
            0,1,5, 5,4,0
        };

        _cubeIndexCount = indices.Length;
        _cubeVao = GL.GenVertexArray();
        #if DEBUG
        CheckGLError("GL.GenVertexArray");
        #endif
        _cubeVbo = GL.GenBuffer();
        #if DEBUG
        CheckGLError("GL.GenBuffer");
        #endif
        _cubeEbo = GL.GenBuffer();
        #if DEBUG
        CheckGLError("GL.GenBuffer");
        #endif

        GL.BindVertexArray(_cubeVao);
        #if DEBUG
        CheckGLError("GL.BindVertexArray");
        #endif
        GL.BindBuffer(BufferTarget.ArrayBuffer, _cubeVbo);
        #if DEBUG
        CheckGLError("GL.BindBuffer");
        #endif
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
        #if DEBUG
        CheckGLError("GL.BufferData");
        #endif
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        #if DEBUG
        CheckGLError("GL.VertexAttribPointer");
        #endif
        GL.EnableVertexAttribArray(0);
        #if DEBUG
        CheckGLError("GL.EnableVertexAttribArray");
        #endif
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _cubeEbo);
        #if DEBUG
        CheckGLError("GL.BindBuffer");
        #endif
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(ushort), indices, BufferUsageHint.StaticDraw);
        #if DEBUG
        CheckGLError("GL.BufferData");
        #endif
        GL.BindVertexArray(0);
        #if DEBUG
        CheckGLError("GL.BindVertexArray");
        #endif
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
                var pos = _worldMats[i].Translation.ToOpenTK();
                var v4 = new Vector4(pos, 1f);
                var clip = v4 * _viewMatrix;
                clip = clip * _projMatrix;
                if (clip.W <= 0)
                    continue;
                var ndc = clip.Xyz / clip.W;
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
                var pos = _worldMats[i].Translation.ToOpenTK();
                var v4 = new Vector4(pos, 1f);
                var clip = v4 * _viewMatrix;
                clip = clip * _projMatrix;
                if (clip.W <= 0)
                    continue;
                var ndc = clip.Xyz / clip.W;
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
        SelectedBoneIndex = result;
        return result;
    }

    public System.Numerics.Vector3 GetBoneWorldPosition(int index)
    {
        if (index < 0 || index >= _worldMats.Length)
            return System.Numerics.Vector3.Zero;
        var pos = _worldMats[index].Translation.ToOpenTK();
        pos = Vector3.TransformPosition(pos, _modelTransform);
        var result = pos.ToNumerics();
        return result;
    }

    public System.Numerics.Vector3 GetCameraPosition()
    {
        return _cameraPos.ToNumerics();
    }

    public System.Numerics.Vector3 WorldToModel(System.Numerics.Vector3 worldPos)
    {
        Matrix4.Invert(_modelTransform, out var inv);
        var pos = Vector3.TransformPosition(worldPos.ToOpenTK(), inv);
        var result = pos.ToNumerics();
        return result;
    }

    public System.Numerics.Vector3 ModelToWorld(System.Numerics.Vector3 modelPos)
    {
        var pos = Vector3.TransformPosition(modelPos.ToOpenTK(), _modelTransform);
        var result = pos.ToNumerics();
        return result;
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
        var rayEye = rayClip * invProj;
        // ビュー空間では前方を -Z とするため、Z軸を反転する
        rayEye.Z = -1f; rayEye.W = 0f;
        var rayWorld = rayEye * invView;
        var dir = Vector3.Normalize(rayWorld.Xyz);
        return (_cameraPos.ToNumerics(), dir.ToNumerics());
    }

    public void SetBoneRotation(int index, Vector3 degrees)
    {
        if (index < 0)
            return;
        if (index < _bones.Count)
        {
            var bone = _bones[index];
            if (bone.HasRotationLimit)
            {
                degrees.X = Math.Clamp(degrees.X, bone.MinRotationDeg.X, bone.MaxRotationDeg.X);
                degrees.Y = Math.Clamp(degrees.Y, bone.MinRotationDeg.Y, bone.MaxRotationDeg.Y);
                degrees.Z = Math.Clamp(degrees.Z, bone.MinRotationDeg.Z, bone.MaxRotationDeg.Z);
            }
        }
        while (_boneRotations.Count <= index)
            _boneRotations.Add(Vector3.Zero);
        _boneRotations[index] = degrees;
        _bonesDirty = true;
        Viewer?.InvalidateSurface();
    }

    // RecalculateBoneMorphs は PmxRenderer.Morph.cs へ移動

    // RecalculateMaterialMorphs は PmxRenderer.Morph.cs へ移動

    private System.Numerics.Matrix4x4[] CalculateWorldMatrices()
    {
        var worldMats = new System.Numerics.Matrix4x4[_bones.Count];
        for (int i = 0; i < _bones.Count; i++)
        {
            var bone = _bones[i];
            System.Numerics.Vector3 euler = i < _boneRotations.Count ? _boneRotations[i].ToNumerics() : System.Numerics.Vector3.Zero;
            var rot = euler.FromEulerDegrees();
            if (bone.HasFixedAxis)
                rot = ProjectRotation(rot, bone.FixedAxis);
            System.Numerics.Vector3 trans = bone.Translation;
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

    private static System.Numerics.Quaternion ProjectRotation(System.Numerics.Quaternion q, System.Numerics.Vector3 axis)
    {
        axis = System.Numerics.Vector3.Normalize(axis);
        if (axis == System.Numerics.Vector3.Zero)
            return System.Numerics.Quaternion.Identity;
        q = System.Numerics.Quaternion.Normalize(q);
        var w = Math.Clamp(q.W, -1f, 1f);
        float angle = 2f * MathF.Acos(w);
        float s = MathF.Sqrt(MathF.Max(0f, 1f - w * w));
        System.Numerics.Vector3 qAxis = s < 1e-6f ? axis : new System.Numerics.Vector3(q.X / s, q.Y / s, q.Z / s);
        float proj = System.Numerics.Vector3.Dot(qAxis, axis);
        return System.Numerics.Quaternion.CreateFromAxisAngle(axis, angle * proj);
    }

    private static (int Texture, bool HasTexture) LoadTextureBytes(byte[]? bytes, int width, int height)
    {
        if (bytes == null)
            return (0, false);

        int texture = GL.GenTexture();
        #if DEBUG
        CheckGLError("GL.GenTexture");
        #endif
        GL.BindTexture((All)TextureTarget.Texture2D, texture);
        #if DEBUG
        CheckGLError("GL.BindTexture");
        #endif

        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            GL.TexImage2D(
                (All)TextureTarget.Texture2D,
                0,
                (All)PixelInternalFormat.Rgba,
                width,
                height,
                0,
                (All)PixelFormat.Rgba,
                (All)PixelType.UnsignedByte,
                handle.AddrOfPinnedObject());
            #if DEBUG
            CheckGLError("GL.TexImage2D");
            #endif
        }
        finally
        {
            handle.Free();
        }

        GL.TexParameter((All)TextureTarget.Texture2D, (All)TextureParameterName.TextureMinFilter, (int)All.Linear);
        #if DEBUG
        CheckGLError("GL.TexParameter");
        #endif
        GL.TexParameter((All)TextureTarget.Texture2D, (All)TextureParameterName.TextureMagFilter, (int)All.Linear);
        #if DEBUG
        CheckGLError("GL.TexParameter");
        #endif

        return (texture, true);
    }

    public void SetBoneWorldPosition(int index, Vector3 worldPos)
    {
        if (index < 0 || index >= _bones.Count)
            return;

        var bone = _bones[index];
        var worldMats = CalculateWorldMatrices();
        var parentWorld = bone.Parent >= 0 && bone.Parent < _bones.Count
            ? worldMats[bone.Parent]
            : System.Numerics.Matrix4x4.Identity;
        System.Numerics.Matrix4x4.Invert(parentWorld, out var invParent);
        var localPos = System.Numerics.Vector3.Transform(worldPos.ToNumerics(), invParent);
        var length = bone.InitialTranslation.Length();
        if (length < 1e-6f)
            return;

        var dir = System.Numerics.Vector3.Normalize(localPos);
        bone.Translation = dir * length;

        var from = System.Numerics.Vector3.Normalize(bone.InitialTranslation);
        var axis = System.Numerics.Vector3.Cross(from, dir);
        float axisLen = axis.Length();
        System.Numerics.Quaternion rot;
        float dot = System.Numerics.Vector3.Dot(from, dir);
        if (axisLen < 1e-6f)
        {
            rot = dot > 0f
                ? System.Numerics.Quaternion.Identity
                : System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.UnitY, MathF.PI);
        }
        else
        {
            axis /= axisLen;
            float angle = MathF.Acos(Math.Clamp(dot, -1f, 1f));
            rot = System.Numerics.Quaternion.CreateFromAxisAngle(axis, angle);
        }
        bone.Rotation = rot;
        _bonesDirty = true;
        Viewer?.InvalidateSurface();
    }

    public Vector3 GetBoneRotation(int index)
    {
        if (index < 0 || index >= _boneRotations.Count)
            return Vector3.Zero;
        return _boneRotations[index];
    }


    // Morph関連メソッドは PmxRenderer.Morph.cs へ移動

    private static (float[] Verts, List<uint> Indices) CreateVertexData(MiniMikuDance.Import.SubMeshData sm, RenderMesh rm)
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

        var indices = new List<uint>();
        foreach (var f in sm.Mesh.Faces)
        {
            foreach (var idx in f.Indices)
                indices.Add((uint)idx);
        }

        rm.IndexCount = indices.Count;

        int baseVertCount = sm.Mesh.Vertices.Count;
        rm.BaseVertices = new Vector3[baseVertCount];
        for (int i = 0; i < baseVertCount; i++)
        {
            var v = sm.Mesh.Vertices[i];
            rm.BaseVertices[i] = new Vector3(v.X, v.Y, v.Z);
        }
        rm.VertexOffsets = new Vector3[baseVertCount];

        int normalsCount = sm.Mesh.Normals.Count;
        rm.Normals = new Vector3[normalsCount];
        for (int i = 0; i < normalsCount; i++)
        {
            var n = sm.Mesh.Normals[i];
            rm.Normals[i] = new Vector3(n.X, n.Y, n.Z);
        }

        int texCount = sm.TexCoords.Count;
        rm.TexCoords = new Vector2[texCount];
        rm.UvOffsets = new Vector2[texCount];
        for (int i = 0; i < texCount; i++)
        {
            var t = sm.TexCoords[i];
            rm.TexCoords[i] = new Vector2(t.X, t.Y);
        }

        int jointIndexCount = sm.JointIndices.Count;
        rm.JointIndices = new Vector4[jointIndexCount];
        for (int i = 0; i < jointIndexCount; i++)
        {
            var j = sm.JointIndices[i];
            rm.JointIndices[i] = new Vector4(j.X, j.Y, j.Z, j.W);
        }

        int jointWeightCount = sm.JointWeights.Count;
        rm.JointWeights = new Vector4[jointWeightCount];
        for (int i = 0; i < jointWeightCount; i++)
        {
            var w = sm.JointWeights[i];
            rm.JointWeights[i] = new Vector4(w.X, w.Y, w.Z, w.W);
        }

        int sdefCount = sm.SdefC.Count;
        rm.SdefC = new Vector3[sdefCount];
        rm.SdefR0 = new Vector3[sdefCount];
        rm.SdefR1 = new Vector3[sdefCount];
        for (int i = 0; i < sdefCount; i++)
        {
            var c = sm.SdefC[i];
            rm.SdefC[i] = new Vector3(c.X, c.Y, c.Z);
            var r0 = sm.SdefR0[i];
            rm.SdefR0[i] = new Vector3(r0.X, r0.Y, r0.Z);
            var r1 = sm.SdefR1[i];
            rm.SdefR1[i] = new Vector3(r1.X, r1.Y, r1.Z);
        }

        return (verts, indices);
    }

    private static void SetupBuffers(RenderMesh rm, MiniMikuDance.Import.SubMeshData sm, float[] verts, List<uint> indices)
    {
        rm.Vao = GL.GenVertexArray();
        #if DEBUG
        CheckGLError("GL.GenVertexArray");
        #endif
        rm.Vbo = GL.GenBuffer();
        #if DEBUG
        CheckGLError("GL.GenBuffer");
        #endif
        rm.Ebo = GL.GenBuffer();
        #if DEBUG
        CheckGLError("GL.GenBuffer");
        #endif
        rm.BaseColor = sm.ColorFactor.ToVector4();
        rm.Color = rm.BaseColor;
        rm.BaseSpecular = sm.Specular.ToOpenTK();
        rm.Specular = rm.BaseSpecular;
        rm.BaseSpecularPower = sm.SpecularPower;
        rm.SpecularPower = rm.BaseSpecularPower;
        rm.BaseEdgeColor = sm.EdgeColor.ToVector4();
        rm.EdgeColor = rm.BaseEdgeColor;
        rm.BaseEdgeSize = sm.EdgeSize;
        rm.EdgeSize = rm.BaseEdgeSize;
        rm.BaseToonColor = sm.ToonColor.ToOpenTK();
        rm.ToonColor = rm.BaseToonColor;
        rm.BaseTextureTint = sm.TextureTint.ToVector4();
        rm.TextureTint = rm.BaseTextureTint;
        rm.SphereMode = sm.SphereMode;

        GL.BindVertexArray(rm.Vao);
        #if DEBUG
        CheckGLError("GL.BindVertexArray");
        #endif
        GL.BindBuffer(BufferTarget.ArrayBuffer, rm.Vbo);
        #if DEBUG
        CheckGLError("GL.BindBuffer");
        #endif
        GL.BufferData(BufferTarget.ArrayBuffer, verts.Length * sizeof(float), verts, BufferUsageHint.StaticDraw);
        #if DEBUG
        CheckGLError("GL.BufferData");
        #endif
        int stride = 8 * sizeof(float);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
        #if DEBUG
        CheckGLError("GL.VertexAttribPointer");
        #endif
        GL.EnableVertexAttribArray(0);
        #if DEBUG
        CheckGLError("GL.EnableVertexAttribArray");
        #endif
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
        #if DEBUG
        CheckGLError("GL.VertexAttribPointer");
        #endif
        GL.EnableVertexAttribArray(1);
        #if DEBUG
        CheckGLError("GL.EnableVertexAttribArray");
        #endif
        GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));
        #if DEBUG
        CheckGLError("GL.VertexAttribPointer");
        #endif
        GL.EnableVertexAttribArray(2);
        #if DEBUG
        CheckGLError("GL.EnableVertexAttribArray");
        #endif
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, rm.Ebo);
        #if DEBUG
        CheckGLError("GL.BindBuffer");
        #endif
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Count * sizeof(uint), indices.ToArray(), BufferUsageHint.StaticDraw);
        #if DEBUG
        CheckGLError("GL.BufferData");
        #endif
        GL.BindVertexArray(0);
        #if DEBUG
        CheckGLError("GL.BindVertexArray");
        #endif
    }

    private static void ApplyTextures(MiniMikuDance.Import.SubMeshData sm, RenderMesh rm)
    {
        (rm.Texture, rm.HasTexture) = LoadTextureBytes(sm.TextureBytes, sm.TextureWidth, sm.TextureHeight);
        sm.TextureBytes = null;
        sm.TextureWidth = 0;
        sm.TextureHeight = 0;

        (rm.SphereTexture, rm.HasSphereTexture) = LoadTextureBytes(sm.SphereTextureBytes, sm.SphereTextureWidth, sm.SphereTextureHeight);
        sm.SphereTextureBytes = null;
        sm.SphereTextureWidth = 0;
        sm.SphereTextureHeight = 0;

        (rm.ToonTexture, rm.HasToonTexture) = LoadTextureBytes(sm.ToonTextureBytes, sm.ToonTextureWidth, sm.ToonTextureHeight);
        sm.ToonTextureBytes = null;
        sm.ToonTextureWidth = 0;
        sm.ToonTextureHeight = 0;

        if (rm.HasTexture)
        {
            rm.BaseTextureTint *= rm.BaseColor;
            rm.TextureTint = rm.BaseTextureTint;
            rm.BaseColor = Vector4.One;
            rm.Color = Vector4.One;
        }
    }

    public void LoadModel(MiniMikuDance.Import.ModelData data)
    {
        RenderMesh[] oldMeshes;
        lock (_meshesLock)
        {
            oldMeshes = _meshesArray;
            _meshes.Clear();
            _meshesArray = Array.Empty<RenderMesh>();
        }
        foreach (var rm in oldMeshes)
        {
            if (rm.Vao != 0)
            {
                GL.DeleteVertexArray(rm.Vao);
                #if DEBUG
                CheckGLError("GL.DeleteVertexArray");
                #endif
            }
            if (rm.Vbo != 0)
            {
                GL.DeleteBuffer(rm.Vbo);
                #if DEBUG
                CheckGLError("GL.DeleteBuffer");
                #endif
            }
            if (rm.Ebo != 0)
            {
                GL.DeleteBuffer(rm.Ebo);
                #if DEBUG
                CheckGLError("GL.DeleteBuffer");
                #endif
            }
            if (rm.Texture != 0)
            {
                GL.DeleteTexture(rm.Texture);
                #if DEBUG
                CheckGLError("GL.DeleteTexture");
                #endif
            }
            if (rm.SphereTexture != 0)
            {
                GL.DeleteTexture(rm.SphereTexture);
                #if DEBUG
                CheckGLError("GL.DeleteTexture");
                #endif
            }
            if (rm.ToonTexture != 0)
            {
                GL.DeleteTexture(rm.ToonTexture);
                #if DEBUG
                CheckGLError("GL.DeleteTexture");
                #endif
            }
        }
        _bones = data.Bones;
        _physicsBones.Clear();
        foreach (var rb in data.RigidBodies)
        {
            if (rb.BoneIndex >= 0)
                _physicsBones.Add(rb.BoneIndex);
        }
        _selectedBoneIndex = -1;
        _worldMats = new System.Numerics.Matrix4x4[_bones.Count];
        _skinMats = new System.Numerics.Matrix4x4[_bones.Count];
        _modelTransform = data.Transform.ToMatrix4();

        if (data.SubMeshes.Count == 0)
        {
            data.SubMeshes.Add(new MiniMikuDance.Import.SubMeshData
            {
                Mesh = data.Mesh
            });
        }

        var newMeshes = new List<RenderMesh>();
        foreach (var sm in data.SubMeshes)
        {
            var rm = new RenderMesh();
            var (verts, indices) = CreateVertexData(sm, rm);
            SetupBuffers(rm, sm, verts, indices);
            ApplyTextures(sm, rm);
            newMeshes.Add(rm);
        }

        lock (_meshesLock)
        {
            _meshes.AddRange(newMeshes);
            _meshesArray = _meshes.ToArray();
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

        var meshesSnapshot = _meshesArray;
        foreach (var rm in meshesSnapshot)
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
                    _orbitY = 0f;
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
                _orbitY = 0f;
                _viewProjDirty = true;
            }
        }
        catch { /* ignore fit errors */ }
    }

    // Render メソッドは PmxRenderer.Render.cs へ移動

    public void Dispose()
    {
        RenderMesh[] meshes;
        lock (_meshesLock)
        {
            meshes = _meshesArray;
            _meshes.Clear();
            _meshesArray = Array.Empty<RenderMesh>();
        }
        foreach (var rm in meshes)
        {
            if (rm.Vao != 0)
            {
                GL.DeleteVertexArray(rm.Vao);
                #if DEBUG
                CheckGLError("GL.DeleteVertexArray");
                #endif
            }
            if (rm.Vbo != 0)
            {
                GL.DeleteBuffer(rm.Vbo);
                #if DEBUG
                CheckGLError("GL.DeleteBuffer");
                #endif
            }
            if (rm.Ebo != 0)
            {
                GL.DeleteBuffer(rm.Ebo);
                #if DEBUG
                CheckGLError("GL.DeleteBuffer");
                #endif
            }
        }
        GL.DeleteBuffer(_gridVbo);
        #if DEBUG
        CheckGLError("GL.DeleteBuffer");
        #endif
        GL.DeleteBuffer(_axesVbo);
        #if DEBUG
        CheckGLError("GL.DeleteBuffer");
        #endif
        GL.DeleteBuffer(_groundVbo);
        #if DEBUG
        CheckGLError("GL.DeleteBuffer");
        #endif
        GL.DeleteBuffer(_boneVbo);
        #if DEBUG
        CheckGLError("GL.DeleteBuffer");
        #endif
        GL.DeleteBuffer(_ikBoneVbo);
        #if DEBUG
        CheckGLError("GL.DeleteBuffer");
        #endif
        GL.DeleteBuffer(_ikBoneEbo);
        #if DEBUG
        CheckGLError("GL.DeleteBuffer");
        #endif
        GL.DeleteBuffer(_cubeVbo);
        #if DEBUG
        CheckGLError("GL.DeleteBuffer");
        #endif
        GL.DeleteBuffer(_cubeEbo);
        #if DEBUG
        CheckGLError("GL.DeleteBuffer");
        #endif
        GL.DeleteVertexArray(_gridVao);
        #if DEBUG
        CheckGLError("GL.DeleteVertexArray");
        #endif
        GL.DeleteVertexArray(_axesVao);
        #if DEBUG
        CheckGLError("GL.DeleteVertexArray");
        #endif
        GL.DeleteVertexArray(_groundVao);
        #if DEBUG
        CheckGLError("GL.DeleteVertexArray");
        #endif
        GL.DeleteVertexArray(_boneVao);
        #if DEBUG
        CheckGLError("GL.DeleteVertexArray");
        #endif
        GL.DeleteVertexArray(_ikBoneVao);
        #if DEBUG
        CheckGLError("GL.DeleteVertexArray");
        #endif
        GL.DeleteVertexArray(_cubeVao);
        #if DEBUG
        CheckGLError("GL.DeleteVertexArray");
        #endif
        GL.DeleteProgram(_program);
        #if DEBUG
        CheckGLError("GL.DeleteProgram");
        #endif
        GL.DeleteProgram(_modelProgram);
        #if DEBUG
        CheckGLError("GL.DeleteProgram");
        #endif
    }
}
