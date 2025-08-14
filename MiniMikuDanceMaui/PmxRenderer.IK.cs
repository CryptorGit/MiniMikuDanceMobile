using System;
using System.Collections.Generic;
using System.Numerics;
using MiniMikuDance;
using MiniMikuDance.App;
using MiniMikuDance.IK;
using MiniMikuDance.Util;
using OpenTK.Graphics.ES30;
using GL = OpenTK.Graphics.ES30.GL;

namespace MiniMikuDanceMaui;

public partial class PmxRenderer
{
    private int _ikBoneVao;
    private int _ikBoneVbo;
    private int _ikBoneEbo;
    private int _ikBoneIndexCount;
    private readonly List<IkBone> _ikBones = new();
    private readonly object _ikBonesLock = new();
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

    public void SetIkBones(IEnumerable<IkBone> bones)
    {
        lock (_ikBonesLock)
        {
            _ikBones.Clear();
            _ikBones.AddRange(bones);
            Nanoem.InitializeIk(_ikBones.Count);
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
            Nanoem.InitializeIk(0);
        }
    }

    private void UpdateIkBoneWorldPositions()
    {
        lock (_ikBonesLock)
        {
            if (_ikBones.Count == 0 || _worldMats.Length == 0)
            {
                return;
            }
            foreach (var ik in _ikBones)
            {
                var target = WorldToModel(ik.Position);
                var pos = new float[] { target.X, target.Y, target.Z };
                Nanoem.SolveIk(ik.ConstraintIndex, ik.PmxBoneIndex, pos);
                var solved = new Vector3(pos[0], pos[1], pos[2]);
                var worldPos = ModelToWorld(solved);
                ik.Position = worldPos;
                SetBoneTranslation(ik.PmxBoneIndex, worldPos.ToOpenTK());
            }
        }
    }

    private void EnsureIkBoneMesh()
    {
        if (_ikBoneVao != 0)
        {
            return;
        }
        const int lat = 8;
        const int lon = 8;
        var vertices = new List<float>();
        var indices = new List<ushort>();
        for (int y = 0; y <= lat; y++)
        {
            float v = (float) y / lat;
            float theta = v * MathF.PI;
            float sinTheta = MathF.Sin(theta);
            float cosTheta = MathF.Cos(theta);
            for (int x = 0; x <= lon; x++)
            {
                float u = (float) x / lon;
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
                indices.Add((ushort) first);
                indices.Add((ushort) second);
                indices.Add((ushort) (first + 1));
                indices.Add((ushort) second);
                indices.Add((ushort) (second + 1));
                indices.Add((ushort) (first + 1));
            }
        }
        _ikBoneIndexCount = indices.Count;
        _ikBoneVao = GL.GenVertexArray();
        _ikBoneVbo = GL.GenBuffer();
        _ikBoneEbo = GL.GenBuffer();

        GL.BindVertexArray(_ikBoneVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _ikBoneVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * sizeof(float), vertices.ToArray(), BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ikBoneEbo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Count * sizeof(ushort), indices.ToArray(), BufferUsageHint.StaticDraw);
        GL.BindVertexArray(0);
    }
}
