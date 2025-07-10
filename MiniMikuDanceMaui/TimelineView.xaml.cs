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
    const float HeaderHeight = 30f;
    const float RowHeight = 40f; // Increased height
    const float LeftPanelWidth = 180f; // Increased width
    const float BoneNameFontSize = 24f; // Increased font size
    const float HeaderFontSize = 18f; // Increased font size

    // Data model
    public ModelData? Model { get; set; }
    private List<string> _boneNames = new List<string>();

    // UI State
    private SKPoint _lastTouchPoint;
    private float _scrollX;
    private float _scrollY;
    private int _currentFrame = 0; // For playhead position

    public event EventHandler? AddKeyClicked;
    public event EventHandler? EditKeyClicked;
    public event EventHandler? DeleteKeyClicked;

    public TimelineView()
    {
        InitializeComponent();
        Debug.WriteLine("TimelineView initialized.");
        CurrentFrameEntry.TextChanged += CurrentFrameEntry_TextChanged;
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
                var minRows = 1; 
                var actualRowCount = Math.Max(minRows, _boneNames.Count);
                var totalContentHeight = actualRowCount * RowHeight;
                
                if (TimelineCanvas.CanvasSize.Width > 0)
                {
                    _scrollX = Math.Clamp(_scrollX, 0, Math.Max(0, totalContentWidth - (TimelineCanvas.CanvasSize.Width - LeftPanelWidth)));
                }
                if (TimelineCanvas.CanvasSize.Height > 0)
                {
                    _scrollY = Math.Clamp(_scrollY, 0, Math.Max(0, totalContentHeight - TimelineCanvas.CanvasSize.Height));
                }

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

        // === 1. Draw Right Panel (Scrollable Content) ===
        canvas.Save();
        canvas.Translate(LeftPanelWidth - _scrollX, 0); // Scroll only horizontally

        var totalContentWidth = FrameCount * FrameWidth;
        var visibleRowCount = (int)((info.Height - HeaderHeight) / RowHeight);
        var actualRowCount = Math.Max(visibleRowCount, _boneNames.Count);
        var totalContentHeight = actualRowCount * RowHeight + HeaderHeight;

        // --- Draw timeline grid and keyframes ---
        canvas.Save();
        canvas.Translate(0, HeaderHeight - _scrollY); // Scroll only vertically for rows

        // Draw alternating row backgrounds
        for (int i = 0; i < actualRowCount; i++)
        {
            if (i % 2 == 1)
            {
                canvas.DrawRect(0, i * RowHeight, totalContentWidth, RowHeight, altRowPaint);
            }
        }

        // Draw horizontal lines
        for (int i = 0; i <= actualRowCount; i++)
        {
            var y = i * RowHeight;
            canvas.DrawLine(0, y, totalContentWidth, y, linePaint);
        }

        // TODO: Draw actual keyframes based on data model

        canvas.Restore();

        // --- Draw header (time ruler) ---
        canvas.DrawRect(0, 0, totalContentWidth, HeaderHeight, headerBgPaint);
        textPaint.TextSize = HeaderFontSize;
        for (int i = 0; i < FrameCount; i++)
        {
            var x = i * FrameWidth;
            canvas.DrawLine(x, 0, x, totalContentHeight, linePaint); // Vertical lines through all rows

            if (i % 5 == 0)
            {
                var text = (i).ToString();
                canvas.DrawText(text, x + 5, HeaderHeight - 10, textPaint);
            }
        }

        canvas.Restore();

        // === 2. Draw Left Panel (Fixed Content) ===
        canvas.Save();
        canvas.ClipRect(new SKRect(0, 0, LeftPanelWidth, info.Height));
        canvas.Clear(new SKColor(40, 40, 40)); // Background for the left panel

        // --- Draw header part of the left panel ---
        canvas.DrawRect(0, 0, LeftPanelWidth, HeaderHeight, headerBgPaint);
        canvas.DrawLine(0, HeaderHeight, LeftPanelWidth, HeaderHeight, linePaint);

        // --- Draw bone names ---
        canvas.Translate(0, HeaderHeight - _scrollY); // Sync vertical scroll with the right panel
        textPaint.TextSize = BoneNameFontSize;
        for (int i = 0; i < _boneNames.Count; i++)
        {
            var y = i * RowHeight;
            canvas.DrawText(_boneNames[i], 10, y + RowHeight - 10, textPaint);
        }

        canvas.Restore();

        // === 3. Draw Playhead (on top of everything) ===
        var playheadX = LeftPanelWidth + _currentFrame * FrameWidth - _scrollX;
        if (playheadX >= LeftPanelWidth)
        {
            canvas.DrawLine(playheadX, HeaderHeight, playheadX, info.Height, playheadPaint);
            canvas.DrawLine(playheadX, 0, playheadX, HeaderHeight, playheadPaint); // Also draw on header
        }
    }

    void DrawKeyframe(SKCanvas canvas, int row, int frame, SKPaint paint)
    {
        var x = frame * FrameWidth + FrameWidth / 2;
        var y = row * RowHeight + RowHeight / 2;
        var path = new SKPath();
        path.MoveTo(x, y - 10);
        path.LineTo(x + 10, y);
        path.LineTo(x, y + 10);
        path.LineTo(x - 10, y);
        path.Close();
        canvas.DrawPath(path, paint);
    }
}

