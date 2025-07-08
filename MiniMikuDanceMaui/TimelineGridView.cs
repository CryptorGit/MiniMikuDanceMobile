using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Skia;
using MiniMikuDance.Motion;
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
        var evenRow = ((Color)Application.Current.Resources["TimelineRowEvenColor"]).ToSKColor();
        var oddRow = ((Color)Application.Current.Resources["TimelineRowOddColor"]).ToSKColor();
        for (int r = 0; r < rowCount; r++)
        {
            fillPaint.Color = r % 2 == 0 ? evenRow : oddRow;
            skCanvas.DrawRect(0, r * RowHeight, frameCount * FrameScale, RowHeight, fillPaint);
        }

        var gridLine = ((Color)Application.Current.Resources["TimelineGridLineColor"]).ToSKColor();
        using var linePaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = gridLine };
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

    public (int Row, int Frame)? HitTest(PointF point)
    {
        if (MotionEditor == null)
            return null;

        int row = (int)(point.Y / RowHeight);
        if (row < 0 || row >= _bones.Count)
            return null;

        var bone = _bones[row];
        if (!MotionEditor.Motion.KeyFrames.TryGetValue(bone, out var set))
            return null;

        const float radius = 16f;
        float r2 = radius * radius;
        foreach (var frame in set)
        {
            float cx = frame * FrameScale + FrameScale / 2f;
            float cy = row * RowHeight + RowHeight / 2f;
            float dx = point.X - cx;
            float dy = point.Y - cy;
            if (dx * dx + dy * dy <= r2)
                return (row, frame);
        }

        return null;
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
            var evenRow = (Color)Application.Current.Resources["TimelineRowEvenColor"];
            var oddRow = (Color)Application.Current.Resources["TimelineRowOddColor"];
            for (int r = startRow; r < endRow; r++)
            {
                canvas.FillColor = r % 2 == 0 ? evenRow : oddRow;
                canvas.FillRectangle(startFrame * FrameScale, r * RowHeight, (endFrame - startFrame) * FrameScale, RowHeight);
            }

            canvas.StrokeColor = (Color)Application.Current.Resources["TimelineGridLineColor"];
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
            canvas.FillColor = (Color)Application.Current.Resources["TimelineKeyFrameColor"];
            const float size = 7f; // half of 14dp
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

                        canvas.SaveState();
                        canvas.Translate(cx, cy);
                        canvas.Rotate(45);
                        canvas.FillColor = Colors.White;
                        canvas.FillRectangle(-size, -size, size * 2, size * 2);
                        if (_selection.Contains((r, f)))
                        {
                            canvas.StrokeColor = Color.FromArgb("#006680");
                            canvas.StrokeSize = 2;
                            canvas.DrawRectangle(-size, -size, size * 2, size * 2);
                        }
                        canvas.RestoreState();
                    }
                }
            }
        }

        canvas.StrokeColor = (Color)Application.Current.Resources["TimelineSelectionColor"];
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
                canvas.StrokeColor = (Color)Application.Current.Resources["TimelinePlayheadColor"];
                canvas.DrawLine(x, startRow * RowHeight, x, endRow * RowHeight);

                canvas.FillColor = Colors.Red;
                float half = FrameScale / 2f;
                var path = new PathF();
                path.MoveTo(x, 0);
                path.LineTo(x - half, FrameScale);
                path.LineTo(x + half, FrameScale);
                path.Close();
                canvas.FillPath(path);
            }
        }
    }
}
