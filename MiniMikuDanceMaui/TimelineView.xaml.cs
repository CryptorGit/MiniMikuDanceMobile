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
    const float LeftPanelWidth = 110f; // Adjusted width for bone name panel to match XAML
    const string RightFootBoneName = "rightFoot";
    private float _maxScrollY = 0;
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
                // Define the set of required Humanoid bones for VRM 0.x
                var requiredHumanoidBones = new List<string>
                {
                    "hips", "spine", "chest", "neck", "head",
                    "leftUpperArm", "leftLowerArm", "leftHand",
                    "rightUpperArm", "rightLowerArm", "rightHand",
                    "leftUpperLeg", "leftLowerLeg", "leftFoot",
                    "rightUpperLeg", "rightLowerLeg", "rightFoot"
                };

                // Initialize bone names from the model, prioritizing VRM 0.x bones if present
                _boneNames.Clear();
                _keyframes.Clear();

                foreach (var requiredBone in requiredHumanoidBones)
                {
                    if (_model.HumanoidBoneList.Any(b => b.Name == requiredBone))
                    {
                        _boneNames.Add(requiredBone);
                        _keyframes[requiredBone] = new List<int>();
                    }
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
        BoneNameScrollView.SizeChanged += OnScrollViewSizeChanged;
        TimelineContentScrollView.SizeChanged += OnScrollViewSizeChanged;
        UpdateCanvasSizes();
    }

    private void OnBoneNameScrollViewScrolled(object? sender, ScrolledEventArgs e)
    {
        if (_isScrolling) return;
        _isScrolling = true;
        Debug.WriteLine($"[TimelineView] OnBoneNameScrollViewScrolled: e.ScrollY={e.ScrollY}, _maxScrollY={_maxScrollY}");
        _scrollY = (float)Math.Min(e.ScrollY, _maxScrollY);
        // Sync vertical scroll of TimelineContentScrollView
        TimelineContentScrollView.ScrollToAsync(TimelineContentScrollView.ScrollX, _scrollY, false).ContinueWith((t) => _isScrolling = false);
        BoneNameCanvas.InvalidateSurface(); // Redraw bone names with new scroll
    }

    private void OnTimelineContentScrollViewScrolled(object? sender, ScrolledEventArgs e)
    {
        if (_isScrolling) return;
        _isScrolling = true;
        Debug.WriteLine($"[TimelineView] OnTimelineContentScrollViewScrolled: e.ScrollY={e.ScrollY}, _maxScrollY={_maxScrollY}");
        _scrollX = (float)e.ScrollX;
        _scrollY = (float)Math.Min(e.ScrollY, _maxScrollY);
        // Sync vertical scroll of BoneNameScrollView
        BoneNameScrollView.ScrollToAsync(BoneNameScrollView.ScrollX, _scrollY, false).ContinueWith((t) => _isScrolling = false);
        TimelineContentCanvas.InvalidateSurface(); // Redraw timeline content with new scroll
    }

    private void OnScrollViewSizeChanged(object? sender, EventArgs e)
    {
        UpdateCanvasSizes();
    }

    private void UpdateCanvasSizes()
    {
        var actualRowCount = Math.Max(1, _boneNames.Count);
        var totalContentHeight = HeaderHeight + actualRowCount * RowHeight;

        // Set the size of the content grids to define the scrollable area
        BoneNameContentGrid.HeightRequest = totalContentHeight;
        TimelineContentGrid.HeightRequest = totalContentHeight;
        TimelineContentGrid.WidthRequest = FrameCount * FrameWidth;

        var scrollableContentHeight = totalContentHeight;
        var visibleScrollableAreaHeight = TimelineContentScrollView.Height;
        _maxScrollY = Math.Max(0, scrollableContentHeight - (float)(visibleScrollableAreaHeight > 0 ? visibleScrollableAreaHeight : 1));

        BoneNameContentGrid.WidthRequest = LeftPanelWidth;

        // Set the canvas size to the visible area of the ScrollView to avoid creating a huge bitmap.
        // The canvas will be redrawn with the correct portion of the timeline based on scroll offsets.
        BoneNameCanvas.WidthRequest = LeftPanelWidth;
        if (BoneNameScrollView.Height > 1)
        {
            BoneNameCanvas.HeightRequest = BoneNameScrollView.Height;
        }
        if (TimelineContentScrollView.Width > 1 && TimelineContentScrollView.Height > 1)
        {
            TimelineContentCanvas.WidthRequest = TimelineContentScrollView.Width;
            TimelineContentCanvas.HeightRequest = TimelineContentScrollView.Height;
        }

        Debug.WriteLine($"[TimelineView] UpdateCanvasSizes: TimelineContentCanvas.WidthRequest={TimelineContentCanvas.WidthRequest}, TimelineContentCanvas.HeightRequest={TimelineContentCanvas.HeightRequest}");
    }


    void OnBoneNamePaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var info = e.Info;
        canvas.Clear(new SKColor(40, 40, 40)); // Background for the left panel

        using var linePaint = new SKPaint { Color = SKColors.Gray, StrokeWidth = 1 };
        using var altRowPaint = new SKPaint { Color = new SKColor(50, 50, 50) };
        using var textPaint = new SKPaint(_boneNameFont) { Color = SKColors.White, IsAntialias = true };

        // --- Draw Header Background (non-scrollable) ---
        using (var headerBgPaint = new SKPaint { Color = new SKColor(60, 60, 60) })
        {
            canvas.DrawRect(0, 0, info.Width, HeaderHeight, headerBgPaint);
        }
        canvas.DrawLine(0, HeaderHeight, info.Width, HeaderHeight, linePaint);

        canvas.Save();
        // Translate the canvas for the scrollable content
        canvas.Translate(0, -_scrollY);

        // --- Draw bone names (scrollable) ---
        if (_boneNames.Count > 0)
        {
            // Calculate visible rows based on current scroll and viewport
            var startRow = (int)((_scrollY) / RowHeight);
            var endRow = (int)((_scrollY + info.Height - HeaderHeight) / RowHeight) + 1;
            endRow = Math.Min(endRow, _boneNames.Count);

            for (int i = startRow; i < endRow; i++)
            {
                var y = HeaderHeight + i * RowHeight; // Y position relative to the translated canvas, offset by header
                if (i % 2 == 1)
                {
                    canvas.DrawRect(0, y, info.Width, RowHeight, altRowPaint);
                }
                // No need to draw even row background as it's the cleared color
                
                canvas.DrawLine(0, y + RowHeight, info.Width, y + RowHeight, linePaint); // Horizontal line for bone names
                // Center text vertically in the row
                canvas.DrawText(_boneNames[i], 10, y + (RowHeight + textPaint.TextSize) / 2, textPaint);
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
        using var keyPaint = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill, IsAntialias = true };
        using var headerTextPaint = new SKPaint(_headerFont) { Color = SKColors.White, TextAlign = SKTextAlign.Center, IsAntialias = true };

        var totalContentWidth = FrameCount * FrameWidth;
        var actualRowCount = Math.Max(1, _boneNames.Count);
        var totalContentHeightWithHeader = HeaderHeight + actualRowCount * RowHeight;

        // --- Draw Header (non-scrollable vertically, scrollable horizontally) ---
        canvas.Save();
        canvas.Translate(-_scrollX, 0); // Only apply horizontal scroll
        // Header background
        using (var headerBgPaint = new SKPaint { Color = new SKColor(60, 60, 60) })
        {
            canvas.DrawRect(0, 0, totalContentWidth, HeaderHeight, headerBgPaint);
        }
        // Header text (frame numbers) and vertical lines
        for (int i = 0; i < FrameCount; i++)
        {
            var x = i * FrameWidth;
            canvas.DrawLine(x, 0, x, HeaderHeight, linePaint); // Vertical lines in header
            if (i % 5 == 0) // Draw label every 5 frames
            {
                canvas.DrawText(i.ToString(), x + FrameWidth / 2, (HeaderHeight + headerTextPaint.TextSize) / 2, headerTextPaint);
            }
        }
        canvas.DrawLine(0, HeaderHeight, totalContentWidth, HeaderHeight, linePaint); // Bottom line of header
        canvas.Restore();

        // --- Draw Timeline Grid and Keyframes (scrollable) ---
        canvas.Save();
        canvas.Translate(-_scrollX, -_scrollY);

        // --- Draw alternating row backgrounds and horizontal lines
        var startRow = (int)((_scrollY) / RowHeight);
        var endRow = (int)((_scrollY + info.Height - HeaderHeight) / RowHeight) + 1;
        endRow = Math.Min(endRow, actualRowCount);

        for (int i = startRow; i < endRow; i++)
        {
            var y = HeaderHeight + i * RowHeight; // Y position relative to the translated canvas, offset by header
            if (i % 2 == 1)
            {
                canvas.DrawRect(0, y, totalContentWidth, RowHeight, altRowPaint);
            }
            canvas.DrawLine(0, y + RowHeight, totalContentWidth, y + RowHeight, linePaint);
        }

        // Draw vertical lines for the grid content
        for (int i = 0; i < FrameCount; i++)
        {
            var x = i * FrameWidth;
            canvas.DrawLine(x, HeaderHeight, x, totalContentHeightWithHeader, linePaint);
        }

        // Draw keyframes
        if (_boneNames.Count > 0)
        {
            for (int i = 0; i < _boneNames.Count; i++)
            {
                var boneRowY = HeaderHeight + (i * RowHeight);
                // Simple check if the row is visible
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
        // Keyframe should be centered on the horizontal line at the bottom of the row, considering the header
        var y_line = HeaderHeight + (row + 1) * RowHeight;
        var path = new SKPath();
        path.MoveTo(x, y_line - 10); // Top point of diamond
        path.LineTo(x + 10, y_line); // Right point
        path.LineTo(x, y_line + 10); // Bottom point
        path.LineTo(x - 10, y_line); // Left point
        path.Close();
        canvas.DrawPath(path, paint);
    }
}

