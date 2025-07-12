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
    const float BoneNameFontSize = 16f; // Font size for bone names
    const float HeaderFontSize = 14f; // Font size for header

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
                var requiredHumanoidBones = new List<string>
                {
                    "hips", "spine", "chest", "neck", "head",
                    "leftUpperArm", "leftLowerArm", "leftHand",
                    "rightUpperArm", "rightLowerArm", "rightHand",
                    "leftUpperLeg", "leftLowerLeg", "leftFoot",
                    "rightUpperLeg", "rightLowerLeg", "rightFoot"
                };

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

    private SKFont _boneNameFont = new SKFont(SKTypeface.Default, BoneNameFontSize);
    private SKFont _headerFont = new SKFont(SKTypeface.Default, HeaderFontSize);

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
        return OpenTK.Mathematics.Vector3.Zero;
    }

    public OpenTK.Mathematics.Vector3 GetBoneRotationAtFrame(string boneName, int frame)
    {
        return OpenTK.Mathematics.Vector3.Zero;
    }

    private void OnPlayClicked(object? sender, EventArgs e) { }
    private void OnPauseClicked(object? sender, EventArgs e) { }

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
    }

    private void OnFramePlusOneClicked(object? sender, EventArgs e)
    {
        _currentFrame = Math.Min(FrameCount - 1, _currentFrame + 1);
        CurrentFrameEntry.Text = _currentFrame.ToString();
    }

    private void OnFrameToStartClicked(object? sender, EventArgs e)
    {
        _currentFrame = 0;
        CurrentFrameEntry.Text = _currentFrame.ToString();
    }

    private void OnFrameToEndClicked(object? sender, EventArgs e)
    {
        _currentFrame = FrameCount - 1;
        CurrentFrameEntry.Text = _currentFrame.ToString();
    }

    void OnAddKeyClicked(object? sender, EventArgs e) => AddKeyClicked?.Invoke(this, EventArgs.Empty);
    void OnEditKeyClicked(object? sender, EventArgs e) => EditKeyClicked?.Invoke(this, EventArgs.Empty);
    void OnDeleteKeyClicked(object? sender, EventArgs e) => DeleteKeyClicked?.Invoke(this, EventArgs.Empty);

    private void OnTimelineViewLoaded(object? sender, EventArgs e)
    {
        BoneNameScrollView.Scrolled += OnBoneNameScrollViewScrolled;
        TimelineContentScrollView.Scrolled += OnTimelineContentScrollViewScrolled;
        BoneNameScrollView.SizeChanged += OnScrollViewSizeChanged;
        TimelineContentScrollView.SizeChanged += OnScrollViewSizeChanged;
        // 初期レイアウト完了後にサイズ計算を行う
        Dispatcher.Dispatch(UpdateCanvasSizes);
    }

    private void OnBoneNameScrollViewScrolled(object? sender, ScrolledEventArgs e)
    {
        if (_isScrolling) return;
        _isScrolling = true;
        _scrollY = (float)Math.Min(e.ScrollY, _maxScrollY);
        TimelineContentScrollView.ScrollToAsync(TimelineContentScrollView.ScrollX, _scrollY, false).ContinueWith((t) => _isScrolling = false);
        BoneNameCanvas.InvalidateSurface();
    }

    private void OnTimelineContentScrollViewScrolled(object? sender, ScrolledEventArgs e)
    {
        if (_isScrolling) return;
        _isScrolling = true;
        _scrollX = (float)e.ScrollX;
        _scrollY = (float)Math.Min(e.ScrollY, _maxScrollY);
        BoneNameScrollView.ScrollToAsync(BoneNameScrollView.ScrollX, _scrollY, false).ContinueWith((t) => _isScrolling = false);
        TimelineContentCanvas.InvalidateSurface();
    }

    private void OnScrollViewSizeChanged(object? sender, EventArgs e)
    {
        UpdateCanvasSizes();
    }

    private void UpdateCanvasSizes()
    {
        var actualRowCount = Math.Max(1, _boneNames.Count);
        var totalContentWidth = FrameCount * FrameWidth;

        var rightFootIndex = _boneNames.IndexOf(RightFootBoneName);
        var scrollableContentHeight = HeaderHeight + actualRowCount * RowHeight;
        if (rightFootIndex >= 0)
        {
            scrollableContentHeight = HeaderHeight + (rightFootIndex + 1) * RowHeight;
        }

        // Set the size of the Grid to define the scrollable area for the ScrollView
        BoneNameContentGrid.HeightRequest = scrollableContentHeight;
        BoneNameContentGrid.WidthRequest = LeftPanelWidth;
        TimelineContentGrid.HeightRequest = scrollableContentHeight;
        TimelineContentGrid.WidthRequest = totalContentWidth;

        // Set ScrollView's HeightRequest to match the content height
        BoneNameScrollView.HeightRequest = scrollableContentHeight;
        TimelineContentScrollView.HeightRequest = scrollableContentHeight;

        // Set the canvas size to the visible area of the ScrollView to avoid creating a huge bitmap
        BoneNameCanvas.HeightRequest = Math.Max(1, BoneNameScrollView.Height);
        TimelineContentCanvas.WidthRequest = Math.Max(1, TimelineContentScrollView.Width);
        TimelineContentCanvas.HeightRequest = Math.Max(1, TimelineContentScrollView.Height);

        _maxScrollY = (float)Math.Max(0, scrollableContentHeight - Math.Max(1, TimelineContentScrollView.Height));

        Debug.WriteLine($"[TimelineView] UpdateCanvasSizes: GridHeight={scrollableContentHeight}, CanvasWidth={TimelineContentCanvas.WidthRequest}, CanvasHeight={TimelineContentCanvas.HeightRequest}, MaxScrollY={_maxScrollY}");

        BoneNameCanvas.InvalidateSurface();
        TimelineContentCanvas.InvalidateSurface();
    }

    void OnBoneNamePaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var info = e.Info;
        canvas.Clear(new SKColor(40, 40, 40)); // Background for the left panel

        using var linePaint = new SKPaint { Color = SKColors.Gray, StrokeWidth = 1 };
        using var altRowPaint = new SKPaint { Color = new SKColor(50, 50, 50) };
        using var textPaint = new SKPaint { Color = SKColors.White, IsAntialias = true };

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
        if (_boneNames.Any())
        {
            // Calculate visible rows based on current scroll and viewport
            var startRow = (int)(_scrollY / RowHeight);
            var endRow = Math.Min(_boneNames.Count, startRow + (int)(info.Height / RowHeight) + 2);

            for (int i = startRow; i < endRow; i++)
            {
                var y = HeaderHeight + i * RowHeight;
                if (i % 2 == 1)
                {
                    canvas.DrawRect(0, y, info.Width, RowHeight, altRowPaint);
                }
                
                canvas.DrawLine(0, y + RowHeight, info.Width, y + RowHeight, linePaint); // Horizontal line for bone names
                float textY = y + (RowHeight - _boneNameFont.Size) / 2 + _boneNameFont.Size;
                canvas.DrawText(_boneNames[i], 10, textY, _boneNameFont, textPaint);
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
        using var headerTextPaint = new SKPaint { Color = SKColors.White, IsAntialias = true };

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
        var startFrameHeader = (int)(_scrollX / FrameWidth);
        var endFrameHeader = Math.Min(FrameCount, startFrameHeader + (int)(info.Width / FrameWidth) + 2);
        for (int i = startFrameHeader; i < endFrameHeader; i++)
        {
            var x = i * FrameWidth;
            canvas.DrawLine(x, 0, x, HeaderHeight, linePaint); // Vertical lines in header
            if (i % 5 == 0) // Draw label every 5 frames
            {
                var text = i.ToString();
                var textWidth = _headerFont.MeasureText(text);
                float textX = x + (FrameWidth - textWidth) / 2;
                float textY = (HeaderHeight + _headerFont.Size) / 2;
                canvas.DrawText(text, textX, textY, _headerFont, headerTextPaint);
            }
        }
        canvas.DrawLine(0, HeaderHeight, totalContentWidth, HeaderHeight, linePaint); // Bottom line of header
        canvas.Restore();

        // --- Draw Timeline Grid and Keyframes (scrollable) ---
        canvas.Save();
        canvas.Translate(-_scrollX, -_scrollY);

        // --- Draw alternating row backgrounds and horizontal lines
        var startRow = (int)(_scrollY / RowHeight);
        var endRow = Math.Min(actualRowCount, startRow + (int)(info.Height / RowHeight) + 2);

        for (int i = startRow; i < endRow; i++)
        {
            var y = HeaderHeight + i * RowHeight;
            if (i % 2 == 1)
            {
                canvas.DrawRect(0, y, totalContentWidth, RowHeight, altRowPaint);
            }
            canvas.DrawLine(0, y + RowHeight, totalContentWidth, y + RowHeight, linePaint);
        }

        // Draw vertical lines for the grid content
        var startFrameContent = (int)(_scrollX / FrameWidth);
        var endFrameContent = Math.Min(FrameCount, startFrameContent + (int)(info.Width / FrameWidth) + 2);
        for (int i = startFrameContent; i < endFrameContent; i++)
        {
            var x = i * FrameWidth;
            canvas.DrawLine(x, HeaderHeight, x, totalContentHeightWithHeader, linePaint);
        }

        // Draw keyframes
        if (_boneNames.Any())
        {
            for (int i = startRow; i < endRow; i++)
            {
                var boneName = _boneNames[i];
                if (_keyframes.TryGetValue(boneName, out var frames))
                {
                    foreach (var frame in frames)
                    {
                        var keyframeX = frame * FrameWidth;
                        if (keyframeX >= _scrollX && keyframeX <= _scrollX + info.Width)
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
        return _keyframes.GetValueOrDefault(boneName, new List<int>());
    }

    void DrawKeyframe(SKCanvas canvas, int row, int frame, SKPaint paint)
    {
        var x = frame * FrameWidth + FrameWidth / 2;
        var y_line = HeaderHeight + (row + 1) * RowHeight;
        var path = new SKPath();
        path.MoveTo(x, y_line - 10);
        path.LineTo(x + 10, y_line);
        path.LineTo(x, y_line + 10);
        path.LineTo(x - 10, y_line);
        path.Close();
        canvas.DrawPath(path, paint);
    }
}

