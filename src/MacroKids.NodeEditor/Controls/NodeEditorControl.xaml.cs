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
            bool isCtrlOrShift = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) ||
                                 Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

            // Se o nó já estiver selecionado e iniciamos o drag, preserva a seleção múltipla. 
            // Senão, seleciona normalmente.
            if (!nodeVm.IsSelected)
            {
                canvasVm.SelectNode(nodeVm, toggle: true, selectMultiple: isCtrlOrShift);
            }
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

            if (DataContext is NodeCanvasViewModel canvasVm && canvasVm.SelectedNodes.Count > 1 && _draggedNode.IsSelected)
            {
                foreach (var n in canvasVm.SelectedNodes)
                {
                    n.X += delta.X;
                    n.Y += delta.Y;
                }
            }
            else
            {
                _draggedNode.X += delta.X;
                _draggedNode.Y += delta.Y;
            }

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
                if (canvasVm.SelectedNodes.Count > 1 && _draggedNode.IsSelected)
                {
                    foreach (var n in canvasVm.SelectedNodes)
                    {
                        canvasVm.MoveNode(n, n.X, n.Y);
                    }
                }
                else
                {
                    canvasVm.MoveNode(_draggedNode, _draggedNode.X, _draggedNode.Y);
                }
            }

            _isDraggingNode = false;
            _draggedNode = null;
            border.ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    private void Pin_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is NodePinViewModel pinVm && DataContext is NodeCanvasViewModel canvasVm)
        {
            if (e.ClickCount == 2)
            {
                var targetId = pinVm.Direction == PinDirection.Input ? pinVm.Id : null;
                var sourceId = pinVm.Direction == PinDirection.Output ? pinVm.Id : null;
                var nodeVm = FindParentDataContext<NodeViewModel>(element);

                if (nodeVm != null)
                {
                    var connsToRemove = canvasVm.Connections
                        .Where(c => (c.SourceNodeId == nodeVm.InstanceId && c.SourcePinId == sourceId) ||
                                    (c.TargetNodeId == nodeVm.InstanceId && c.TargetPinId == targetId))
                        .ToList();

                    foreach (var conn in connsToRemove)
                    {
                        canvasVm.DisconnectConnection(conn.ConnectionId);
                    }
                    e.Handled = true;
                    return;
                }
            }

            if (pinVm.Direction == PinDirection.Output)
            {
                var nodeVm = FindParentDataContext<NodeViewModel>(element);
                if (nodeVm != null)
                {
                    _pendingSourceNode = nodeVm;
                    _pendingSourcePin = pinVm.Pin;
                    canvasVm.BeginConnectionPreview(nodeVm, pinVm.Id, e.GetPosition(EditorCanvas));
                    e.Handled = true;
                }
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

    private void KeyCaptureTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is TextBox textBox && textBox.DataContext is NodePinViewModel pinVm)
        {
            e.Handled = true;

            string keyName = e.Key.ToString();
            if (e.Key == Key.System)
                keyName = e.SystemKey.ToString();

            // Mapeamentos amigáveis para teclas comuns
            keyName = keyName switch
            {
                "LeftCtrl" or "RightCtrl" => "Ctrl",
                "LeftShift" or "RightShift" => "Shift",
                "LeftAlt" or "RightAlt" => "Alt",
                "Return" => "Enter",
                "Escape" => "Esc",
                "Back" => "Backspace",
                "Space" => "Space",
                _ => keyName
            };

            // Se for pin de combinação de teclas (combo)
            if (pinVm.Id == "combo")
            {
                string modifiers = "";
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) modifiers += "Ctrl+";
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) modifiers += "Shift+";
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) modifiers += "Alt+";

                // Se o usuário só pressionou um modificador isolado
                if (keyName == "Ctrl" || keyName == "Shift" || keyName == "Alt" || keyName == "None")
                {
                    if (modifiers.EndsWith('+'))
                        pinVm.Value = modifiers.Substring(0, modifiers.Length - 1);
                    else
                        pinVm.Value = keyName;
                }
                else
                {
                    pinVm.Value = modifiers + keyName;
                    Keyboard.ClearFocus(); // Desfoca ao capturar a combinação completa
                }
            }
            else
            {
                // Captura simples de tecla única
                pinVm.Value = keyName;
                Keyboard.ClearFocus();
            }
        }
    }

    private Point _selectionStartPoint;
    private bool _isSelecting;

    private void EditorCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not NodeCanvasViewModel canvasVm)
            return;

        // Se clicar em um nó ou pino, não desenha o retângulo de seleção
        if (e.OriginalSource is DependencyObject depObj && (IsOverNodeElement(depObj) || IsOverPinElement(depObj)))
            return;

        _isSelecting = true;
        _selectionStartPoint = e.GetPosition(EditorCanvas);

        SelectionRectangle.Width = 0;
        SelectionRectangle.Height = 0;
        SelectionRectangle.Visibility = Visibility.Visible;
        Canvas.SetLeft(SelectionRectangle, _selectionStartPoint.X);
        Canvas.SetTop(SelectionRectangle, _selectionStartPoint.Y);

        EditorCanvas.CaptureMouse();
        
        // Limpa a seleção anterior se Ctrl/Shift não estiverem pressionados
        if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl) &&
            !Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
        {
            canvasVm.ClearSelection();
        }

        e.Handled = true;
    }

    private void EditorCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isSelecting || !EditorCanvas.IsMouseCaptured)
            return;

        Point currentPoint = e.GetPosition(EditorCanvas);

        double x = Math.Min(_selectionStartPoint.X, currentPoint.X);
        double y = Math.Min(_selectionStartPoint.Y, currentPoint.Y);
        double width = Math.Abs(_selectionStartPoint.X - currentPoint.X);
        double height = Math.Abs(_selectionStartPoint.Y - currentPoint.Y);

        Canvas.SetLeft(SelectionRectangle, x);
        Canvas.SetTop(SelectionRectangle, y);
        SelectionRectangle.Width = width;
        SelectionRectangle.Height = height;

        e.Handled = true;
    }

    private void EditorCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isSelecting)
        {
            _isSelecting = false;
            SelectionRectangle.Visibility = Visibility.Collapsed;
            EditorCanvas.ReleaseMouseCapture();

            if (DataContext is NodeCanvasViewModel canvasVm)
            {
                double rectLeft = Canvas.GetLeft(SelectionRectangle);
                double rectTop = Canvas.GetTop(SelectionRectangle);
                Rect selectionRect = new Rect(rectLeft, rectTop, SelectionRectangle.Width, SelectionRectangle.Height);

                bool selectMultiple = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) ||
                                     Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

                foreach (var node in canvasVm.Nodes)
                {
                    // Usa estimativa de tamanho do nó de 215x150
                    Rect nodeRect = new Rect(node.X, node.Y, 215, 150);
                    if (selectionRect.IntersectsWith(nodeRect))
                    {
                        canvasVm.SelectNode(node, toggle: false, selectMultiple: true);
                    }
                }
            }
            e.Handled = true;
        }
    }

    private static bool IsOverNodeElement(DependencyObject? source)
    {
        while (source != null)
        {
            if (source is FrameworkElement fe && fe.DataContext is NodeViewModel)
                return true;
            source = VisualTreeHelper.GetParent(source);
        }
        return false;
    }
}

