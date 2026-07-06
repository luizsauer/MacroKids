using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MacroKids.Core.Models;
using MacroKids.NodeEditor.ViewModels;

namespace MacroKids.NodeEditor.Controls;

public partial class NodeEditorControl : UserControl
{
    private Point _dragStart;
    private bool _isDraggingNode;
    private NodeViewModel? _draggedNode;

    public NodeEditorControl()
    {
        InitializeComponent();
    }

    private void Node_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && sender is Border border && border.DataContext is NodeViewModel nodeVm)
        {
            if (DataContext is NodeCanvasViewModel canvasVm)
            {
                canvasVm.SelectNode(nodeVm);
            }

            _isDraggingNode = true;
            _draggedNode = nodeVm;
            _dragStart = e.GetPosition(EditorCanvas);
            border.CaptureMouse();
            e.Handled = true;
        }
    }

    private void Node_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDraggingNode && _draggedNode != null && sender is Border border && border.IsMouseCaptured)
        {
            Point current = e.GetPosition(EditorCanvas);
            Vector delta = current - _dragStart;

            // Simple movement update, bypassing the undo history directly on active drag
            // In next stage, we can wrap this in a MoveNodeCommand at DragEnd for clean undo integration
            _draggedNode.X += delta.X;
            _draggedNode.Y += delta.Y;

            _dragStart = current;
            e.Handled = true;
        }
    }

    private NodeViewModel? _pendingSourceNode;
    private NodePin? _pendingSourcePin;

    private void Node_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDraggingNode && sender is Border border)
        {
            _isDraggingNode = false;
            _draggedNode = null;
            border.ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    private void Pin_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is NodePin pin && pin.Direction == PinDirection.Output)
        {
            // Find parent node view model by checking DataContext of parent elements
            var nodeVm = FindParentDataContext<NodeViewModel>(element);
            if (nodeVm != null)
            {
                _pendingSourceNode = nodeVm;
                _pendingSourcePin = pin;
                e.Handled = true;
            }
        }
    }

    private void Pin_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_pendingSourceNode != null && _pendingSourcePin != null &&
            sender is FrameworkElement element && element.Tag is NodePin targetPin && targetPin.Direction == PinDirection.Input)
        {
            var targetNodeVm = FindParentDataContext<NodeViewModel>(element);
            if (targetNodeVm != null && targetNodeVm.InstanceId != _pendingSourceNode.InstanceId)
            {
                if (DataContext is NodeCanvasViewModel canvasVm)
                {
                    canvasVm.ConnectPins(
                        _pendingSourceNode.InstanceId, _pendingSourcePin.Id,
                        targetNodeVm.InstanceId, targetPin.Id);
                }
            }
        }

        // Clean up pending states
        _pendingSourceNode = null;
        _pendingSourcePin = null;
    }

    private static T? FindParentDataContext<T>(DependencyObject child) where T : class
    {
        DependencyObject current = child;
        while (current != null)
        {
            if (current is FrameworkElement fe && fe.DataContext is T targetVm)
                return targetVm;

            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}
