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
        Background = new SolidColorBrush(Color.FromRgb(30, 30, 46)); // Default dark bg #1E1E2E
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
        // Draw background grid dots
        DrawGridDots(dc);

        base.OnRender(dc);
    }

    private void DrawGridDots(DrawingContext dc)
    {
        // Compute area to draw based on viewport bounds
        double width = ActualWidth;
        double height = ActualHeight;

        if (width == 0 || height == 0) return;

        double gridSize = 20.0;
        Pen pen = new Pen(new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)), 1);
        
        // Draw standard subtle grid dots or lines
        // For simplicity, drawing faint points aligned to grid
        double startX = (OffsetX % gridSize) - OffsetX;
        double startY = (OffsetY % gridSize) - OffsetY;

        Brush dotBrush = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255));

        for (double x = -OffsetX; x < width - OffsetX; x += gridSize)
        {
            for (double y = -OffsetY; y < height - OffsetY; y += gridSize)
            {
                dc.DrawEllipse(dotBrush, null, new Point(x, y), 1.5, 1.5);
            }
        }
    }
}
