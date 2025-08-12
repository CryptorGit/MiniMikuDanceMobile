using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using MiniMikuDance.Import;
using MiniMikuDance.Util;

namespace MiniMikuDance.IK;

public static class IkManager
{
    private static readonly Dictionary<int, IkBone> BonesDict = new();
    private static readonly Dictionary<int, (IIkSolver Solver, IkBone[] Chain, IkLink[] Links, int Iterations)> Solvers = new();
    private static readonly Dictionary<int, Vector3> FixedAxes = new();

    // レンダラーから提供される各種処理を委譲用デリゲートとして保持
    public static System.Func<float, float, int>? PickFunc { get; set; }
    public static System.Func<int, Vector3>? GetBonePositionFunc { get; set; }
    public static System.Func<Vector3>? GetCameraPositionFunc { get; set; }
    public static System.Action<int, OpenTK.Mathematics.Vector3>? SetBoneRotation { get; set; }
    public static System.Action<int, OpenTK.Mathematics.Vector3>? SetBoneTranslation { get; set; }
    public static System.Func<Vector3, Vector3>? ToModelSpaceFunc { get; set; }
    public static System.Func<Vector3, Vector3>? ToWorldSpaceFunc { get; set; }
    public static System.Action? InvalidateViewer { get; set; }

    private static int _selectedBoneIndex = -1;
    private static Plane _dragPlane;

    public static int SelectedBoneIndex => _selectedBoneIndex;
    public static Plane DragPlane => _dragPlane;

    public static IReadOnlyDictionary<int, IkBone> Bones => BonesDict;

    public static void LoadPmxIkBones(IReadOnlyList<BoneData> modelBones)
    {
        Clear();
        for (int i = 0; i < modelBones.Count; i++)
        {
            if (modelBones[i].HasFixedAxis)
                FixedAxes[i] = Vector3.Normalize(modelBones[i].FixedAxis);
        }
        for (int i = 0; i < modelBones.Count; i++)
        {
            var ik = modelBones[i].Ik;
            if (ik == null)
                continue;

            RegisterIkBone(i, modelBones[i], ik, modelBones);
        }

        // "足IK" ボーンが取りこぼされていないか確認する
        for (int i = 0; i < modelBones.Count; i++)
        {
            if (BonesDict.ContainsKey(i))
                continue;
            var name = modelBones[i].Name;
            if (name.Contains("足", StringComparison.Ordinal) &&
                (name.Contains("IK", StringComparison.OrdinalIgnoreCase) || name.Contains("ＩＫ")))
            {
                var ik = modelBones[i].Ik;
                if (ik != null)
                    RegisterIkBone(i, modelBones[i], ik, modelBones);
            }
        }
    }

    private static void RegisterIkBone(int index, BoneData bRoot, IkInfo ik, IReadOnlyList<BoneData> modelBones)
    {
        var rootPos = Vector3.Transform(Vector3.Zero, bRoot.BindMatrix);
        BonesDict[index] = new IkBone(index, rootPos, bRoot.Rotation, bRoot.BaseForward, bRoot.BaseUp)
        {
            RotationLimit = ik.RotationLimit,
            PoleVector = ik.PoleVector
        };

        var chainIndices = new List<int>(ik.Links.Count + 1);
        var ikLinks = new IkLink[ik.Links.Count];
        for (int j = ik.Links.Count - 1, k = 0; j >= 0; j--, k++)
        {
            var link = ik.Links[j];
            chainIndices.Add(link.BoneIndex);
            ikLinks[k] = link;
        }
        chainIndices.Add(ik.Target);

        var chain = new IkBone[chainIndices.Count + 1];
        chain[0] = BonesDict[index];
        for (int j = 0; j < chainIndices.Count; j++)
        {
            var idx = chainIndices[j];
            var b = modelBones[idx];
            var pos = Vector3.Transform(Vector3.Zero, b.BindMatrix);
            chain[j + 1] = new IkBone(idx, pos, b.Rotation, b.BaseForward, b.BaseUp)
            {
                RotationLimit = ik.RotationLimit
            };
        }

        var solverChain = chain[1..];
        IIkSolver solver;
        if (solverChain.Length == 3)
        {
            float l1 = Vector3.Distance(solverChain[0].Position, solverChain[1].Position);
            float l2 = Vector3.Distance(solverChain[1].Position, solverChain[2].Position);
            solver = new TwoBoneSolver(l1, l2);
        }
        else
        {
            solver = new CcdSolver();
        }
        Solvers[index] = (solver, chain, ikLinks, ik.Iterations);
        Trace.WriteLine($"IKチェーンを構築しました: {index} -> {string.Join(" -> ", chainIndices)}");
    }

    // レンダラーから提供された情報を用いてボーン選択を行う
    public static int PickBone(float screenX, float screenY)
    {
        if (PickFunc == null || GetBonePositionFunc == null || GetCameraPositionFunc == null)
            return -1;

        if (_selectedBoneIndex >= 0 && BonesDict.TryGetValue(_selectedBoneIndex, out var prev))
            prev.IsSelected = false;

        int idx = PickFunc(screenX, screenY);
        _selectedBoneIndex = idx;
        if (idx >= 0)
        {
            if (BonesDict.TryGetValue(idx, out var sel))
                sel.IsSelected = true;

            var bonePos = GetBonePositionFunc(idx);
            var camPos = GetCameraPositionFunc();
            if (ToModelSpaceFunc != null)
            {
                // レンダラー提供の WorldToModel で座標系を揃える
                bonePos = ToModelSpaceFunc(bonePos);
                camPos = ToModelSpaceFunc(camPos);
            }
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

        var origin = ray.Origin;
        var dir = ray.Direction;
        if (ToModelSpaceFunc != null)
        {
            // レイの始点・方向ともにモデル座標系へ変換する
            var originModel = ToModelSpaceFunc(origin);
            var dirEnd = ToModelSpaceFunc(origin + dir);
            dir = Vector3.Normalize(dirEnd - originModel);
            origin = originModel;
        }

        var denom = Vector3.Dot(_dragPlane.Normal, dir);
        if (System.Math.Abs(denom) < 1e-6f)
            return null;

        var t = -(Vector3.Dot(_dragPlane.Normal, origin) + _dragPlane.D) / denom;
        if (t < 0)
            return null;

        return origin + dir * t;
    }

    public static void UpdateTarget(int boneIndex, Vector3 position)
    {
        try
        {
            if (!BonesDict.TryGetValue(boneIndex, out var bone))
                return;

            bone.Position = position;
            Trace.WriteLine($"UpdateTarget: index={boneIndex} pos={position}");
            if (!Solvers.TryGetValue(boneIndex, out var solver))
            {
                Trace.WriteLine($"Solver not found for bone index {boneIndex}.");
                return;
            }

            var chain = solver.Chain;
            var links = solver.Links;
            var iterations = solver.Iterations;
            if (chain.Length < 2)
                return;

            var root = chain[0];
            var deltaRoot = root.Position - root.BasePosition;
            for (int i = 1; i < chain.Length; i++)
                chain[i].Position = chain[i].BasePosition + deltaRoot;

            var solveChain = chain[1..];
            var ikSolver = solver.Solver;
            solveChain[^1].Position = position;
            ClampChainRotations(solveChain, links, chain[0].Rotation);
            ikSolver.Solve(solveChain, links, iterations, rotationLimit: chain[0].RotationLimit);

            if (SetBoneTranslation != null)
            {
                foreach (var b in chain)
                {
                    var worldPos = ToWorldSpaceFunc != null ? ToWorldSpaceFunc(b.Position) : b.Position;
                    SetBoneTranslation(b.PmxBoneIndex, worldPos.ToOpenTK());
                }
            }
            else
            {
                Trace.WriteLine("SetBoneTranslation delegate is null.");
            }

            var parentRot = Quaternion.Identity;
            foreach (var b in solveChain)
            {
                var localRot = parentRot == Quaternion.Identity ? b.Rotation : Quaternion.Inverse(parentRot) * b.Rotation;
                var delta = Quaternion.Inverse(b.BaseRotation) * localRot;
                if (FixedAxes.TryGetValue(b.PmxBoneIndex, out var axis))
                    delta = ProjectRotation(delta, axis);
                if (SetBoneRotation != null)
                    SetBoneRotation(b.PmxBoneIndex, delta.ToEulerDegrees().ToOpenTK());
                parentRot = b.Rotation;
            }
            InvalidateViewer?.Invoke();
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"UpdateTarget exception: {ex}");
        }
    }

    private static void ClampChainRotations(IkBone[] chain, IkLink[] links, Quaternion rootRot)
    {
        var parent = rootRot;
        for (int i = 0; i < links.Length && i < chain.Length - 1; i++)
        {
            var link = links[i];
            var bone = chain[i];
            if (link.HasLimit)
            {
                var local = Quaternion.Inverse(parent) * bone.Rotation;
                var euler = local.ToEulerDegrees() * (MathF.PI / 180f);
                var clamped = Vector3.Clamp(euler, link.MinAngle, link.MaxAngle);
                var deg = clamped * (180f / MathF.PI);
                bone.Rotation = parent * deg.FromEulerDegrees();
            }
            parent = bone.Rotation;
        }
    }

    public static void ReleaseSelection()
    {
        if (_selectedBoneIndex >= 0 && BonesDict.TryGetValue(_selectedBoneIndex, out var prev))
            prev.IsSelected = false;
        _selectedBoneIndex = -1;
        InvalidateViewer?.Invoke();
    }

    public static void Clear()
    {
        ReleaseSelection();
        BonesDict.Clear();
        Solvers.Clear();
        FixedAxes.Clear();
        Trace.WriteLine($"IkManager.Clear: SelectedBoneIndex={_selectedBoneIndex} Bones={BonesDict.Count}");
    }

    private static Quaternion ProjectRotation(Quaternion q, Vector3 axis)
    {
        axis = Vector3.Normalize(axis);
        if (axis == Vector3.Zero)
            return Quaternion.Identity;
        q = Quaternion.Normalize(q);
        var w = Math.Clamp(q.W, -1f, 1f);
        float angle = 2f * MathF.Acos(w);
        float s = MathF.Sqrt(MathF.Max(1f - w * w, 0f));
        Vector3 qAxis = s < 1e-6f ? axis : new Vector3(q.X / s, q.Y / s, q.Z / s);
        float proj = Vector3.Dot(qAxis, axis);
        return Quaternion.CreateFromAxisAngle(axis, angle * proj);
    }
}

