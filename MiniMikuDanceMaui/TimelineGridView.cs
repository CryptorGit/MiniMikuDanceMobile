using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using MiniMikuDance.Motion;
using Microsoft.Maui.Graphics.Skia;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiniMikuDanceMaui;

public class TimelineGridView : GraphicsView, IDrawable
{
    public int FrameScale { get; set; } = 10; // pixel per frame
    public int RowHeight { get; set; } = 48;
    public MotionEditor? MotionEditor { get; set; }
    public MotionPlayer? MotionPlayer { get; set; }
    private readonly List<string> _bones = new();
    private readonly HashSet<(int Row, int Frame)> _selection = new();
    private SKPicture? _gridCache;
    private int _cacheFrameCount;
    private int _cacheRowCount;
    private int _cacheFrameScale;
    private int _cacheRowHeight;

    public TimelineGridView()
    {
        Drawable = this;
    }

    private void BuildGridCache(int frameCount, int rowCount)
    {
        _gridCache?.Dispose();
        var recorder = new SKPictureRecorder();
        var rect = new SKRect(0, 0, frameCount * FrameScale, rowCount * RowHeight);
        var skCanvas = recorder.BeginRecording(rect);

        using var fillPaint = new SKPaint { Style = SKPaintStyle.Fill };
        for (int r = 0; r < rowCount; r++)
        {
            fillPaint.Color = SKColor.Parse(r % 2 == 0 ? "#151515" : "#202020");
            skCanvas.DrawRect(0, r * RowHeight, frameCount * FrameScale, RowHeight, fillPaint);
        }

        using var linePaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = SKColors.Gray };
        for (int i = 0; i <= frameCount; i++)
        {
            float x = i * FrameScale;
            skCanvas.DrawLine(x, 0, x, rowCount * RowHeight, linePaint);
        }
        for (int r = 0; r <= rowCount; r++)
        {
            float y = r * RowHeight;
            skCanvas.DrawLine(0, y, frameCount * FrameScale, y, linePaint);
        }

        _gridCache = recorder.EndRecording();
    }

    public void SetBones(IEnumerable<string> bones)
    {
        _bones.Clear();
        _bones.AddRange(bones);
        Invalidate();
    }

    public int AddBone(string bone)
    {
        if (_bones.Contains(bone))
            return _bones.IndexOf(bone);
        _bones.Add(bone);
        Invalidate();
        return _bones.Count - 1;
    }

    public IEnumerable<(int Row,int Frame)> Selection => _selection;

    public void ClearSelection()
    {
        _selection.Clear();
        Invalidate();
    }

    public void Select(int row, int frame, bool append = false)
    {
        if (!append) _selection.Clear();
        _selection.Add((row, frame));
        Invalidate();
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        int frameCount = MotionEditor?.Motion.Frames.Length ?? 0;
        int rowCount = _bones.Count;
        WidthRequest = frameCount * FrameScale;
        HeightRequest = rowCount * RowHeight;

        canvas.FillColor = Colors.Transparent;
        canvas.FillRectangle(0, 0, (float)WidthRequest, (float)HeightRequest);

        bool needCache = _gridCache == null ||
                         _cacheFrameCount != frameCount ||
                         _cacheRowCount != rowCount ||
                         _cacheFrameScale != FrameScale ||
                         _cacheRowHeight != RowHeight;
        if (needCache)
        {
            _cacheFrameCount = frameCount;
            _cacheRowCount = rowCount;
            _cacheFrameScale = FrameScale;
            _cacheRowHeight = RowHeight;
            BuildGridCache(frameCount, rowCount);
        }

        int startFrame = Math.Max(0, (int)Math.Floor(dirtyRect.Left / FrameScale));
        int endFrame = Math.Min(frameCount, (int)Math.Ceiling(dirtyRect.Right / FrameScale));
        int startRow = Math.Max(0, (int)Math.Floor(dirtyRect.Top / RowHeight));
        int endRow = Math.Min(rowCount, (int)Math.Ceiling(dirtyRect.Bottom / RowHeight));

        if (canvas is SkiaCanvas skCanvas && _gridCache != null)
        {
            var clip = new SKRect(startFrame * FrameScale, startRow * RowHeight, endFrame * FrameScale, endRow * RowHeight);
            skCanvas.Canvas.Save();
            skCanvas.Canvas.ClipRect(clip);
            skCanvas.Canvas.DrawPicture(_gridCache);
            skCanvas.Canvas.Restore();
        }
        else
        {
            for (int r = startRow; r < endRow; r++)
            {
                canvas.FillColor = Color.FromArgb(r % 2 == 0 ? "#151515" : "#202020");
                canvas.FillRectangle(startFrame * FrameScale, r * RowHeight, (endFrame - startFrame) * FrameScale, RowHeight);
            }

            canvas.StrokeColor = Colors.Gray;
            for (int i = startFrame; i <= endFrame; i++)
            {
                float x = i * FrameScale;
                canvas.DrawLine(x, startRow * RowHeight, x, endRow * RowHeight);
            }
            for (int r = startRow; r <= endRow; r++)
            {
                float y = r * RowHeight;
                canvas.DrawLine(startFrame * FrameScale, y, endFrame * FrameScale, y);
            }
        }

        if (MotionEditor != null)
        {
            canvas.FillColor = Colors.Yellow;
            for (int r = startRow; r < endRow; r++)
            {
                var bone = _bones[r];
                if (MotionEditor.Motion.KeyFrames.TryGetValue(bone, out var set))
                {
                    foreach (var f in set)
                    {
                        if (f < startFrame || f >= endFrame) continue;
                        float cx = f * FrameScale + FrameScale / 2f;
                        float cy = r * RowHeight + RowHeight / 2f;
                        canvas.FillCircle(cx, cy, 3);
                    }
                }
            }
        }

        canvas.StrokeColor = Color.FromArgb("#006680");
        foreach (var (row, frame) in _selection)
        {
            if (row < startRow || row >= endRow) continue;
            if (frame < startFrame || frame >= endFrame) continue;
            canvas.DrawRectangle(frame * FrameScale, row * RowHeight, FrameScale, RowHeight);
        }

        if (MotionPlayer != null)
        {
            float x = MotionPlayer.FrameIndex * FrameScale;
            if (x >= startFrame * FrameScale && x <= endFrame * FrameScale)
            {
                canvas.StrokeColor = Colors.Red;
                canvas.DrawLine(x, startRow * RowHeight, x, endRow * RowHeight);
            }
        }
    }
}
