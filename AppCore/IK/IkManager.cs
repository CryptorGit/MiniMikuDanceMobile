using System.Collections.Generic;
using System.Numerics;
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
    private static readonly Dictionary<IkBoneType, string> BoneNames = new()
    {
        { IkBoneType.Head, "head" },
        { IkBoneType.Chest, "chest" },
        { IkBoneType.Hip, "hips" },
        { IkBoneType.LeftShoulder, "leftShoulder" },
        { IkBoneType.LeftHand, "leftHand" },
        { IkBoneType.RightShoulder, "rightShoulder" },
        { IkBoneType.RightHand, "rightHand" },
        { IkBoneType.LeftKnee, "leftKnee" },
        { IkBoneType.LeftFoot, "leftFoot" },
        { IkBoneType.RightKnee, "rightKnee" },
        { IkBoneType.RightFoot, "rightFoot" }
    };

    private static readonly Dictionary<IkBoneType, IkBone> BonesDict = new();
    private static readonly Dictionary<int, IkBone> BoneIndexDict = new();
    private static readonly Dictionary<int, (IIkSolver Solver, IkBone[] Chain)> Solvers = new();

    // レンダラーから提供される各種処理を委譲用デリゲートとして保持
    public static System.Func<float, float, int>? PickFunc { get; set; }
    public static System.Func<int, Vector3>? GetBonePositionFunc { get; set; }
    public static System.Func<Vector3>? GetCameraPositionFunc { get; set; }
    public static System.Action<int, OpenTK.Mathematics.Vector3>? SetBoneRotation { get; set; }
    public static System.Action<int, OpenTK.Mathematics.Vector3>? SetBoneTranslation { get; set; }

    private static int _selectedBoneIndex = -1;
    private static Plane _dragPlane;

    public static int SelectedBoneIndex => _selectedBoneIndex;
    public static Plane DragPlane => _dragPlane;

    public static IReadOnlyDictionary<IkBoneType, IkBone> Bones => BonesDict;

    public static void Initialize(IReadOnlyList<BoneData> modelBones)
    {
        if (BonesDict.Count > 0) return;
        foreach (var kv in BoneNames)
        {
            int idx = FindBoneIndex(modelBones, kv.Value);
            if (idx >= 0)
            {
                var b = modelBones[idx];
                var ik = new IkBone(idx, b.BindMatrix.Translation, Quaternion.Identity);
                BonesDict[kv.Key] = ik;
                BoneIndexDict[idx] = ik;
            }
        }
        SetupSolvers();
    }

    private static int FindBoneIndex(IReadOnlyList<BoneData> bones, string name)
    {
        for (int i = 0; i < bones.Count; i++)
        {
            if (bones[i].Name == name) return i;
        }
        return -1;
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

        int idx = PickFunc(screenX, screenY);
        _selectedBoneIndex = idx;
        if (idx >= 0)
        {
            var bonePos = GetBonePositionFunc(idx);
            var camPos = GetCameraPositionFunc();
            var normal = Vector3.Normalize(camPos - bonePos);
            _dragPlane = new Plane(normal, -Vector3.Dot(normal, bonePos));
        }
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
        if (BoneIndexDict.TryGetValue(boneIndex, out var bone))
        {
            bone.Position = position;
            if (Solvers.TryGetValue(boneIndex, out var solver))
            {
                solver.Solver.Solve(solver.Chain);
                foreach (var b in solver.Chain)
                {
                    SetBoneRotation?.Invoke(b.PmxBoneIndex, b.Rotation.ToEulerDegrees().ToOpenTK());
                    SetBoneTranslation?.Invoke(b.PmxBoneIndex, b.Position.ToOpenTK());
                }
            }
        }
    }

    public static void ReleaseSelection()
    {
        _selectedBoneIndex = -1;
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

        Solvers.Clear();

        if (BonesDict.TryGetValue(IkBoneType.Chest, out var chest) &&
            BonesDict.TryGetValue(IkBoneType.LeftShoulder, out var ls) &&
            BonesDict.TryGetValue(IkBoneType.LeftHand, out var lh))
        {
            var solver = new TwoBoneSolver(Dist(chest, ls), Dist(ls, lh));
            var chain = new[] { chest, ls, lh };
            Solvers[lh.PmxBoneIndex] = (solver, chain);
        }

        if (BonesDict.TryGetValue(IkBoneType.Chest, out chest) &&
            BonesDict.TryGetValue(IkBoneType.RightShoulder, out var rs) &&
            BonesDict.TryGetValue(IkBoneType.RightHand, out var rh))
        {
            var solver = new TwoBoneSolver(Dist(chest, rs), Dist(rs, rh));
            var chain = new[] { chest, rs, rh };
            Solvers[rh.PmxBoneIndex] = (solver, chain);
        }

        if (BonesDict.TryGetValue(IkBoneType.Hip, out var hip) &&
            BonesDict.TryGetValue(IkBoneType.LeftKnee, out var lk) &&
            BonesDict.TryGetValue(IkBoneType.LeftFoot, out var lf))
        {
            var solver = new TwoBoneSolver(Dist(hip, lk), Dist(lk, lf));
            var chain = new[] { hip, lk, lf };
            Solvers[lf.PmxBoneIndex] = (solver, chain);
        }

        if (BonesDict.TryGetValue(IkBoneType.Hip, out hip) &&
            BonesDict.TryGetValue(IkBoneType.RightKnee, out var rk) &&
            BonesDict.TryGetValue(IkBoneType.RightFoot, out var rf))
        {
            var solver = new TwoBoneSolver(Dist(hip, rk), Dist(rk, rf));
            var chain = new[] { hip, rk, rf };
            Solvers[rf.PmxBoneIndex] = (solver, chain);
        }

        if (BonesDict.TryGetValue(IkBoneType.Hip, out hip) &&
            BonesDict.TryGetValue(IkBoneType.Chest, out chest) &&
            BonesDict.TryGetValue(IkBoneType.Head, out var head))
        {
            var lengths = new[] { Dist(hip, chest), Dist(chest, head) };
            var chain = new[] { hip, chest, head };
            var solver = new FabrikSolver(lengths);
            Solvers[head.PmxBoneIndex] = (solver, chain);
        }
    }
}
