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
    public ModelData? Model
    {
        get => _model;
        set
        {
            _model = value;
            if (_model != null)
            {
                Debug.WriteLine($"[TimelineView] Model set. Bone count: {_model.Bones.Count}");
                // Initialize bone names from the model
                _boneNames = _model.Bones.Select(b => b.Name).ToList();
                foreach (var boneName in _boneNames)
                {
                    _keyframes[boneName] = new List<int>();
                }
            }
            else
            {
                _boneNames.Clear();
                _keyframes.Clear();
            }
            UpdateCanvasSizes();
            TimelineContentCanvas.InvalidateSurface();
            BoneNameCanvas.InvalidateSurface();
        }
    }
    private ModelData? _model;
    public List<string> BoneNames => _boneNames;
    private List<string> _boneNames = new List<string>();
    private Dictionary<string, List<int>> _keyframes = new Dictionary<string, List<int>>();

    private SKFont _boneNameFont = new SKFont { Size = BoneNameFontSize };
    private SKFont _headerFont = new SKFont { Size = HeaderFontSize };

    // UI State
    
    private float _scrollX;
    private float _scrollY;
    private bool _isScrolling = false;
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
        this.Loaded += OnTimelineViewLoaded;
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
        TimelineContentCanvas.InvalidateSurface();
        BoneNameCanvas.InvalidateSurface();
    }

    private void CurrentFrameEntry_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (int.TryParse(e.NewTextValue, out int frame))
        {
            _currentFrame = frame;
            TimelineContentCanvas.InvalidateSurface();
            BoneNameCanvas.InvalidateSurface();
        }
    }

    private void OnFrameMinusOneClicked(object? sender, EventArgs e)
    {
        _currentFrame = Math.Max(0, _currentFrame - 1);
        CurrentFrameEntry.Text = _currentFrame.ToString();
        TimelineContentCanvas.InvalidateSurface();
        BoneNameCanvas.InvalidateSurface();
    }

    private void OnFramePlusOneClicked(object? sender, EventArgs e)
    {
        _currentFrame = Math.Min(FrameCount - 1, _currentFrame + 1);
        CurrentFrameEntry.Text = _currentFrame.ToString();
        TimelineContentCanvas.InvalidateSurface();
        BoneNameCanvas.InvalidateSurface();
    }

    private void OnFrameToStartClicked(object? sender, EventArgs e)
    {
        _currentFrame = 0;
        CurrentFrameEntry.Text = _currentFrame.ToString();
        TimelineContentCanvas.InvalidateSurface();
        BoneNameCanvas.InvalidateSurface();
    }

    private void OnFrameToEndClicked(object? sender, EventArgs e)
    {
        _currentFrame = FrameCount - 1;
        CurrentFrameEntry.Text = _currentFrame.ToString();
        TimelineContentCanvas.InvalidateSurface();
        BoneNameCanvas.InvalidateSurface();
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
            UpdateCanvasSizes(); // Update canvas sizes after adding a bone
            TimelineContentCanvas.InvalidateSurface(); // Redraw to show the new bone
            BoneNameCanvas.InvalidateSurface();
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

    private void OnTimelineViewLoaded(object? sender, EventArgs e)
    {
        BoneNameScrollView.Scrolled += OnBoneNameScrollViewScrolled;
        TimelineContentScrollView.Scrolled += OnTimelineContentScrollViewScrolled;
        UpdateCanvasSizes();
    }

    private void OnBoneNameScrollViewScrolled(object? sender, ScrolledEventArgs e)
    {
        if (_isScrolling) return;
        _isScrolling = true;
        _scrollY = (float)e.ScrollY;
        // Sync vertical scroll of TimelineContentScrollView
        TimelineContentScrollView.ScrollToAsync(TimelineContentScrollView.ScrollX, e.ScrollY, false).ContinueWith((t) => _isScrolling = false);
        BoneNameCanvas.InvalidateSurface(); // Redraw bone names with new scroll
    }

    private void OnTimelineContentScrollViewScrolled(object? sender, ScrolledEventArgs e)
    {
        if (_isScrolling) return;
        _isScrolling = true;
        _scrollX = (float)e.ScrollX;
        _scrollY = (float)e.ScrollY;
        // Sync vertical scroll of BoneNameScrollView
        BoneNameScrollView.ScrollToAsync(BoneNameScrollView.ScrollX, e.ScrollY, false).ContinueWith((t) => _isScrolling = false);
        TimelineContentCanvas.InvalidateSurface(); // Redraw timeline content with new scroll
    }

    private void UpdateCanvasSizes()
    {
        var actualRowCount = Math.Max(1, _boneNames.Count);
        var totalContentHeight = actualRowCount * RowHeight + HeaderHeight; // Add HeaderHeight for the top ruler

        BoneNameContentGrid.HeightRequest = totalContentHeight;
        TimelineContentGrid.HeightRequest = totalContentHeight;
        TimelineContentGrid.WidthRequest = FrameCount * FrameWidth;

        // Calculate max bone name width for BoneNameCanvas
        float maxBoneNameWidth = 0;
        if (_boneNames.Any())
        {
            foreach (var boneName in _boneNames)
            {
                maxBoneNameWidth = Math.Max(maxBoneNameWidth, _boneNameFont.MeasureText(boneName));
            }
        }
                BoneNameContentGrid.WidthRequest = Math.Max(LeftPanelWidth, maxBoneNameWidth + 20); // Add some padding

        Debug.WriteLine($"[TimelineView] UpdateCanvasSizes: actualRowCount={actualRowCount}, totalContentHeight={totalContentHeight}, maxBoneNameWidth={maxBoneNameWidth}, BoneNameContentGrid.WidthRequest={BoneNameContentGrid.WidthRequest}, TimelineContentGrid.HeightRequest={TimelineContentGrid.HeightRequest}, TimelineContentGrid.WidthRequest={TimelineContentGrid.WidthRequest}");
    }

    

    void OnBoneNamePaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var info = e.Info;
        canvas.Clear(new SKColor(40, 40, 40)); // Background for the left panel

        using var linePaint = new SKPaint { Color = SKColors.Gray, StrokeWidth = 1 };
        using var altRowPaint = new SKPaint { Color = new SKColor(50, 50, 50) };
        using var headerBgPaint = new SKPaint { Color = new SKColor(30, 30, 30) };
        using var textPaint = new SKPaint { Color = SKColors.White, IsAntialias = true };

        canvas.Save();
        canvas.Translate(0, -_scrollY);
        canvas.ClipRect(info.Rect);

        // --- Draw header part of the left panel ---
        canvas.DrawRect(0, 0, info.Width, HeaderHeight, headerBgPaint);
        canvas.DrawLine(0, HeaderHeight, info.Width, HeaderHeight, linePaint);

        // --- Draw bone names ---
        using var boneNameFont = new SKFont { Size = BoneNameFontSize };
        if (_boneNames.Count > 0)
        {
            var startRow = (int)(_scrollY / RowHeight);
            var endRow = (int)((_scrollY + info.Height) / RowHeight) + 1;
            endRow = Math.Min(endRow, _boneNames.Count);

            for (int i = startRow; i < endRow; i++)
            {
                var y = i * RowHeight + HeaderHeight;
                if (i % 2 == 1)
                {
                    canvas.DrawRect(0, y, info.Width, RowHeight, altRowPaint);
                }
                else
                {
                    canvas.DrawRect(0, y, info.Width, RowHeight, new SKPaint { Color = new SKColor(40, 40, 40) }); // Base background for even rows
                }
                canvas.DrawLine(0, y + RowHeight, info.Width, y + RowHeight, linePaint); // Horizontal line for bone names
                canvas.DrawText(_boneNames[i], 10, y + (RowHeight / 2) + (BoneNameFontSize / 3), _boneNameFont, textPaint);
            }
        }
        canvas.Restore();
    }

    void OnTimelineContentPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
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

        canvas.Save();
        canvas.Translate(-_scrollX, -_scrollY);
        canvas.ClipRect(info.Rect);

        // --- Draw header (time ruler) ---
        canvas.DrawRect(0, 0, totalContentWidth, HeaderHeight, headerBgPaint);
        for (int i = 0; i < FrameCount; i++)
        {
            var x = i * FrameWidth;
            canvas.DrawLine(x, 0, x, HeaderHeight, linePaint);
            if (i % 5 == 0)
            {
                var text = i.ToString();
                canvas.DrawText(text, x + 5, HeaderHeight - 10, _headerFont, textPaint);
            }
        }

        // --- Draw alternating row backgrounds and horizontal lines
        var startRow = (int)(_scrollY / RowHeight);
        var endRow = (int)((_scrollY + info.Height) / RowHeight) + 1;
        endRow = Math.Min(endRow, actualRowCount);

        for (int i = startRow; i < endRow; i++)
        {
            var y = i * RowHeight + HeaderHeight;
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
            canvas.DrawLine(x, HeaderHeight, x, totalContentHeight + HeaderHeight, linePaint);
        }

        // Draw keyframes
        if (_boneNames.Count > 0)
        {
            for (int i = 0; i < _boneNames.Count; i++)
            {
                var boneRowY = i * RowHeight + HeaderHeight;
                if (boneRowY + RowHeight > _scrollY && boneRowY < _scrollY + info.Height)
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
            }
        }

        // === Draw Playhead (on top of everything) ===
        var playheadX = _currentFrame * FrameWidth;
        canvas.DrawLine(playheadX, 0, playheadX, info.Height, playheadPaint);
        canvas.Restore();
    }

    public void AddKeyframe(string boneName, int frame)
    {
        if (_keyframes.TryGetValue(boneName, out var frames))
        {
            if (!frames.Contains(frame))
            {
                frames.Add(frame);
                frames.Sort();
                TimelineContentCanvas.InvalidateSurface();
            }
        }
    }

    public List<int> GetKeyframesForBone(string boneName)
    {
        if (_keyframes.TryGetValue(boneName, out var frames))
        {
            return frames;
        }
        return new List<int>();
    }

    void DrawKeyframe(SKCanvas canvas, int row, int frame, SKPaint paint)
    {
        var x = frame * FrameWidth + FrameWidth / 2;
        // Keyframe should be centered on the horizontal line at the bottom of the row
        var y_line = (row + 1) * RowHeight;
        var path = new SKPath();
        path.MoveTo(x, y_line - 10); // Top point of diamond
        path.LineTo(x + 10, y_line); // Right point
        path.LineTo(x, y_line + 10); // Bottom point
        path.LineTo(x - 10, y_line); // Left point
        path.Close();
        canvas.DrawPath(path, paint);
    }
}

