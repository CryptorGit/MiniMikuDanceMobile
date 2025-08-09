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

    public static IReadOnlyDictionary<int, IkBone> Bones => BonesDict;

    public static void LoadPmxIkBones(IReadOnlyList<BoneData> modelBones)
    {
        Clear();
        for (int i = 0; i < modelBones.Count; i++)
        {
            var ik = modelBones[i].Ik;
            if (ik == null)
                continue;

            var bRoot = modelBones[i];
            var rootPos = Vector3.Transform(Vector3.Zero, bRoot.BindMatrix);
            BonesDict[i] = new IkBone(i, rootPos, bRoot.Rotation);

            var chainIndices = new List<int>(ik.Chain);
            chainIndices.Reverse();
            chainIndices.Add(ik.Target);

            var chain = new IkBone[chainIndices.Count];
            for (int j = 0; j < chainIndices.Count; j++)
            {
                var idx = chainIndices[j];
                var b = modelBones[idx];
                var pos = Vector3.Transform(Vector3.Zero, b.BindMatrix);
                chain[j] = new IkBone(idx, pos, b.Rotation);
            }

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
            Solvers[i] = (solver, chain);
            Trace.WriteLine($"IKチェーンを構築しました: {i} -> {string.Join(" -> ", chainIndices)}");
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

            var bonePos = GetBonePositionFunc(idx);
            var camPos = GetCameraPositionFunc();
            if (ToModelSpaceFunc != null)
            {
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
        if (BonesDict.TryGetValue(boneIndex, out var bone))
        {
            bone.Position = position;
            Trace.WriteLine($"UpdateTarget: index={boneIndex} pos={position}");
            SetBoneTranslation?.Invoke(boneIndex, position.ToOpenTK());
            if (Solvers.TryGetValue(boneIndex, out var solver))
            {
                solver.Chain[^1].Position = position;
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
        if (_selectedBoneIndex >= 0 && BonesDict.TryGetValue(_selectedBoneIndex, out var prev))
            prev.IsSelected = false;
        _selectedBoneIndex = -1;
        InvalidateViewer?.Invoke();
    }

    public static void Clear()
    {
        BonesDict.Clear();
        Solvers.Clear();
        ReleaseSelection();
    }
}

