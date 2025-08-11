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
    private static readonly Dictionary<int, (IIkSolver Solver, IkBone[] Chain)> Solvers = new();
    private static readonly HashSet<int> FullBodyEffectors = new();

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

        RegisterFullBodyIk(modelBones);
    }

    private static void RegisterIkBone(int index, BoneData bRoot, IkInfo ik, IReadOnlyList<BoneData> modelBones)
    {
        var rootPos = Vector3.Transform(Vector3.Zero, bRoot.BindMatrix);
        BonesDict[index] = new IkBone(index, rootPos, bRoot.Rotation, bRoot.BaseForward, bRoot.BaseUp);

        var chainIndices = new List<int>(ik.Chain.Count + 1);
        for (int j = ik.Chain.Count - 1; j >= 0; j--)
            chainIndices.Add(ik.Chain[j].Bone);
        chainIndices.Add(ik.Target);

        var chain = new IkBone[chainIndices.Count + 1];
        chain[0] = BonesDict[index];
        for (int j = 0; j < chainIndices.Count; j++)
        {
            var idx = chainIndices[j];
            var b = modelBones[idx];
            var pos = Vector3.Transform(Vector3.Zero, b.BindMatrix);
            chain[j + 1] = new IkBone(idx, pos, b.Rotation, b.BaseForward, b.BaseUp);
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
            var lengths = new float[solverChain.Length - 1];
            for (int j = 0; j < lengths.Length; j++)
                lengths[j] = Vector3.Distance(solverChain[j].Position, solverChain[j + 1].Position);
            solver = new FabrikSolver(lengths);
        }
        Solvers[index] = (solver, chain);
        Trace.WriteLine($"IKチェーンを構築しました: {index} -> {string.Join(" -> ", chainIndices)}");
    }

    private static int FindBoneIndex(IReadOnlyList<BoneData> bones, params string[] keywords)
    {
        for (int i = 0; i < bones.Count; i++)
        {
            var name = bones[i].Name;
            foreach (var k in keywords)
            {
                if (name.Contains(k, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
        }
        return -1;
    }

    private static IkBone[] BuildChain(IReadOnlyList<BoneData> bones, params int[] indices)
    {
        var chain = new IkBone[indices.Length];
        for (int i = 0; i < indices.Length; i++)
        {
            var idx = indices[i];
            var b = bones[idx];
            var pos = Vector3.Transform(Vector3.Zero, b.BindMatrix);
            if (!BonesDict.TryGetValue(idx, out var ikBone))
            {
                ikBone = new IkBone(idx, pos, b.Rotation, b.BaseForward, b.BaseUp);
                BonesDict[idx] = ikBone;
            }
            chain[i] = ikBone;
        }
        return chain;
    }

    private static void RegisterFullBodyIk(IReadOnlyList<BoneData> modelBones)
    {
        int hips = FindBoneIndex(modelBones, "腰", "センター", "hips", "hip", "center");
        if (hips < 0)
        {
            Trace.WriteLine("Hips bone not found for FullBodyIK.");
            return;
        }

        var segments = new List<(IIkSolver solver, IkBone[] chain)>();

        void AddSegment(int endIdx, int midIdx = -1)
        {
            if (endIdx < 0)
                return;
            IkBone[] chain = midIdx >= 0 ? BuildChain(modelBones, hips, midIdx, endIdx) : BuildChain(modelBones, hips, endIdx);
            IIkSolver solver;
            if (chain.Length == 3)
            {
                float l1 = Vector3.Distance(chain[0].Position, chain[1].Position);
                float l2 = Vector3.Distance(chain[1].Position, chain[2].Position);
                solver = new TwoBoneSolver(l1, l2);
            }
            else
            {
                var lengths = new float[chain.Length - 1];
                for (int j = 0; j < lengths.Length; j++)
                    lengths[j] = Vector3.Distance(chain[j].Position, chain[j + 1].Position);
                solver = new FabrikSolver(lengths);
            }
            segments.Add((solver, chain));
        }

        int head = FindBoneIndex(modelBones, "頭", "head");
        if (head >= 0)
            AddSegment(head);

        int leftElbow = FindBoneIndex(modelBones, "左ひじ", "左肘", "leftelbow");
        int leftWrist = FindBoneIndex(modelBones, "左手首", "左手", "leftwrist", "left wrist");
        if (leftWrist >= 0)
            AddSegment(leftWrist, leftElbow);

        int rightElbow = FindBoneIndex(modelBones, "右ひじ", "右肘", "rightelbow");
        int rightWrist = FindBoneIndex(modelBones, "右手首", "右手", "rightwrist", "right wrist");
        if (rightWrist >= 0)
            AddSegment(rightWrist, rightElbow);

        int leftKnee = FindBoneIndex(modelBones, "左ひざ", "左膝", "leftknee");
        int leftAnkle = FindBoneIndex(modelBones, "左足首", "leftankle", "left ankle");
        if (leftAnkle >= 0)
            AddSegment(leftAnkle, leftKnee);

        int rightKnee = FindBoneIndex(modelBones, "右ひざ", "右膝", "rightknee");
        int rightAnkle = FindBoneIndex(modelBones, "右足首", "rightankle", "right ankle");
        if (rightAnkle >= 0)
            AddSegment(rightAnkle, rightKnee);

        if (segments.Count == 0)
            return;

        var fbSolver = new FullBodyIkSolver(segments.ToArray());
        foreach (var segment in segments)
        {
            int effector = segment.chain[^1].PmxBoneIndex;
            Solvers[effector] = (fbSolver, segment.chain);
            FullBodyEffectors.Add(effector);
        }
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

            if (FullBodyEffectors.Contains(idx))
                Trace.WriteLine($"FullBodyIK effector selected: {idx}");

            var bonePos = GetBonePositionFunc(idx);
            var camPos = GetCameraPositionFunc();
            if (ToModelSpaceFunc != null)
            {
                // レンダラー側の WorldToModel ではZ軸のみが反転するため、
                // ドラッグ平面は同一のモデル座標系(反転後)で構築される。
                // X/Y軸はそのまま一致している。
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
            // レイの始点・方向ともにモデル座標系へ変換する。
            // Z軸が反転するが、X/Y軸は反転しない。
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
        {
            // Z 軸反転によりレイ方向が逆転した場合は補正する
            dir = -dir;
            denom = Vector3.Dot(_dragPlane.Normal, dir);
            if (System.Math.Abs(denom) < 1e-6f)
                return null;
            t = -(Vector3.Dot(_dragPlane.Normal, origin) + _dragPlane.D) / denom;
            if (t < 0)
                return null;
        }

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
            if (SetBoneTranslation == null)
            {
                Trace.WriteLine("SetBoneTranslation delegate is null.");
                return;
            }
            var targetWorld = ToWorldSpaceFunc != null ? ToWorldSpaceFunc(position) : position;
            SetBoneTranslation(boneIndex, targetWorld.ToOpenTK());
            if (!Solvers.TryGetValue(boneIndex, out var solver))
            {
                Trace.WriteLine($"Solver not found for bone index {boneIndex}.");
                return;
            }

            var chain = solver.Chain;
            if (chain.Length < 2)
                return;

            var ikSolver = solver.Solver;
            if (ikSolver is FullBodyIkSolver fbSolver)
            {
                foreach (var b in chain)
                {
                    b.Position = b.BasePosition;
                    b.Rotation = b.BaseRotation;
                }

                chain[^1].Position = position;
                fbSolver.Solve(chain);

                var parentRot = Quaternion.Identity;
                foreach (var b in chain)
                {
                    var localRot = parentRot == Quaternion.Identity ? b.Rotation : Quaternion.Inverse(parentRot) * b.Rotation;
                    var delta = Quaternion.Inverse(b.BaseRotation) * localRot;
                    if (SetBoneRotation != null)
                        SetBoneRotation(b.PmxBoneIndex, delta.ToEulerDegrees().ToOpenTK());
                    parentRot = b.Rotation;
                }
                InvalidateViewer?.Invoke();
                return;
            }

            var root = chain[0];
            var deltaRoot = root.Position - root.BasePosition;
            for (int i = 1; i < chain.Length; i++)
                chain[i].Position = chain[i].BasePosition + deltaRoot;

            var solveChain = chain[1..];
            solveChain[^1].Position = position;
            ikSolver.Solve(solveChain);
            var parent = Quaternion.Identity;
            foreach (var b in solveChain)
            {
                var localRot = parent == Quaternion.Identity ? b.Rotation : Quaternion.Inverse(parent) * b.Rotation;
                var delta = Quaternion.Inverse(b.BaseRotation) * localRot;
                if (SetBoneRotation != null)
                    SetBoneRotation(b.PmxBoneIndex, delta.ToEulerDegrees().ToOpenTK());
                parent = b.Rotation;
            }
            InvalidateViewer?.Invoke();
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"UpdateTarget exception: {ex}");
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
        Trace.WriteLine($"IkManager.Clear: SelectedBoneIndex={_selectedBoneIndex} Bones={BonesDict.Count}");
    }
}

