using Microsoft.Maui.Controls;
using MiniMikuDance.Import;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;


namespace MiniMikuDanceMaui;

public partial class TimelineView : ContentView
{
    const int FrameCount = 200;
    const float FrameWidth = 40f; // Adjusted width
        public const float HeaderHeight = 30f;
    const float RowHeight = 30f; // Adjusted height
    const float LeftPanelWidth = 90f; // Adjusted width for bone name panel to match XAML
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
            BoneKeyInputKeyCanvas.InvalidateSurface();
        }
    }
    private ModelData? _model;
    public List<string> BoneNames => _boneNames;
    private List<string> _boneNames = new List<string>();
    private Dictionary<string, List<int>> _keyframes = new Dictionary<string, List<int>>();

    // UI State
    private SKFont _keyframeFont;
    private SKFont _boneNameFont;
        private SKFont _headerFont;
    public int CurrentFrame
    {
        get => _currentFrame;
        set
        {
            if (_currentFrame != value)
            {
                _currentFrame = value;
                TimelineContentCanvas.InvalidateSurface();
                BoneNameCanvas.InvalidateSurface();
                BoneKeyInputKeyCanvas.InvalidateSurface();
            }
        }
    }
    private int _currentFrame = 0;
    private bool _isScrolling = false;
    private float _scrollY = 0;
    private float _scrollX = 0;
    private int _selectedKeyInputBoneIndex = 0;

    public event EventHandler? AddKeyClicked;
    public event EventHandler? EditKeyClicked;
    public event EventHandler? DeleteKeyClicked;

    public int SelectedKeyInputBoneIndex
    {
        get => _selectedKeyInputBoneIndex;
        set
        {
            if (_selectedKeyInputBoneIndex != value)
            {
                _selectedKeyInputBoneIndex = value;
                BoneKeyInputKeyCanvas.InvalidateSurface();
            }
        }
    }

    public TimelineView()
    {
        InitializeComponent();
        _keyframeFont = new SKFont(SKTypeface.Default, 12);
        _boneNameFont = new SKFont(SKTypeface.Default, BoneNameFontSize);
        _headerFont = new SKFont(SKTypeface.Default, HeaderFontSize);
        this.Loaded += OnTimelineViewLoaded;
    }

    public List<int> GetKeyframesForBone(string boneName)
    {
        if (_keyframes.TryGetValue(boneName, out var frames))
        {
            return frames;
        }
        return new List<int>();
    }

    public void AddKeyframe(string boneName, int frame)
    {
        if (!_keyframes.ContainsKey(boneName))
        {
            _keyframes[boneName] = new List<int>();
        }
        if (!_keyframes[boneName].Contains(frame))
        {
            _keyframes[boneName].Add(frame);
            _keyframes[boneName].Sort(); // Keep keyframes sorted
            TimelineContentCanvas.InvalidateSurface();
        }
    }

    public void RemoveKeyframe(string boneName, int frame)
    {
        if (_keyframes.ContainsKey(boneName) && _keyframes[boneName].Contains(frame))
        {
            _keyframes[boneName].Remove(frame);
            TimelineContentCanvas.InvalidateSurface();
        }
    }

    public bool HasKeyframe(string boneName, int frame)
    {
        return _keyframes.ContainsKey(boneName) && _keyframes[boneName].Contains(frame);
    }

    public void ClearKeyframes()
    {
        _keyframes.Clear();
        TimelineContentCanvas.InvalidateSurface();
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
        CurrentFrame = 0;
        CurrentFrameEntry.Text = CurrentFrame.ToString();
        TimelineContentCanvas.InvalidateSurface();
        BoneNameCanvas.InvalidateSurface();
        BoneKeyInputKeyCanvas.InvalidateSurface();
    }

    private void CurrentFrameEntry_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (int.TryParse(e.NewTextValue, out int frame))
        {
            CurrentFrame = frame;
            TimelineContentCanvas.InvalidateSurface();
            BoneNameCanvas.InvalidateSurface();
            BoneKeyInputKeyCanvas.InvalidateSurface();
        }
    }

    private void OnFrameMinusOneClicked(object? sender, EventArgs e)
    {
        CurrentFrame = Math.Max(0, CurrentFrame - 1);
        CurrentFrameEntry.Text = CurrentFrame.ToString();
    }

    private void OnFramePlusOneClicked(object? sender, EventArgs e)
    {
        CurrentFrame = Math.Min(FrameCount - 1, CurrentFrame + 1);
        CurrentFrameEntry.Text = CurrentFrame.ToString();
    }

    private void OnFrameToStartClicked(object? sender, EventArgs e)
    {
        CurrentFrame = 0;
        CurrentFrameEntry.Text = CurrentFrame.ToString();
    }

    private void OnFrameToEndClicked(object? sender, EventArgs e)
    {
        CurrentFrame = FrameCount - 1;
        CurrentFrameEntry.Text = CurrentFrame.ToString();
    }

    void OnAddKeyClicked(object? sender, EventArgs e) => AddKeyClicked?.Invoke(this, EventArgs.Empty);
    void OnEditKeyClicked(object? sender, EventArgs e) => EditKeyClicked?.Invoke(this, EventArgs.Empty);
    void OnDeleteKeyClicked(object? sender, EventArgs e) => DeleteKeyClicked?.Invoke(this, EventArgs.Empty);

    private void OnTimelineViewLoaded(object? sender, EventArgs e)
    {
        BoneNameScrollView.Scrolled += OnBoneNameScrollViewScrolled;
        TimelineContentScrollView.Scrolled += OnTimelineContentScrollViewScrolled;
        FrameHeaderScroll.Scrolled += OnFrameHeaderScrollViewScrolled;
        BoneNameScrollView.SizeChanged += OnScrollViewSizeChanged;
        TimelineContentScrollView.SizeChanged += OnScrollViewSizeChanged;
        FrameHeaderScroll.SizeChanged += OnScrollViewSizeChanged;
        
        HeaderCanvas.EnableTouchEvents = true;
        HeaderCanvas.Touch += OnHeaderTouch;

        // Ensure the drawing area is configured when the view first appears
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
        FrameHeaderScroll.ScrollToAsync(_scrollX, 0, false);
        TimelineContentCanvas.InvalidateSurface();
        BoneKeyInputKeyCanvas.InvalidateSurface(); // Invalidate key input canvas on scroll
    }

    private void OnFrameHeaderScrollViewScrolled(object? sender, ScrolledEventArgs e)
    {
        if (_isScrolling) return;
        _isScrolling = true;
        _scrollX = (float)e.ScrollX;
        TimelineContentScrollView.ScrollToAsync(_scrollX, TimelineContentScrollView.ScrollY, false)
            .ContinueWith((t) => _isScrolling = false);
    }

    private void OnScrollViewSizeChanged(object? sender, EventArgs e)
    {
        UpdateCanvasSizes();
    }

    private void UpdateCanvasSizes()
    {


        var actualRowCount = Math.Max(1, _boneNames.Count);
        var totalContentWidth = FrameCount * FrameWidth;

        
        var fullScrollableContentHeight = HeaderHeight + actualRowCount * RowHeight;

        // Ensure ScrollView.Height is available before calculating max scroll
        if (TimelineContentScrollView.Height <= 0 || BoneNameScrollView.Height <= 0)
        {
            // If ScrollView hasn't been measured yet, defer calculation

            return;
        }

        // 2. Calculate the full maximum scroll offset
        var fullMaxScrollOffset = Math.Max(0, fullScrollableContentHeight - TimelineContentScrollView.Height);

        // 3. Calculate the new (half) maximum scroll offset
        var newMaxScrollOffset = fullMaxScrollOffset / 2.0f;

        // 4. Set _maxScrollY to the new (half) maximum scroll offset
        _maxScrollY = (float)fullMaxScrollOffset;

        // 5. Set the HeightRequest of the Grids to reflect the new scrollable range
        BoneNameContentGrid.HeightRequest = Math.Max(BoneNameScrollView.Height, fullScrollableContentHeight);
        TimelineContentGrid.HeightRequest = Math.Max(TimelineContentScrollView.Height, fullScrollableContentHeight);
        TimelineContentGrid.WidthRequest = totalContentWidth; // Keep full width for horizontal scroll

        // Set the canvas size to the full scrollable content height
        BoneNameCanvas.HeightRequest = fullScrollableContentHeight;
        BoneNameCanvas.WidthRequest = LeftPanelWidth; // Keep fixed width for bone name canvas
        TimelineContentCanvas.WidthRequest = totalContentWidth;
        TimelineContentCanvas.HeightRequest = fullScrollableContentHeight;

        // Set sizes for the new bone key input canvases
        BoneKeyInputKeyCanvas.WidthRequest = totalContentWidth;




        BoneNameCanvas.InvalidateSurface();
        TimelineContentCanvas.InvalidateSurface();
        BoneKeyInputKeyCanvas.InvalidateSurface();
    }

    void OnBoneNamePaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var info = e.Info;
        canvas.Clear(new SKColor(40, 40, 40)); // Background for the left panel



        using var linePaint = new SKPaint { Color = SKColors.Gray, StrokeWidth = 1 };
        using var altRowPaint = new SKPaint { Color = new SKColor(50, 50, 50) };
        using var selectedRowPaint = new SKPaint { Color = new SKColor(80, 80, 80) };
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
                // Highlight selected bone row
                if (i == _selectedKeyInputBoneIndex)
                {
                    canvas.DrawRect(0, y, info.Width, RowHeight, selectedRowPaint);
                }
                else if (i % 2 == 1)
                {
                    canvas.DrawRect(0, y, info.Width, RowHeight, altRowPaint);
                }

                canvas.DrawLine(0, y + RowHeight, info.Width, y + RowHeight, linePaint);
                float textY = y + (RowHeight - _boneNameFont.Size) / 2 + _boneNameFont.Size;
                canvas.DrawText(_boneNames[i], 10, textY, SKTextAlign.Left, _boneNameFont, textPaint);
            }
        }
        canvas.Restore();
    }

        private void OnHeaderPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            var info = e.Info;
            canvas.Clear(SKColors.Transparent);
            using var minorPaint = new SKPaint { Color = SKColors.LightGray, StrokeWidth = 1 };
            using var majorPaint = new SKPaint { Color = SKColors.Gray, StrokeWidth = 2 };
            using var numberPaint = new SKPaint { Color = SKColors.Gray, IsAntialias = true };
            using var markerPaint = new SKPaint { Color = SKColors.Red, StrokeWidth = 2 };
            for (int f = 0; f < FrameCount; f++)
            {
                float x = f * FrameWidth - _scrollX;
                canvas.DrawLine(x, 0, x, info.Height, f % 10 == 0 ? majorPaint : minorPaint);
                if (f % 10 == 0)
                {
                    var text = f.ToString();
                    canvas.DrawText(text, x + 2, info.Height - 2, SKTextAlign.Left, _headerFont, numberPaint);
                }
            }
            float markerX = CurrentFrame * FrameWidth - _scrollX + FrameWidth / 2;
            canvas.DrawLine(markerX, 0, markerX, info.Height, markerPaint);
        }

    private void OnHeaderTouch(object? sender, SKTouchEventArgs e)
    {
        if (e.ActionType == SKTouchAction.Released)
        {
            int frame = (int)((e.Location.X + _scrollX) / FrameWidth);
            frame = Math.Clamp(frame, 0, FrameCount - 1);
            CurrentFrame = frame;
            CurrentFrameEntry.Text = CurrentFrame.ToString();
        }
        e.Handled = true;
    }

    void OnTimelineContentPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var info = e.Info;
        // Main timeline content (bone tracks)
        canvas.Clear(SKColors.Transparent);

        using var altRowPaint = new SKPaint { Color = new SKColor(50, 50, 50) };
        using var linePaint = new SKPaint { Color = SKColors.Gray, StrokeWidth = 1 };
        using var minorPaint = new SKPaint { Color = SKColors.LightGray, StrokeWidth = 1 };
        using var majorPaint = new SKPaint { Color = SKColors.Gray, StrokeWidth = 2 };
        using var keyframePaint = new SKPaint { Color = SKColors.Yellow, Style = SKPaintStyle.Fill };
        using var playheadPaint = new SKPaint { Color = SKColors.Red, StrokeWidth = 2 };

        canvas.Save();
        // Align with header and vertical scroll
        canvas.Translate(-_scrollX, -_scrollY);

        // Draw alternating row backgrounds and horizontal lines
        var totalWidth = FrameCount * FrameWidth;
        for (int i = 0; i < _boneNames.Count; i++)
        {
            var y = HeaderHeight + i * RowHeight;
            if (i % 2 == 1)
                canvas.DrawRect(0, y, totalWidth, RowHeight, altRowPaint);
            canvas.DrawLine(0, y + RowHeight, totalWidth, y + RowHeight, linePaint);
        }

        // Draw vertical grid lines
        for (int f = 0; f < FrameCount; f++)
        {
            var x = f * FrameWidth;
            canvas.DrawLine(x, HeaderHeight, x, HeaderHeight + _boneNames.Count * RowHeight,
                f % 10 == 0 ? majorPaint : minorPaint);
        }

        // Draw keyframes for all bones
        foreach (var bone in _boneNames)
        {
            if (!_keyframes.TryGetValue(bone, out var frames))
                continue;
            var row = _boneNames.IndexOf(bone);
            foreach (var frame in frames)
            {
                var x = frame * FrameWidth + FrameWidth / 2 - _scrollX;
                var y = HeaderHeight + row * RowHeight + RowHeight / 2 - _scrollY;
                canvas.DrawCircle(x, y, 5, keyframePaint);
            }
        }

        // Draw current frame playhead
        var playX = CurrentFrame * FrameWidth + FrameWidth / 2 - _scrollX;
        canvas.DrawLine(playX, HeaderHeight, playX, HeaderHeight + _boneNames.Count * RowHeight, playheadPaint);
        canvas.Restore();
    }


    void OnBoneKeyInputKeyPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var info = e.Info;
        // Background for key input area
        canvas.Clear(new SKColor(40, 40, 40));
        // Top and bottom borders
        using var borderPaint = new SKPaint { Color = SKColors.Gray, StrokeWidth = 1 };
        canvas.DrawLine(0, 0, info.Width, 0, borderPaint);
        canvas.DrawLine(0, info.Height - 1, info.Width, info.Height - 1, borderPaint);

        using var linePaint = new SKPaint { Color = SKColors.Gray, StrokeWidth = 1 };
        using var keyframePaint = new SKPaint { Color = SKColors.Yellow, Style = SKPaintStyle.Fill };
        using var textPaint = new SKPaint { Color = SKColors.White, IsAntialias = true };

        // Draw vertical lines for frames
        for (int i = 0; i < FrameCount; i++)
        {
            var x = i * FrameWidth - _scrollX;
            canvas.DrawLine(x, 0, x, info.Height, linePaint);
        }

        // Draw keyframes for the selected bone
        if (_boneNames.Count > _selectedKeyInputBoneIndex)
        {
            var boneName = _boneNames[_selectedKeyInputBoneIndex];
            if (_keyframes.TryGetValue(boneName, out var frames))
            {
                foreach (var frame in frames)
                {
                    var x = frame * FrameWidth - _scrollX + FrameWidth / 2;
                    var y = info.Height / 2; // Center of the canvas
                    canvas.DrawCircle(x, y, 5, keyframePaint);
                    canvas.DrawText(frame.ToString(), x + 7, y + 5, SKTextAlign.Left, _keyframeFont, textPaint);
                }
            }
        }

        // Draw current frame playhead
        using var playheadPaint = new SKPaint { Color = SKColors.Red, StrokeWidth = 2 };
        var playheadX = CurrentFrame * FrameWidth - _scrollX + FrameWidth / 2;
        canvas.DrawLine(playheadX, 0, playheadX, info.Height, playheadPaint);
    }

}
