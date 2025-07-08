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
    public int FrameScale { get; set; } = 24; // pixel per frame
    public int RowHeight { get; set; } = 44;
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
        LogService.WriteLine("TimelineGridView created");
    }

    private void BuildGridCache(int frameCount, int rowCount)
    {
        if (Application.Current?.Resources == null)
        {
            LogService.WriteLine("Application resources not ready in BuildGridCache");
            return;
        }

        LogService.WriteLine($"BuildGridCache {frameCount} frames {rowCount} rows");

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

        var verticalLine = ((Color)Application.Current.Resources["TimelineGridVerticalLineColor"]).ToSKColor();
        var horizontalLine = ((Color)Application.Current.Resources["TimelineGridHorizontalLineColor"]).ToSKColor();
        var majorLine = ((Color)Application.Current.Resources["TimelineGridMajorLineColor"]).ToSKColor();
        using var vPaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = verticalLine, StrokeWidth = 1 };
        using var hPaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = horizontalLine, StrokeWidth = 1 };
        using var mPaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = majorLine, StrokeWidth = 1 };
        using var textPaint = new SKPaint { Style = SKPaintStyle.Fill, Color = ((Color)Application.Current.Resources["TextColor"]).ToSKColor(), IsAntialias = true };
        using var textFont = new SKFont { Size = 12 };
        for (int i = 0; i <= frameCount; i++)
        {
            float x = i * FrameScale;
            if (i % 5 == 0)
            {
                skCanvas.DrawLine(x, 0, x, rowCount * RowHeight, mPaint);
                skCanvas.DrawText(i.ToString(), x + 2, 12, SKTextAlign.Left, textFont, textPaint);
            }
            else
            {
                skCanvas.DrawLine(x, 0, x, rowCount * RowHeight, vPaint);
            }
        }
        var gridLine = ((Color)Application.Current.Resources["TimelineGridLineColor"]).ToSKColor();
        var accentLine = ((Color)Application.Current.Resources["TimelineGridAccentColor"]).ToSKColor();
        using var linePaint = new SKPaint { Style = SKPaintStyle.Stroke };
        for (int i = 0; i <= frameCount; i++)
        {
            float x = i * FrameScale;
            linePaint.Color = i % 5 == 0 ? accentLine : gridLine;
            skCanvas.DrawLine(x, 0, x, rowCount * RowHeight, linePaint);
        }
        for (int r = 0; r <= rowCount; r++)
        {
            float y = r * RowHeight;
            skCanvas.DrawLine(0, y, frameCount * FrameScale, y, hPaint);
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
        if (Application.Current?.Resources == null)
        {
            LogService.WriteLine("Application resources not ready in Draw");
            return;
        }

        LogService.WriteLine($"Draw called {dirtyRect}");

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

            var verticalLine = (Color)Application.Current.Resources["TimelineGridVerticalLineColor"];
            var horizontalLine = (Color)Application.Current.Resources["TimelineGridHorizontalLineColor"];
            var majorLine = (Color)Application.Current.Resources["TimelineGridMajorLineColor"];
            var textColor = (Color)Application.Current.Resources["TextColor"];

            for (int i = startFrame; i <= endFrame; i++)
            {
                float x = i * FrameScale;
                if (i % 5 == 0)
                {
                    canvas.StrokeColor = majorLine;
                    canvas.StrokeSize = 2;
                    canvas.DrawLine(x, startRow * RowHeight, x, endRow * RowHeight);
                    canvas.StrokeSize = 1;
                    canvas.StrokeColor = verticalLine;
                    canvas.FontColor = textColor;
                    canvas.DrawString(i.ToString(), x + 2, 0, FrameScale, RowHeight, HorizontalAlignment.Left, VerticalAlignment.Top);
                }
                else
                {
                    canvas.StrokeColor = verticalLine;
                    canvas.DrawLine(x, startRow * RowHeight, x, endRow * RowHeight);
                }
            }
            canvas.StrokeColor = horizontalLine;
            
            var gridLineColor = (Color)Application.Current.Resources["TimelineGridLineColor"];
            var accentColor = (Color)Application.Current.Resources["TimelineGridAccentColor"];
            for (int i = startFrame; i <= endFrame; i++)
            {
                float x = i * FrameScale;
                canvas.StrokeColor = i % 5 == 0 ? accentColor : gridLineColor;
                canvas.DrawLine(x, startRow * RowHeight, x, endRow * RowHeight);
            }
            canvas.StrokeColor = gridLineColor;
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
