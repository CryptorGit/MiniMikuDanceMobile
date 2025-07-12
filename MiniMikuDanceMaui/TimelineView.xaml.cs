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
        UpdateCanvasSizes();
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

        // Set the canvas size to the total scrollable content size.
        // The ScrollView will manage the window into this large canvas.
        BoneNameCanvas.HeightRequest = scrollableContentHeight;
        BoneNameCanvas.WidthRequest = LeftPanelWidth;
        TimelineContentCanvas.HeightRequest = scrollableContentHeight;
        TimelineContentCanvas.WidthRequest = totalContentWidth;

        _maxScrollY = (float)Math.Max(0, scrollableContentHeight - TimelineContentScrollView.Height);

        Debug.WriteLine($"[TimelineView] UpdateCanvasSizes: CanvasHeight={scrollableContentHeight}, MaxScrollY={_maxScrollY}");

        BoneNameCanvas.InvalidateSurface();
        TimelineContentCanvas.InvalidateSurface();
    }

    void OnBoneNamePaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var info = e.Info;
        // The canvas is now the full size of the content, so we get the visible part from the scroll view.
        var visibleRect = new SKRect(_scrollX, _scrollY, _scrollX + (float)BoneNameScrollView.Width, _scrollY + (float)BoneNameScrollView.Height);
        canvas.ClipRect(visibleRect);
        canvas.Clear(new SKColor(40, 40, 40));

        using var linePaint = new SKPaint { Color = SKColors.Gray, StrokeWidth = 1 };
        using var altRowPaint = new SKPaint { Color = new SKColor(50, 50, 50) };
        using var textPaint = new SKPaint { Color = SKColors.White, IsAntialias = true };

        // Draw the non-scrolling header relative to the current scroll position.
        using (var headerBgPaint = new SKPaint { Color = new SKColor(60, 60, 60) })
        {
            canvas.DrawRect(0, _scrollY, (float)BoneNameScrollView.Width, HeaderHeight, headerBgPaint);
        }
        canvas.DrawLine(0, _scrollY + HeaderHeight, (float)BoneNameScrollView.Width, _scrollY + HeaderHeight, linePaint);

        // Draw bone names (they are now in the full canvas space)
        if (_boneNames.Any())
        {
            for (int i = 0; i < _boneNames.Count; i++)
            {
                var y = HeaderHeight + i * RowHeight;
                if (y > _scrollY + BoneNameScrollView.Height || y + RowHeight < _scrollY) continue; // Cull non-visible rows

                if (i % 2 == 1)
                {
                    canvas.DrawRect(0, y, (float)BoneNameScrollView.Width, RowHeight, altRowPaint);
                }
                canvas.DrawLine(0, y + RowHeight, (float)BoneNameScrollView.Width, y + RowHeight, linePaint);
                float textY = y + (RowHeight - _boneNameFont.Size) / 2 + _boneNameFont.Size;
                canvas.DrawText(_boneNames[i], 10, textY, _boneNameFont, textPaint);
            }
        }
    }

    void OnTimelineContentPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var info = e.Info;
        var visibleRect = new SKRect(_scrollX, _scrollY, _scrollX + (float)TimelineContentScrollView.Width, _scrollY + (float)TimelineContentScrollView.Height);
        canvas.ClipRect(visibleRect);
        canvas.Clear(SKColors.Transparent);

        using var linePaint = new SKPaint { Color = SKColors.Gray, StrokeWidth = 1 };
        using var altRowPaint = new SKPaint { Color = new SKColor(50, 50, 50) };
        using var keyPaint = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill, IsAntialias = true };
        using var headerTextPaint = new SKPaint { Color = SKColors.White, IsAntialias = true };

        var totalContentWidth = FrameCount * FrameWidth;

        // Draw the non-scrolling header relative to the current scroll position.
        using (var headerBgPaint = new SKPaint { Color = new SKColor(60, 60, 60) })
        {
            canvas.DrawRect(_scrollX, _scrollY, (float)TimelineContentScrollView.Width, HeaderHeight, headerBgPaint);
        }

        for (int i = 0; i < FrameCount; i++)
        {
            var x = i * FrameWidth;
            if (x < _scrollX - FrameWidth || x > _scrollX + TimelineContentScrollView.Width) continue;
            canvas.DrawLine(x, _scrollY, x, _scrollY + HeaderHeight, linePaint);
            if (i % 5 == 0)
            {
                var text = i.ToString();
                var textWidth = _headerFont.MeasureText(text);
                float textX = x + (FrameWidth - textWidth) / 2;
                float textY = _scrollY + (HeaderHeight + _headerFont.Size) / 2;
                canvas.DrawText(text, textX, textY, _headerFont, headerTextPaint);
            }
        }
        canvas.DrawLine(_scrollX, _scrollY + HeaderHeight, _scrollX + (float)TimelineContentScrollView.Width, _scrollY + HeaderHeight, linePaint);

        // Draw the main grid content (rows and columns)
        for (int i = 0; i < _boneNames.Count; i++)
        {
            var y = HeaderHeight + i * RowHeight;
            if (y > _scrollY + TimelineContentScrollView.Height || y + RowHeight < _scrollY) continue;

            if (i % 2 == 1)
            {
                canvas.DrawRect(_scrollX, y, (float)TimelineContentScrollView.Width, RowHeight, altRowPaint);
            }
            canvas.DrawLine(_scrollX, y + RowHeight, _scrollX + (float)TimelineContentScrollView.Width, y + RowHeight, linePaint);
        }

        for (int i = 0; i < FrameCount; i++)
        {
            var x = i * FrameWidth;
            if (x < _scrollX - FrameWidth || x > _scrollX + TimelineContentScrollView.Width) continue;
            canvas.DrawLine(x, HeaderHeight, x, info.Height, linePaint);
        }

        // Draw keyframes
        if (_boneNames.Any())
        {
            for (int i = 0; i < _boneNames.Count; i++)
            {
                var y = HeaderHeight + i * RowHeight;
                if (y > _scrollY + TimelineContentScrollView.Height || y + RowHeight < _scrollY) continue;

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

