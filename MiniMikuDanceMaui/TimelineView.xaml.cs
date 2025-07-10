using Microsoft.Maui.Controls;
using MiniMikuDance.Import;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using System.Diagnostics;

namespace MiniMikuDanceMaui;

public partial class TimelineView : ContentView
{
    const int FrameCount = 200;
    const float FrameWidth = 40f;
    const float RowHeight = 30f;
    const float LeftPanelWidth = 150f;

    // Data model
    public ModelData? Model { get; set; }
    private List<string> _boneNames = new List<string>();

    // UI State
    private SKPoint _lastTouchPoint;
    private float _scrollX;
    private float _scrollY;
    private int _currentFrame = 0; // For playhead position

    public TimelineView()
    {
        InitializeComponent();
        Debug.WriteLine("TimelineView initialized.");
        CurrentFrameEntry.TextChanged += CurrentFrameEntry_TextChanged;
    }

    private void CurrentFrameEntry_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (int.TryParse(e.NewTextValue, out int frame))
        {
            _currentFrame = frame;
            TimelineHeaderCanvas.InvalidateSurface();
            TimelineCanvas.InvalidateSurface();
        }
    }

    private void OnFrameMinusOneClicked(object? sender, EventArgs e)
    {
        _currentFrame = Math.Max(0, _currentFrame - 1);
        CurrentFrameEntry.Text = _currentFrame.ToString();
        TimelineHeaderCanvas.InvalidateSurface();
        TimelineCanvas.InvalidateSurface();
    }

    private void OnFramePlusOneClicked(object? sender, EventArgs e)
    {
        _currentFrame = Math.Min(FrameCount - 1, _currentFrame + 1);
        CurrentFrameEntry.Text = _currentFrame.ToString();
        TimelineHeaderCanvas.InvalidateSurface();
        TimelineCanvas.InvalidateSurface();
    }

    private void OnFrameToStartClicked(object? sender, EventArgs e)
    {
        _currentFrame = 0;
        CurrentFrameEntry.Text = _currentFrame.ToString();
        TimelineHeaderCanvas.InvalidateSurface();
        TimelineCanvas.InvalidateSurface();
    }

    private void OnFrameToEndClicked(object? sender, EventArgs e)
    {
        _currentFrame = FrameCount - 1;
        CurrentFrameEntry.Text = _currentFrame.ToString();
        TimelineHeaderCanvas.InvalidateSurface();
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
        Debug.WriteLine("Add Key clicked.");
        if (Application.Current?.MainPage != null)
        {
            Application.Current.MainPage.DisplayAlert("Add Key", "Add Key window would appear here.", "OK");
        }
    }

    void OnEditKeyClicked(object? sender, EventArgs e)
    {
        Debug.WriteLine("Edit Key clicked.");
        if (Application.Current?.MainPage != null)
        {
            Application.Current.MainPage.DisplayAlert("Edit Key", "Edit Key window would appear here.", "OK");
        }
    }

    void OnDeleteKeyClicked(object? sender, EventArgs e)
    {
        Debug.WriteLine("Delete Key clicked.");
        if (Application.Current?.MainPage != null)
        {
            Application.Current.MainPage.DisplayAlert("Delete Key", "Delete Key window would appear here.", "OK");
        }
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
                
                TimelineHeaderCanvas.InvalidateSurface();
                TimelineCanvas.InvalidateSurface();
                Debug.WriteLine($"ScrollX: {_scrollX}, ScrollY: {_scrollY}");
                break;
        }
        // e.Handled = true; // Temporarily remove to see if it affects propagation
    }

    void OnTimelineHeaderPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        Debug.WriteLine("OnTimelineHeaderPaintSurface called.");
        var canvas = e.Surface.Canvas;
        var info = e.Info;

        canvas.Clear(SKColors.Transparent);

        using var linePaint = new SKPaint { Color = SKColors.Gray, StrokeWidth = 1 };
        using var textFont = new SKFont { Size = 20 }; // Increased font size
        using var textPaint = new SKPaint { Color = SKColors.White };
        using var playheadPaint = new SKPaint { Color = SKColors.Red, StrokeWidth = 2 };

        canvas.Save();
        canvas.Translate(-_scrollX, 0);

        for (int i = 0; i < FrameCount; i++)
        {
            var x = i * FrameWidth;
            canvas.DrawLine(x, 0, x, info.Height, linePaint);

            if (i % 5 == 0)
            {
                var text = (i + 1).ToString();
                canvas.DrawText(text, x + 5, info.Height - 5, textFont, textPaint);
            }
        }
        
        canvas.Restore();

        var playheadX = _currentFrame * FrameWidth - _scrollX; 
        canvas.DrawLine(playheadX, 0, playheadX, info.Height, playheadPaint);
        
        // Temporary: Draw a solid rectangle to confirm playhead position
        canvas.DrawRect(new SKRect(playheadX - 5, 0, playheadX + 5, info.Height), new SKPaint { Color = SKColors.Blue, Style = SKPaintStyle.Fill });

        using var separatorPaint = new SKPaint { Color = new SKColor(60, 60, 60), Style = SKPaintStyle.Fill };
        canvas.DrawRect(new SKRect(0, 0, LeftPanelWidth, info.Height), separatorPaint);
    }

    void OnTimelinePaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        Debug.WriteLine("OnTimelinePaintSurface called.");
        var canvas = e.Surface.Canvas;
        var info = e.Info;
        var rowCount = _boneNames.Count;

        canvas.Clear(SKColors.Transparent);

        // === Draw Right Panel (Scrollable Content) ===
        canvas.Save();
        canvas.Translate(LeftPanelWidth - _scrollX, -_scrollY);

        using var linePaint = new SKPaint { Color = SKColors.Gray, StrokeWidth = 1 };
        using var altRowPaint = new SKPaint { Color = new SKColor(60, 60, 60) };
        using var keyPaint = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill };

        var minRows = 1; 
        var actualRowCount = Math.Max(minRows, rowCount);

        for (int i = 0; i < actualRowCount; i++)
        {
            var y = i * RowHeight;
            if (i % 2 == 1)
            {
                canvas.DrawRect(0, y, FrameCount * FrameWidth, RowHeight, altRowPaint);
            }
        }

        for (int i = 0; i < FrameCount; i++)
        {
            var x = i * FrameWidth;
            canvas.DrawLine(x, 0, x, actualRowCount * RowHeight, linePaint);
        }
        
        for (int i = 0; i < actualRowCount; i++)
        {
            var y = i * RowHeight;
            canvas.DrawLine(0, y + RowHeight, FrameCount * FrameWidth, y + RowHeight, linePaint);
        }

        // TODO: Draw actual keyframes based on data model
        
        canvas.Restore();

        // === Draw Left Panel (Fixed Content) ===
        canvas.Save();
        canvas.ClipRect(new SKRect(0, 0, LeftPanelWidth, info.Height));
        canvas.Clear(new SKColor(40, 40, 40));
        
        canvas.Translate(0, -_scrollY);

        using var boneFont = new SKFont { Size = 20 }; // Increased font size
        using var boneTextPaint = new SKPaint { Color = SKColors.White };
        using var buttonPaint = new SKPaint { Color = SKColors.DarkGray };
        using var buttonFont = new SKFont { Size = 20 }; // Increased font size
        using var buttonTextPaint = new SKPaint { Color = SKColors.White };

        for (int i = 0; i < rowCount; i++)
        {
            var y = i * RowHeight;
            canvas.DrawLine(0, y + RowHeight, LeftPanelWidth, y + RowHeight, linePaint);
            canvas.DrawText(_boneNames[i], 10, y + 20, boneFont, boneTextPaint);
        }
        
        canvas.Restore();
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

