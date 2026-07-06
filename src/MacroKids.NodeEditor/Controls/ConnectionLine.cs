using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MacroKids.NodeEditor.Controls;

/// <summary>
/// A custom WPF Shape that draws a smooth cubic Bezier curve between a start and end point.
/// </summary>
public class ConnectionLine : Shape
{
    public static readonly DependencyProperty StartPointProperty =
        DependencyProperty.Register(nameof(StartPoint), typeof(Point), typeof(ConnectionLine),
            new FrameworkPropertyMetadata(new Point(0, 0), FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty EndPointProperty =
        DependencyProperty.Register(nameof(EndPoint), typeof(Point), typeof(ConnectionLine),
            new FrameworkPropertyMetadata(new Point(0, 0), FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

    public Point StartPoint
    {
        get => (Point)GetValue(StartPointProperty);
        set => SetValue(StartPointProperty, value);
    }

    public Point EndPoint
    {
        get => (Point)GetValue(EndPointProperty);
        set => SetValue(EndPointProperty, value);
    }

    protected override Geometry DefiningGeometry
    {
        get
        {
            StreamGeometry geometry = new StreamGeometry();
            using (StreamGeometryContext context = geometry.Open())
            {
                context.BeginFigure(StartPoint, isFilled: false, isClosed: false);

                // Compute control points for natural horizontal flow curve
                double distanceX = Math.Abs(EndPoint.X - StartPoint.X);
                double offset = Math.Max(distanceX * 0.5, 30.0);

                Point controlPoint1 = new Point(StartPoint.X + offset, StartPoint.Y);
                Point controlPoint2 = new Point(EndPoint.X - offset, EndPoint.Y);

                context.BezierTo(controlPoint1, controlPoint2, EndPoint, isStroked: true, isSmoothJoin: true);
            }

            return geometry;
        }
    }

    // Set custom visual pen and fill behavior so that hit-testing on the line works nicely,
    // and make sure we can trigger double click reliably.
    // In WPF Shape, we can override or implement custom hit test or just let it use standard pen/stroke.
    // Let's use standard stroke hit-test since standard Shape already handles this. We don't override GetPen.
}
