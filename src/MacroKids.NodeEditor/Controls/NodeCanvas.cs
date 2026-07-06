using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MacroKids.NodeEditor.Controls;

/// <summary>
/// Custom Canvas control that implements zooming, panning, and background grid dots rendering.
/// </summary>
public class NodeCanvas : Canvas
{
    private Point _panStart;
    private bool _isPanning;
    private TranslateTransform _translateTransform;
    private ScaleTransform _scaleTransform;
    private TransformGroup _transformGroup;

    public static readonly DependencyProperty ZoomProperty =
        DependencyProperty.Register(nameof(Zoom), typeof(double), typeof(NodeCanvas), 
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender, OnZoomChanged));

    public static readonly DependencyProperty OffsetXProperty =
        DependencyProperty.Register(nameof(OffsetX), typeof(double), typeof(NodeCanvas), 
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, OnOffsetChanged));

    public static readonly DependencyProperty OffsetYProperty =
        DependencyProperty.Register(nameof(OffsetY), typeof(double), typeof(NodeCanvas), 
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, OnOffsetChanged));

    public static readonly DependencyProperty IsGridVisibleProperty =
        DependencyProperty.Register(nameof(IsGridVisible), typeof(bool), typeof(NodeCanvas),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

    public double Zoom
    {
        get => (double)GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    public double OffsetX
    {
        get => (double)GetValue(OffsetXProperty);
        set => SetValue(OffsetXProperty, value);
    }

    public double OffsetY
    {
        get => (double)GetValue(OffsetYProperty);
        set => SetValue(OffsetYProperty, value);
    }

    public bool IsGridVisible
    {
        get => (bool)GetValue(IsGridVisibleProperty);
        set => SetValue(IsGridVisibleProperty, value);
    }

    public NodeCanvas()
    {
        _translateTransform = new TranslateTransform();
        _scaleTransform = new ScaleTransform();
        _transformGroup = new TransformGroup();
        _transformGroup.Children.Add(_scaleTransform);
        _transformGroup.Children.Add(_translateTransform);

        RenderTransform = _transformGroup;
        ClipToBounds = true;

        Focusable = true;
        SetResourceReference(BackgroundProperty, "BrushBackground");

        // Set a huge initial width and height so the canvas acts like an infinite space.
        // The scroll/pan and zoom will have plenty of virtual surface to work on.
        Width = 50000;
        Height = 50000;
    }

    private static void OnZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NodeCanvas canvas)
        {
            canvas._scaleTransform.ScaleX = (double)e.NewValue;
            canvas._scaleTransform.ScaleY = (double)e.NewValue;
        }
    }

    private static void OnOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NodeCanvas canvas)
        {
            if (e.Property == OffsetXProperty)
                canvas._translateTransform.X = (double)e.NewValue;
            else if (e.Property == OffsetYProperty)
                canvas._translateTransform.Y = (double)e.NewValue;
        }
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.ChangedButton == MouseButton.Middle || 
            (e.ChangedButton == MouseButton.Left && Keyboard.IsKeyDown(Key.Space)))
        {
            _isPanning = true;
            _panStart = e.GetPosition(this.Parent as UIElement ?? this);
            CaptureMouse();
            e.Handled = true;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (_isPanning && IsMouseCaptured)
        {
            Point current = e.GetPosition(this.Parent as UIElement ?? this);
            Vector delta = current - _panStart;

            OffsetX += delta.X;
            OffsetY += delta.Y;

            _panStart = current;
            e.Handled = true;
        }
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);

        if (_isPanning)
        {
            _isPanning = false;
            ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        double zoomFactor = e.Delta > 0 ? 1.1 : 0.9;
        Zoom = Math.Clamp(Zoom * zoomFactor, 0.1, 5.0);
        e.Handled = true;
    }

    protected override void OnRender(DrawingContext dc)
    {
        // Fill visible area first so there are no black/empty edges when zoomed out.
        // We need to "undo" the ScaleTransform to get local coordinates that map to the
        // full visible screen area, then fill with the background brush.
        double safeZoom = Math.Max(Zoom, 0.01);
        double visLeft   = -OffsetX / safeZoom;
        double visTop    = -OffsetY / safeZoom;
        double visWidth  = ActualWidth  / safeZoom;
        double visHeight = ActualHeight / safeZoom;

        dc.DrawRectangle(Background ?? Brushes.Transparent, null,
            new Rect(visLeft, visTop, visWidth, visHeight));

        if (IsGridVisible)
            DrawGridDots(dc, visLeft, visTop, visWidth, visHeight);

        // Do not call base.OnRender — we draw Background ourselves to cover the visible area.
    }

    private static void DrawGridDots(DrawingContext dc,
        double visLeft, double visTop, double visWidth, double visHeight)
    {
        const double gridSize = 25.0;
        var dotBrush = new SolidColorBrush(Color.FromArgb(45, 150, 165, 200));

        // Dot radius in local coords — stays ~1.5 screen px regardless of zoom
        // (ScaleTransform multiplies by Zoom, so local radius = 1.5/Zoom keeps screen size constant)
        // We draw in the local coord space so dots visually travel with the canvas.
        // Start at the first grid line that's visible (snapped to grid).
        double startX = Math.Floor(visLeft  / gridSize) * gridSize;
        double startY = Math.Floor(visTop   / gridSize) * gridSize;
        double endX   = visLeft  + visWidth  + gridSize;
        double endY   = visTop   + visHeight + gridSize;

        for (double x = startX; x < endX; x += gridSize)
        {
            for (double y = startY; y < endY; y += gridSize)
            {
                dc.DrawEllipse(dotBrush, null, new Point(x, y), 1.2, 1.2);
            }
        }
    }
}
