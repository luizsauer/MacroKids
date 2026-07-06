using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
}
