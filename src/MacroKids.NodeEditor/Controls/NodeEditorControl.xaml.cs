using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using MacroKids.Core.Models;
using MacroKids.NodeEditor.ViewModels;

namespace MacroKids.NodeEditor.Controls;

public partial class NodeEditorControl : UserControl
{
    private Point _paletteDragStart;
    private Point _dragStart;
    private bool _isDraggingNode;
    private NodeViewModel? _draggedNode;

    public NodeEditorControl()
    {
        InitializeComponent();
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if (e.Key == Key.Delete && DataContext is NodeCanvasViewModel canvasVm)
        {
            canvasVm.DeleteSelectedCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void Node_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || sender is not Border border || border.DataContext is not NodeViewModel nodeVm)
            return;

        if (ShouldIgnoreDrag(e.OriginalSource as DependencyObject))
            return;

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

    private void Connection_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2 || sender is not FrameworkElement element || element.DataContext is not ConnectionViewModel connectionVm)
            return;

        if (DataContext is NodeCanvasViewModel canvasVm)
        {
            canvasVm.DisconnectConnection(connectionVm.ConnectionId);
            e.Handled = true;
        }
    }

    private void EditorCanvas_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (DataContext is not NodeCanvasViewModel canvasVm || !canvasVm.IsConnectingPins)
            return;

        canvasVm.UpdateConnectionPreview(e.GetPosition(EditorCanvas));
    }

    private void EditorCanvas_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not NodeCanvasViewModel canvasVm || !canvasVm.IsConnectingPins)
            return;

        // If releasing over a pin, let Pin_PreviewMouseLeftButtonUp handle the connection.
        // PreviewMouseLeftButtonUp tunnels top-down, so this Grid handler fires BEFORE
        // the pin's handler. We must not cancel here if the target is a pin.
        if (IsOverPinElement(e.OriginalSource as DependencyObject))
            return;

        canvasVm.CancelConnectionPreview();
    }

    private static bool IsOverPinElement(DependencyObject? source)
    {
        while (source != null)
        {
            if (source is FrameworkElement fe && fe.DataContext is NodePinViewModel)
                return true;
            source = VisualTreeHelper.GetParent(source);
        }
        return false;
    }

    private void EditorCanvas_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(NodeMetadata)) || e.Data.GetDataPresent(DataFormats.StringFormat))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }
    }

    private void EditorCanvas_Drop(object sender, DragEventArgs e)
    {
        if (DataContext is not NodeCanvasViewModel canvasVm)
            return;

        var point = e.GetPosition(EditorCanvas);
        var canvasX = (point.X - canvasVm.OffsetX) / canvasVm.Zoom;
        var canvasY = (point.Y - canvasVm.OffsetY) / canvasVm.Zoom;

        if (e.Data.GetData(typeof(NodeMetadata)) is NodeMetadata metadata)
        {
            canvasVm.AddNode(metadata.TypeId, canvasX, canvasY);
            e.Handled = true;
            return;
        }

        if (e.Data.GetData(DataFormats.StringFormat) is string typeId && !string.IsNullOrWhiteSpace(typeId))
        {
            canvasVm.AddNode(typeId, canvasX, canvasY);
            e.Handled = true;
        }
    }

    private void PaletteItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is not NodeMetadata)
            return;

        _paletteDragStart = e.GetPosition(this);
    }

    private void PaletteItem_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is not NodeMetadata metadata)
            return;

        if (e.LeftButton != MouseButtonState.Pressed)
            return;

        Point current = e.GetPosition(this);
        if (Math.Abs(current.X - _paletteDragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(current.Y - _paletteDragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        var data = new DataObject();
        data.SetData(typeof(NodeMetadata), metadata);
        data.SetData(DataFormats.StringFormat, metadata.TypeId);
        DragDrop.DoDragDrop(element, data, DragDropEffects.Copy);
        e.Handled = true;
    }

    private void Node_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDraggingNode && _draggedNode != null && sender is Border border && border.IsMouseCaptured)
        {
            Point current = e.GetPosition(EditorCanvas);
            Vector delta = current - _dragStart;

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
            if (_draggedNode != null && DataContext is NodeCanvasViewModel canvasVm)
            {
                canvasVm.MoveNode(_draggedNode, _draggedNode.X, _draggedNode.Y);
            }

            _isDraggingNode = false;
            _draggedNode = null;
            border.ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    private void Pin_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is NodePinViewModel pinVm && pinVm.Direction == PinDirection.Output)
        {
            var nodeVm = FindParentDataContext<NodeViewModel>(element);
            if (nodeVm != null && DataContext is NodeCanvasViewModel canvasVm)
            {
                _pendingSourceNode = nodeVm;
                _pendingSourcePin = pinVm.Pin;
                canvasVm.BeginConnectionPreview(nodeVm, pinVm.Id, e.GetPosition(EditorCanvas));
                e.Handled = true;
            }
        }
    }

    private void Pin_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        bool completed = false;

        if (_pendingSourceNode != null && _pendingSourcePin != null &&
            sender is FrameworkElement element && element.DataContext is NodePinViewModel targetPinVm &&
            targetPinVm.Direction == PinDirection.Input)
        {
            var targetNodeVm = FindParentDataContext<NodeViewModel>(element);
            if (targetNodeVm != null && targetNodeVm.InstanceId != _pendingSourceNode.InstanceId &&
                DataContext is NodeCanvasViewModel canvasVm)
            {
                completed = canvasVm.TryCompleteConnection(targetNodeVm, targetPinVm.Id);
            }
        }

        // If not completed, cancel preview (e.g., released on wrong pin type)
        if (!completed && DataContext is NodeCanvasViewModel cv && cv.IsConnectingPins)
            cv.CancelConnectionPreview();

        _pendingSourceNode = null;
        _pendingSourcePin = null;

        if (completed)
            e.Handled = true;
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

    private static bool ShouldIgnoreDrag(DependencyObject? source)
    {
        while (source != null)
        {
            if (source is TextBoxBase or ComboBox or ButtonBase)
                return true;

            if (source is FrameworkElement fe && fe.DataContext is NodePinViewModel)
                return true;

            source = VisualTreeHelper.GetParent(source);
        }

        return false;
    }
}
