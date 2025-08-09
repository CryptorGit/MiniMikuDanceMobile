using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using MiniMikuDance.Data;
using MiniMikuDance.Import;
using MiniMikuDance.Util;

namespace MiniMikuDance.IK;

public enum IkBoneType
{
    Head,
    Chest,
    Hip,
    LeftShoulder,
    LeftHand,
    RightShoulder,
    RightHand,
    LeftKnee,
    LeftFoot,
    RightKnee,
    RightFoot
}

public static class IkManager
{
    private static readonly Dictionary<IkBoneType, List<string>> BoneNames = new()
    {
        { IkBoneType.Hip, new List<string> { "hips" } },
        { IkBoneType.Chest, new List<string> { "chest" } },
        { IkBoneType.Head, new List<string> { "head" } },
        { IkBoneType.LeftShoulder, new List<string> { "leftShoulder" } },
        { IkBoneType.LeftHand, new List<string> { "leftHand" } },
        { IkBoneType.RightShoulder, new List<string> { "rightShoulder" } },
        { IkBoneType.RightHand, new List<string> { "rightHand" } },
        { IkBoneType.LeftKnee, new List<string> { "leftKnee" } },
        { IkBoneType.LeftFoot, new List<string> { "leftFoot" } },
        { IkBoneType.RightKnee, new List<string> { "rightKnee" } },
        { IkBoneType.RightFoot, new List<string> { "rightFoot" } }
    };

    private static readonly Dictionary<IkBoneType, string[]> HumanoidFallbacks = new()
    {
        { IkBoneType.Chest, new[] { "spine" } },
        { IkBoneType.LeftShoulder, new[] { "leftUpperArm" } },
        { IkBoneType.RightShoulder, new[] { "rightUpperArm" } },
        { IkBoneType.LeftKnee, new[] { "leftLowerLeg" } },
        { IkBoneType.RightKnee, new[] { "rightLowerLeg" } }
    };

    private static readonly Dictionary<IkBoneType, IkBoneType> ParentMap = new()
    {
        { IkBoneType.Chest, IkBoneType.Hip },
        { IkBoneType.Head, IkBoneType.Chest },
        { IkBoneType.LeftShoulder, IkBoneType.Chest },
        { IkBoneType.LeftHand, IkBoneType.LeftShoulder },
        { IkBoneType.RightShoulder, IkBoneType.Chest },
        { IkBoneType.RightHand, IkBoneType.RightShoulder },
        { IkBoneType.LeftKnee, IkBoneType.Hip },
        { IkBoneType.LeftFoot, IkBoneType.LeftKnee },
        { IkBoneType.RightKnee, IkBoneType.Hip },
        { IkBoneType.RightFoot, IkBoneType.RightKnee }
    };

    private static readonly Dictionary<IkBoneType, Vector3> DefaultOffsets = new()
    {
        { IkBoneType.Chest, new Vector3(0f, 0.2f, 0f) },
        { IkBoneType.Head, new Vector3(0f, 0.2f, 0f) },
        { IkBoneType.LeftShoulder, new Vector3(-0.2f, 0.15f, 0f) },
        { IkBoneType.LeftHand, new Vector3(-0.5f, 0f, 0f) },
        { IkBoneType.RightShoulder, new Vector3(0.2f, 0.15f, 0f) },
        { IkBoneType.RightHand, new Vector3(0.5f, 0f, 0f) },
        { IkBoneType.LeftKnee, new Vector3(-0.2f, -0.4f, 0f) },
        { IkBoneType.LeftFoot, new Vector3(-0.2f, -0.8f, 0f) },
        { IkBoneType.RightKnee, new Vector3(0.2f, -0.4f, 0f) },
        { IkBoneType.RightFoot, new Vector3(0.2f, -0.8f, 0f) }
    };
    private static float _scaleFactor = 1f;

    private static bool _mappingLoaded;

    private static readonly Dictionary<IkBoneType, IkBone> BonesDict = new();
    private static readonly Dictionary<int, IkBone> BoneIndexDict = new();
    private static readonly Dictionary<int, (IIkSolver Solver, IkBone[] Chain)> Solvers = new();

    // レンダラーから提供される各種処理を委譲用デリゲートとして保持
    public static System.Func<float, float, int>? PickFunc { get; set; }
    public static System.Func<int, Vector3>? GetBonePositionFunc { get; set; }
    public static System.Func<Vector3>? GetCameraPositionFunc { get; set; }
    public static System.Action<int, OpenTK.Mathematics.Vector3>? SetBoneRotation { get; set; }
    public static System.Action<int, OpenTK.Mathematics.Vector3>? SetBoneTranslation { get; set; }
    public static System.Func<Vector3, Vector3>? ToModelSpaceFunc { get; set; }
    public static System.Action? InvalidateViewer { get; set; }

    private static int _selectedBoneIndex = -1;
    private static Plane _dragPlane;

    public static int SelectedBoneIndex => _selectedBoneIndex;
    public static Plane DragPlane => _dragPlane;

    public static IReadOnlyDictionary<IkBoneType, IkBone> Bones => BonesDict;

    private static void LoadMappings()
    {
        if (_mappingLoaded)
            return;
        try
        {
            var cfg = DataManager.Instance.LoadConfig<IkBoneMappingConfig>("IkBoneMapping");
            foreach (var kv in cfg.Mapping)
            {
                if (System.Enum.TryParse<IkBoneType>(kv.Key, out var type))
                {
                    if (!BoneNames.TryGetValue(type, out var list))
                    {
                        list = new List<string>();
                        BoneNames[type] = list;
                    }
                    foreach (var n in kv.Value)
                    {
                        if (!list.Contains(n))
                            list.Add(n);
                    }
                }
            }
        }
        catch
        {
            // ignore config load errors
        }
        _mappingLoaded = true;
    }

    private static void CalculateScale(IReadOnlyList<BoneData> modelBones)
    {
        const float defaultHipChest = 0.2f;
        _scaleFactor = 1f;

        int hipIdx = -1;
        int chestIdx = -1;
        if (BoneNames.TryGetValue(IkBoneType.Hip, out var hipNames))
            hipIdx = FindBoneIndex(modelBones, hipNames);
        if (BoneNames.TryGetValue(IkBoneType.Chest, out var chestNames))
            chestIdx = FindBoneIndex(modelBones, chestNames);

        if (hipIdx >= 0 && chestIdx >= 0)
        {
            var hip = Vector3.Transform(Vector3.Zero, modelBones[hipIdx].BindMatrix);
            var chest = Vector3.Transform(Vector3.Zero, modelBones[chestIdx].BindMatrix);
            var dist = Vector3.Distance(hip, chest);
            if (dist > 1e-5f)
                _scaleFactor = dist / defaultHipChest;
        }
        else
        {
            float total = 0f;
            int count = 0;
            for (int i = 0; i < modelBones.Count; i++)
            {
                var b = modelBones[i];
                if (b.Parent >= 0 && b.Parent < modelBones.Count)
                {
                    var p = modelBones[b.Parent];
                    var bPos = Vector3.Transform(Vector3.Zero, b.BindMatrix);
                    var pPos = Vector3.Transform(Vector3.Zero, p.BindMatrix);
                    var dist = Vector3.Distance(bPos, pPos);
                    if (dist > 1e-5f)
                    {
                        total += dist;
                        count++;
                    }
                }
            }
            if (count > 0)
            {
                var avg = total / count;
                if (avg > 1e-5f)
                    _scaleFactor = avg / defaultHipChest;
            }
        }
    }

    /// <summary>
    /// VRChat 相当の11ボーン構成を生成する
    /// </summary>
    /// <param name="modelBones">PMXモデルのボーン一覧</param>
    /// <param name="humanoidBones">ヒューマノイドボーン名とインデックスのマッピング</param>
    public static void GenerateVrChatSkeleton(IReadOnlyList<BoneData> modelBones,
        IDictionary<string, int>? humanoidBones = null,
        IReadOnlyList<(string Name, int Index)>? humanoidBoneList = null)
    {
        Clear();
        LoadMappings();
        CalculateScale(modelBones);
        foreach (IkBoneType type in System.Enum.GetValues<IkBoneType>())
        {
            if (!BoneNames.TryGetValue(type, out var names))
                continue;
            int idx = -1;
            if (humanoidBoneList != null)
            {
                foreach (var hb in humanoidBoneList)
                {
                    foreach (var n in names)
                    {
                        if (string.Equals(hb.Name, n, System.StringComparison.OrdinalIgnoreCase))
                        {
                            idx = hb.Index;
                            break;
                        }
                    }
                    if (idx >= 0)
                        break;
                }
            }

            if (idx < 0)
                idx = FindBoneIndex(modelBones, names);
            if (idx < 0 && humanoidBones != null)
            {
                string? resolved = null;
                foreach (var n in names)
                {
                    if (humanoidBones.TryGetValue(n, out idx))
                    {
                        resolved = n;
                        break;
                    }
                }
                if (idx < 0 && HumanoidFallbacks.TryGetValue(type, out var fallbacks))
                {
                    foreach (var n in fallbacks)
                    {
                        if (humanoidBones.TryGetValue(n, out idx))
                        {
                            resolved = n;
                            break;
                        }
                    }
                }

                if (idx >= 0)
                    Trace.WriteLine($"{type} を名前から特定できなかったため、Humanoidボーン '{resolved}' を使用しました。");
            }

            if (idx >= 0)
            {
                var b = modelBones[idx];
                var worldPos = Vector3.Transform(Vector3.Zero, b.BindMatrix);
                var ik = new IkBone(idx, worldPos, b.Rotation);
                BonesDict[type] = ik;
                BoneIndexDict[idx] = ik;
            }
            else
            {
                Trace.WriteLine($"{type} ボーンが見つかりません。");
                System.Console.Error.WriteLine($"IKマッピング失敗: {type} ボーンが見つかりません。");
                if (type == IkBoneType.Hip)
                {
                    BonesDict[type] = new IkBone(-1, Vector3.Zero, Quaternion.Identity);
                }
                else
                {
                    CreateVirtualBone(type);
                }
            }
        }
        SetupSolvers();
    }

    private static int FindBoneIndex(IReadOnlyList<BoneData> bones, IEnumerable<string> names)
    {
        foreach (var n in names)
        {
            for (int i = 0; i < bones.Count; i++)
            {
                if (string.Equals(bones[i].Name, n, System.StringComparison.OrdinalIgnoreCase))
                    return i;
            }
        }
        return -1;
    }

    private static void CreateVirtualBone(IkBoneType type)
    {
        if (!ParentMap.TryGetValue(type, out var parentType))
            return;
        if (!BonesDict.TryGetValue(parentType, out var parent))
            return;
        var offset = DefaultOffsets.TryGetValue(type, out var o) ? o : Vector3.Zero;
        offset *= _scaleFactor;
        var ik = new IkBone(-1, parent.Position + offset, Quaternion.Identity);
        BonesDict[type] = ik;
    }

    public static IkBone? Get(IkBoneType type)
    {
        return BonesDict.TryGetValue(type, out var bone) ? bone : null;
    }

    // レンダラーから提供された情報を用いてボーン選択を行う
    public static int PickBone(float screenX, float screenY)
    {
        if (PickFunc == null || GetBonePositionFunc == null || GetCameraPositionFunc == null)
            return -1;

        if (_selectedBoneIndex >= 0 && BoneIndexDict.TryGetValue(_selectedBoneIndex, out var prev))
            prev.IsSelected = false;

        int idx = PickFunc(screenX, screenY);
        _selectedBoneIndex = idx;
        if (idx >= 0)
        {
            if (BoneIndexDict.TryGetValue(idx, out var sel))
                sel.IsSelected = true;

            var bonePos = GetBonePositionFunc(idx);
            var camPos = GetCameraPositionFunc();
            var normal = Vector3.Normalize(camPos - bonePos);
            _dragPlane = new Plane(normal, -Vector3.Dot(normal, bonePos));
        }
        InvalidateViewer?.Invoke();
        return idx;
    }

    public static Vector3? IntersectDragPlane((Vector3 Origin, Vector3 Direction) ray)
    {
        if (_selectedBoneIndex < 0)
            return null;

        var denom = Vector3.Dot(_dragPlane.Normal, ray.Direction);
        if (System.Math.Abs(denom) < 1e-6f)
            return null;

        var t = -(Vector3.Dot(_dragPlane.Normal, ray.Origin) + _dragPlane.D) / denom;
        if (t < 0)
            return null;

        return ray.Origin + ray.Direction * t;
    }

    public static void UpdateTarget(int boneIndex, Vector3 position)
    {
        if (ToModelSpaceFunc != null)
            position = ToModelSpaceFunc(position);

        if (BoneIndexDict.TryGetValue(boneIndex, out var bone))
        {
            bone.Position = position;
            if (Solvers.TryGetValue(boneIndex, out var solver))
            {
                solver.Solver.Solve(solver.Chain);
                foreach (var b in solver.Chain)
                {
                    var delta = Quaternion.Inverse(b.BaseRotation) * b.Rotation;
                    SetBoneRotation?.Invoke(b.PmxBoneIndex, delta.ToEulerDegrees().ToOpenTK());
                    SetBoneTranslation?.Invoke(b.PmxBoneIndex, b.Position.ToOpenTK());
                }
                InvalidateViewer?.Invoke();
            }
        }
    }

    public static void ReleaseSelection()
    {
        if (_selectedBoneIndex >= 0 && BoneIndexDict.TryGetValue(_selectedBoneIndex, out var prev))
            prev.IsSelected = false;
        _selectedBoneIndex = -1;
        InvalidateViewer?.Invoke();
    }

    public static void Clear()
    {
        BonesDict.Clear();
        BoneIndexDict.Clear();
        Solvers.Clear();
        ReleaseSelection();
    }

    private static void SetupSolvers()
    {
        static float Dist(IkBone a, IkBone b) => Vector3.Distance(a.Position, b.Position);
        static bool AllMapped(params IkBone[] bones)
        {
            foreach (var b in bones)
                if (b.PmxBoneIndex < 0)
                    return false;
            return true;
        }

        Solvers.Clear();

        if (BonesDict.TryGetValue(IkBoneType.Chest, out var chest) &&
            BonesDict.TryGetValue(IkBoneType.LeftShoulder, out var ls) &&
            BonesDict.TryGetValue(IkBoneType.LeftHand, out var lh))
        {
            var chain = new[] { chest, ls, lh };
            if (AllMapped(chain))
            {
                var solver = new TwoBoneSolver(Dist(chest, ls), Dist(ls, lh));
                Solvers[lh.PmxBoneIndex] = (solver, chain);
            }
        }

        if (BonesDict.TryGetValue(IkBoneType.Chest, out chest) &&
            BonesDict.TryGetValue(IkBoneType.RightShoulder, out var rs) &&
            BonesDict.TryGetValue(IkBoneType.RightHand, out var rh))
        {
            var chain = new[] { chest, rs, rh };
            if (AllMapped(chain))
            {
                var solver = new TwoBoneSolver(Dist(chest, rs), Dist(rs, rh));
                Solvers[rh.PmxBoneIndex] = (solver, chain);
            }
        }

        if (BonesDict.TryGetValue(IkBoneType.Hip, out var hip) &&
            BonesDict.TryGetValue(IkBoneType.LeftKnee, out var lk) &&
            BonesDict.TryGetValue(IkBoneType.LeftFoot, out var lf))
        {
            var chain = new[] { hip, lk, lf };
            if (AllMapped(chain))
            {
                var solver = new TwoBoneSolver(Dist(hip, lk), Dist(lk, lf));
                Solvers[lf.PmxBoneIndex] = (solver, chain);
            }
        }

        if (BonesDict.TryGetValue(IkBoneType.Hip, out hip) &&
            BonesDict.TryGetValue(IkBoneType.RightKnee, out var rk) &&
            BonesDict.TryGetValue(IkBoneType.RightFoot, out var rf))
        {
            var chain = new[] { hip, rk, rf };
            if (AllMapped(chain))
            {
                var solver = new TwoBoneSolver(Dist(hip, rk), Dist(rk, rf));
                Solvers[rf.PmxBoneIndex] = (solver, chain);
            }
        }

        if (BonesDict.TryGetValue(IkBoneType.Hip, out hip) &&
            BonesDict.TryGetValue(IkBoneType.Chest, out chest) &&
            BonesDict.TryGetValue(IkBoneType.Head, out var head))
        {
            var chain = new[] { hip, chest, head };
            if (AllMapped(chain))
            {
                var lengths = new[] { Dist(hip, chest), Dist(chest, head) };
                var solver = new FabrikSolver(lengths);
                Solvers[head.PmxBoneIndex] = (solver, chain);
            }
        }
    }
}
