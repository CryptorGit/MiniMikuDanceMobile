using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using MiniMikuDance.Motion;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiniMikuDanceMaui;

public class TimelineGridView : GraphicsView, IDrawable
{
    public int FrameScale { get; set; } = 10; // pixel per frame
    public int RowHeight { get; set; } = 20;
    public MotionEditor? MotionEditor { get; set; }
    public MotionPlayer? MotionPlayer { get; set; }
    private readonly List<string> _bones = new();
    private readonly HashSet<(int Row, int Frame)> _selection = new();

    public TimelineGridView()
    {
        Drawable = this;
    }

    public void SetBones(IEnumerable<string> bones)
    {
        _bones.Clear();
        _bones.AddRange(bones);
        Invalidate();
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

        canvas.StrokeColor = Colors.Gray;
        canvas.FillColor = Colors.Gray;
        for (int i = 0; i <= frameCount; i++)
        {
            float x = i * FrameScale;
            canvas.DrawLine(x, 0, x, rowCount * RowHeight);

            float half = FrameScale / 2f;
            var path = new PathF();
            path.MoveTo(x - half, 0);
            path.LineTo(x + half, 0);
            path.LineTo(x, half);
            path.Close();
            canvas.FillPath(path);
        }
        for (int r = 0; r <= rowCount; r++)
        {
            float y = r * RowHeight;
            canvas.DrawLine(0, y, frameCount * FrameScale, y);
        }

        if (MotionEditor != null)
        {
            canvas.FillColor = Colors.Yellow;
            for (int r = 0; r < rowCount; r++)
            {
                var bone = _bones[r];
                if (MotionEditor.Motion.KeyFrames.TryGetValue(bone, out var set))
                {
                    foreach (var f in set)
                    {
                        float cx = f * FrameScale + FrameScale/2f;
                        float cy = r * RowHeight + RowHeight/2f;
                        canvas.FillCircle(cx, cy, 3);
                    }
                }
            }
        }

        canvas.StrokeColor = Colors.Cyan;
        foreach (var (row, frame) in _selection)
        {
            canvas.DrawRectangle(frame * FrameScale, row * RowHeight, FrameScale, RowHeight);
        }

        if (MotionPlayer != null)
        {
            float x = MotionPlayer.FrameIndex * FrameScale;
            canvas.StrokeColor = Colors.Red;
            canvas.DrawLine(x, 0, x, rowCount * RowHeight);
        }
    }
}
