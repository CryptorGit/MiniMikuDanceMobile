using Microsoft.Maui.Controls;
using MiniMikuDance.Import;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using System.Diagnostics;

namespace MiniMikuDanceMaui;

public partial class TimelineView : ContentView
{
    const int FrameCount = 200;
    const float FrameWidth = 50f; // Increased width
    const float HeaderHeight = 45f;
    const float RowHeight = 60f; // Increased height
    const float LeftPanelWidth = 225f; // Increased width
    const float BoneNameFontSize = 36f; // Increased font size
    const float HeaderFontSize = 27f; // Increased font size

    // Data model
    public ModelData? Model { get; set; }
    public List<string> BoneNames => _boneNames;
    private List<string> _boneNames = new List<string>();
    private Dictionary<string, List<int>> _keyframes = new Dictionary<string, List<int>>();

    // UI State
    private SKPoint _lastTouchPoint;
    private float _scrollX;
    private float _scrollY;
    private int _currentFrame = 0; // For playhead position
    public int CurrentFrame => _currentFrame;

    public event EventHandler? AddKeyClicked;
    public event EventHandler? EditKeyClicked;
    public event EventHandler? DeleteKeyClicked;

    public TimelineView()
    {
        InitializeComponent();
        Debug.WriteLine("TimelineView initialized.");
        CurrentFrameEntry.TextChanged += CurrentFrameEntry_TextChanged;
    }

    public OpenTK.Mathematics.Vector3 GetBoneTranslationAtFrame(string boneName, int frame)
    {
        // TODO: Implement actual logic to get bone translation at a specific frame
        // For now, return a dummy value
        return OpenTK.Mathematics.Vector3.Zero;
    }

    public OpenTK.Mathematics.Vector3 GetBoneRotationAtFrame(string boneName, int frame)
    {
        // TODO: Implement actual logic to get bone rotation at a specific frame
        // For now, return a dummy value
        return OpenTK.Mathematics.Vector3.Zero;
    }

    private void OnPlayClicked(object? sender, EventArgs e)
    {
        // TODO: Implement play logic
    }

    private void OnPauseClicked(object? sender, EventArgs e)
    {
        // TODO: Implement pause logic
    }

    private void OnStopClicked(object? sender, EventArgs e)
    {
        _currentFrame = 0;
        CurrentFrameEntry.Text = _currentFrame.ToString();
        TimelineCanvas.InvalidateSurface();
    }

    private void CurrentFrameEntry_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (int.TryParse(e.NewTextValue, out int frame))
        {
            _currentFrame = frame;
            TimelineCanvas.InvalidateSurface();
        }
    }

    private void OnFrameMinusOneClicked(object? sender, EventArgs e)
    {
        _currentFrame = Math.Max(0, _currentFrame - 1);
        CurrentFrameEntry.Text = _currentFrame.ToString();
        TimelineCanvas.InvalidateSurface();
    }

    private void OnFramePlusOneClicked(object? sender, EventArgs e)
    {
        _currentFrame = Math.Min(FrameCount - 1, _currentFrame + 1);
        CurrentFrameEntry.Text = _currentFrame.ToString();
        TimelineCanvas.InvalidateSurface();
    }

    private void OnFrameToStartClicked(object? sender, EventArgs e)
    {
        _currentFrame = 0;
        CurrentFrameEntry.Text = _currentFrame.ToString();
        TimelineCanvas.InvalidateSurface();
    }

    private void OnFrameToEndClicked(object? sender, EventArgs e)
    {
        _currentFrame = FrameCount - 1;
        CurrentFrameEntry.Text = _currentFrame.ToString();
        TimelineCanvas.InvalidateSurface();
    }

    async void OnAddBoneClicked(object? sender, EventArgs e)
    {
        Debug.WriteLine("OnAddBoneClicked triggered.");
        if (Model == null || Application.Current?.MainPage == null)
        {
            Debug.WriteLine("Model or MainPage not available for bone selection.");
            return;
        }

        var availableBones = Model.Bones.Select(b => b.Name).Except(_boneNames).ToArray();

        if (!availableBones.Any())
        {
            await Application.Current.MainPage.DisplayAlert("No Bones", "All available bones have been added.", "OK");
            return;
        }

        string? selectedBone = await Application.Current.MainPage.DisplayActionSheet("Select a bone to add", "Cancel", null, availableBones);

        if (selectedBone != null && selectedBone != "Cancel")
        {
            _boneNames.Add(selectedBone);
            _keyframes[selectedBone] = new List<int>();
            TimelineCanvas.InvalidateSurface(); // Redraw to show the new bone
            Debug.WriteLine($"Added bone: {selectedBone}");
        }
    }

    void OnAddKeyClicked(object? sender, EventArgs e)
    {
        AddKeyClicked?.Invoke(this, EventArgs.Empty);
    }

    void OnEditKeyClicked(object? sender, EventArgs e)
    {
        EditKeyClicked?.Invoke(this, EventArgs.Empty);
    }

    void OnDeleteKeyClicked(object? sender, EventArgs e)
    {
        DeleteKeyClicked?.Invoke(this, EventArgs.Empty);
    }

    void OnCanvasTouch(object? sender, SKTouchEventArgs e)
    {
        Debug.WriteLine($"OnCanvasTouch: {e.ActionType}, Location: {e.Location}");
        switch (e.ActionType)
        {
            case SKTouchAction.Pressed:
                _lastTouchPoint = e.Location;
                break;
            case SKTouchAction.Moved:
                var deltaX = e.Location.X - _lastTouchPoint.X;
                var deltaY = e.Location.Y - _lastTouchPoint.Y;

                _scrollX -= deltaX;
                _scrollY -= deltaY;

                // Clamp scroll values
                var totalContentWidth = FrameCount * FrameWidth;
                var actualRowCount = Math.Max(1, _boneNames.Count);
                var totalContentHeight = actualRowCount * RowHeight;
                
                _scrollX = Math.Clamp(_scrollX, 0, Math.Max(0, totalContentWidth - (TimelineCanvas.CanvasSize.Width - LeftPanelWidth)));
                _scrollY = Math.Clamp(_scrollY, 0, Math.Max(0, totalContentHeight - (TimelineCanvas.CanvasSize.Height - HeaderHeight)));

                _lastTouchPoint = e.Location;
                
                TimelineCanvas.InvalidateSurface();
                Debug.WriteLine($"ScrollX: {_scrollX}, ScrollY: {_scrollY}");
                break;
        }
        // e.Handled = true; // Temporarily remove to see if it affects propagation
    }

    void OnTimelinePaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var info = e.Info;
        canvas.Clear(SKColors.Transparent);

        // Define paints
        using var linePaint = new SKPaint { Color = SKColors.Gray, StrokeWidth = 1 };
        using var altRowPaint = new SKPaint { Color = new SKColor(50, 50, 50) };
        using var headerBgPaint = new SKPaint { Color = new SKColor(30, 30, 30) };
        using var textPaint = new SKPaint { Color = SKColors.White, IsAntialias = true };
        using var playheadPaint = new SKPaint { Color = SKColors.Red, StrokeWidth = 2 };
        using var keyPaint = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill, IsAntialias = true };

        var totalContentWidth = FrameCount * FrameWidth;
        var actualRowCount = Math.Max(1, _boneNames.Count);
        var totalContentHeight = actualRowCount * RowHeight;

        // === 1. Draw Right Panel (Horizontally scrollable) ===
        canvas.Save();
        canvas.Translate(LeftPanelWidth, 0);
        canvas.ClipRect(new SKRect(0, 0, info.Width - LeftPanelWidth, info.Height));
        canvas.Translate(-_scrollX, 0);

        // --- Draw header (time ruler) ---
        canvas.DrawRect(0, 0, totalContentWidth, HeaderHeight, headerBgPaint);
        using var headerFont = new SKFont { Size = HeaderFontSize };
        for (int i = 0; i < FrameCount; i++)
        {
            var x = i * FrameWidth;
            canvas.DrawLine(x, 0, x, HeaderHeight, linePaint);
            if (i % 5 == 0)
            {
                var text = i.ToString();
                canvas.DrawText(text, x + 5, HeaderHeight - 10, headerFont, textPaint);
            }
        }

        // --- Draw timeline grid and keyframes (vertically scrollable) ---
        canvas.Save();
        canvas.Translate(0, HeaderHeight - _scrollY);

        // Draw alternating row backgrounds and horizontal lines
        for (int i = 0; i < actualRowCount; i++)
        {
            var y = i * RowHeight;
            if (i % 2 == 1)
            {
                canvas.DrawRect(0, y, totalContentWidth, RowHeight, altRowPaint);
            }
            canvas.DrawLine(0, y + RowHeight, totalContentWidth, y + RowHeight, linePaint);
        }

        // Draw vertical lines for the grid
        for (int i = 0; i < FrameCount; i++)
        {
            var x = i * FrameWidth;
            canvas.DrawLine(x, 0, x, totalContentHeight, linePaint);
        }

        // Draw keyframes
        for (int i = 0; i < _boneNames.Count; i++)
        {
            var boneName = _boneNames[i];
            if (_keyframes.TryGetValue(boneName, out var frames))
            {
                foreach (var frame in frames)
                {
                    DrawKeyframe(canvas, i, frame, keyPaint);
                }
            }
        }

        canvas.Restore(); // Restore from vertical scroll
        canvas.Restore(); // Restore from horizontal scroll

        // === 2. Draw Left Panel (Fixed Horizontally, Vertically scrollable) ===
        canvas.Save();
        canvas.ClipRect(new SKRect(0, 0, LeftPanelWidth, info.Height));
        // Draw alternating row backgrounds for left panel
        canvas.Save();
        canvas.Translate(0, HeaderHeight - _scrollY); // Sync vertical scroll
        for (int i = 0; i < _boneNames.Count; i++)
        {
            var y = i * RowHeight;
            if (i % 2 == 1)
            {
                canvas.DrawRect(0, y, LeftPanelWidth, RowHeight, altRowPaint);
            }
            else
            {
                canvas.DrawRect(0, y, LeftPanelWidth, RowHeight, new SKPaint { Color = new SKColor(40, 40, 40) }); // Base background for even rows
            }
            canvas.DrawLine(0, y + RowHeight, LeftPanelWidth, y + RowHeight, linePaint); // Horizontal line for bone names
            canvas.DrawText(_boneNames[i], 10, y + (RowHeight / 2) + (BoneNameFontSize / 3), boneNameFont, textPaint);
        }
        canvas.Restore(); // Restore from vertical scroll
        canvas.Restore(); // Restore from clip

        // === 3. Draw Playhead (on top of everything) ===
        var playheadX = LeftPanelWidth + _currentFrame * FrameWidth - _scrollX;
        if (playheadX >= LeftPanelWidth)
        {
            canvas.DrawLine(playheadX, 0, playheadX, info.Height, playheadPaint);
        }
    }

    void DrawKeyframe(SKCanvas canvas, int row, int frame, SKPaint paint)
    {
        var x = frame * FrameWidth + FrameWidth / 2;
        // Keyframe should be centered on the horizontal line at the bottom of the row
        var y_line = (row + 1) * RowHeight + HeaderHeight; // Adjusted for header height and row bottom
        var path = new SKPath();
        path.MoveTo(x, y_line - 10); // Top point of diamond
        path.LineTo(x + 10, y_line); // Right point
        path.LineTo(x, y_line + 10); // Bottom point
        path.LineTo(x - 10, y_line); // Left point
        path.Close();
        canvas.DrawPath(path, paint);
    }
}

