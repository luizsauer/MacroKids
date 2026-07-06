using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacroKids.Core.Commands;
using MacroKids.Core.Interfaces;
using MacroKids.Core.Models;

namespace MacroKids.NodeEditor.ViewModels;

/// <summary>
/// ViewModel for the infinite canvas editor.
/// Owns the collection of nodes and connections, handles viewport state (zoom/pan),
/// and wraps all mutations through the <see cref="CommandHistory"/> for Undo/Redo.
/// </summary>
public sealed partial class NodeCanvasViewModel : ObservableObject
{
    private readonly INodeRegistry _registry;
    private readonly CommandHistory _history;
    private FlowDocument _document;

    public NodeCanvasViewModel(INodeRegistry registry, CommandHistory? history = null)
    {
        _registry = registry;
        _history = history ?? new CommandHistory();
        _document = new FlowDocument
        {
            Id = Guid.NewGuid(),
            Name = "Untitled",
            Description = string.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            EngineVersion = "0.1.4-dev"
        };

        _history.HistoryChanged += (_, _) =>
        {
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
        };
    }

    // ── Collections ───────────────────────────────────────────────────────────

    public ObservableCollection<NodeViewModel> Nodes { get; } = [];
    public ObservableCollection<ConnectionViewModel> Connections { get; } = [];

    // ── Viewport ──────────────────────────────────────────────────────────────

    [ObservableProperty] private double _zoom = 1.0;
    [ObservableProperty] private double _offsetX;
    [ObservableProperty] private double _offsetY;
    [ObservableProperty] private bool _isGridVisible = true;
    [ObservableProperty] private bool _isConnectingPins;
    [ObservableProperty] private Point _connectionPreviewStart;
    [ObservableProperty] private Point _connectionPreviewEnd;
    [ObservableProperty] private Guid? _connectionSourceNodeId;
    [ObservableProperty] private string? _connectionSourcePinId;

    public const double MinZoom = 0.1;
    public const double MaxZoom = 5.0;
    public string ZoomLabel => $"{Zoom:P0}";

    // ── Selection ─────────────────────────────────────────────────────────────

    [ObservableProperty] private NodeViewModel? _selectedNode;

    public void SelectNode(NodeViewModel? node)
    {
        if (SelectedNode is not null)
            SelectedNode.IsSelected = false;

        SelectedNode = node;

        if (node is not null)
            node.IsSelected = true;
    }

    public void ClearSelection() => SelectNode(null);

    partial void OnZoomChanged(double value) => OnPropertyChanged(nameof(ZoomLabel));

    public void BeginConnectionPreview(NodeViewModel sourceNode, string sourcePinId, Point startPoint)
    {
        ConnectionSourceNodeId = sourceNode.InstanceId;
        ConnectionSourcePinId = sourcePinId;
        ConnectionPreviewStart = startPoint;
        ConnectionPreviewEnd = startPoint;
        IsConnectingPins = true;
    }

    public void UpdateConnectionPreview(Point currentPoint)
    {
        if (!IsConnectingPins)
            return;

        ConnectionPreviewEnd = currentPoint;
    }

    public void CancelConnectionPreview()
    {
        IsConnectingPins = false;
        ConnectionSourceNodeId = null;
        ConnectionSourcePinId = null;
    }

    public bool TryCompleteConnection(NodeViewModel targetNode, string targetPinId)
    {
        if (!IsConnectingPins || ConnectionSourceNodeId is null || string.IsNullOrWhiteSpace(ConnectionSourcePinId))
            return false;

        ConnectPins(ConnectionSourceNodeId.Value, ConnectionSourcePinId, targetNode.InstanceId, targetPinId);
        CancelConnectionPreview();
        return true;
    }

    // ── Node operations ───────────────────────────────────────────────────────

    /// <summary>
    /// Syncs all current VM PinValues back to the corresponding FlowNodes in the document.
    /// Call before any RebuildFromDocument so inline edits survive the rebuild.
    /// </summary>
    private void SyncVmPinValuesToDocument()
    {
        var nodeMap = _document.Nodes.ToDictionary(n => n.InstanceId);
        foreach (var vm in Nodes)
        {
            if (nodeMap.TryGetValue(vm.InstanceId, out var node))
                node.PinValues = new Dictionary<string, object?>(vm.PinValues);
        }
    }

    /// <summary>Place a new node on the canvas at the given canvas coordinates.</summary>
    public void AddNode(string typeId, double canvasX, double canvasY)
    {
        if (!_registry.TryGet(typeId, out var metadata, out _) || metadata is null)
            return;

        SyncVmPinValuesToDocument();

        var flowNode = new FlowNode
        {
            InstanceId = Guid.NewGuid(),
            TypeId = typeId,
            X = canvasX,
            Y = canvasY
        };

        _history.Execute(new CreateNodeCommand(_document, flowNode));
        TouchDocument();
        RebuildFromDocument(flowNode.InstanceId, preserveViewport: true);
    }

    /// <summary>Delete currently selected node.</summary>
    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void DeleteSelected()
    {
        if (SelectedNode is null)
            return;

        var flowNode = _document.Nodes.FirstOrDefault(n => n.InstanceId == SelectedNode.InstanceId);
        if (flowNode is null)
            return;

        SyncVmPinValuesToDocument();
        _history.Execute(new DeleteNodeCommand(_document, [flowNode]));
        TouchDocument();
        ClearSelection();
        RebuildFromDocument(preserveViewport: true);
    }

    /// <summary>Duplicate currently selected node.</summary>
    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void DuplicateSelected()
    {
        if (SelectedNode is null)
            return;

        var flowNode = _document.Nodes.FirstOrDefault(n => n.InstanceId == SelectedNode.InstanceId);
        if (flowNode is null)
            return;

        SyncVmPinValuesToDocument();

        // Create a copy with offset
        var duplicateNode = CloneNode(flowNode, Guid.NewGuid());
        duplicateNode.X += 30;
        duplicateNode.Y += 30;

        _history.Execute(new CreateNodeCommand(_document, duplicateNode));
        TouchDocument();
        RebuildFromDocument(duplicateNode.InstanceId, preserveViewport: true);
    }

    private bool HasSelection => SelectedNode is not null;

    // ── Move ─────────────────────────────────────────────────────────────────

    public void MoveNode(NodeViewModel vm, double newX, double newY)
    {
        var flowNode = _document.Nodes.FirstOrDefault(n => n.InstanceId == vm.InstanceId);
        if (flowNode is null)
            return;

        // Persist current pin values so they survive any future undo/redo rebuild
        flowNode.PinValues = new Dictionary<string, object?>(vm.PinValues);

        _history.Execute(new MoveNodeCommand(_document, [(flowNode, newX, newY)]));
        TouchDocument();
        // No RebuildFromDocument: X/Y already updated live on the VM during drag.
        // ConnectionViewModels update automatically via PropertyChanged on NodeViewModel.X/Y.
    }

    // ── Connect pins ──────────────────────────────────────────────────────────

    public void ConnectPins(
        Guid sourceNodeId, string sourcePinId,
        Guid targetNodeId, string targetPinId)
    {
        if (Connections.Any(c =>
                c.TargetNodeId == targetNodeId &&
                c.TargetPinId == targetPinId))
            return;

        var sourceVm = Nodes.FirstOrDefault(n => n.InstanceId == sourceNodeId);
        var targetVm = Nodes.FirstOrDefault(n => n.InstanceId == targetNodeId);
        if (sourceVm == null || targetVm == null)
            return;

        var connection = new FlowConnection
        {
            Id = Guid.NewGuid(),
            SourceNodeId = sourceNodeId,
            SourcePinId = sourcePinId,
            TargetNodeId = targetNodeId,
            TargetPinId = targetPinId
        };

        _history.Execute(new ConnectPinsCommand(_document, connection));
        TouchDocument();
        SyncVmPinValuesToDocument();
        RebuildFromDocument(preserveViewport: true);
    }

    public void DisconnectConnection(Guid connectionId)
    {
        var connection = _document.Connections.FirstOrDefault(c => c.Id == connectionId);
        if (connection is null)
            return;

        SyncVmPinValuesToDocument();
        _history.Execute(new DisconnectPinsCommand(_document, connection));
        TouchDocument();
        RebuildFromDocument(preserveViewport: true);
    }

    [RelayCommand]
    private void FitToContent()
    {
        if (!Nodes.Any())
        {
            ZoomReset();
            return;
        }

        const double padding = 80.0;
        var minX = Nodes.Min(n => n.X);
        var minY = Nodes.Min(n => n.Y);

        // Viewport formula: viewport_pos = canvas_pos * zoom + offset
        // To make (minX, minY) appear at screen (padding, padding):
        //   padding = minX * Zoom + OffsetX  →  OffsetX = padding - minX * Zoom
        OffsetX = padding - minX * Zoom;
        OffsetY = padding - minY * Zoom;
    }

    [RelayCommand]
    private void ToggleGrid() => IsGridVisible = !IsGridVisible;

    // ── Viewport helpers ─────────────────────────────────────────────────────

    public void SetZoom(double newZoom) =>
        Zoom = Math.Clamp(newZoom, MinZoom, MaxZoom);

    [RelayCommand] private void ZoomIn() => SetZoom(Zoom * 1.15);
    [RelayCommand] private void ZoomOut() => SetZoom(Zoom / 1.15);
    [RelayCommand] private void ZoomReset() { Zoom = 1.0; OffsetX = 0; OffsetY = 0; }

    // ── Undo / Redo ───────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanUndoAction))]
    private void Undo()
    {
        _history.Undo();
        RebuildFromDocument(preserveViewport: true);
    }

    [RelayCommand(CanExecute = nameof(CanRedoAction))]
    private void Redo()
    {
        _history.Redo();
        RebuildFromDocument(preserveViewport: true);
    }

    private bool CanUndoAction => _history.CanUndo;
    private bool CanRedoAction => _history.CanRedo;

    // ── Document sync ─────────────────────────────────────────────────────────

    public FlowDocument ToFlowDocument()
    {
        var existingNodes = _document.Nodes.ToDictionary(n => n.InstanceId);
        var nextNodes = new List<FlowNode>(Nodes.Count);

        foreach (var vm in Nodes)
        {
            if (!existingNodes.TryGetValue(vm.InstanceId, out var node))
            {
                node = vm.ToFlowNode();
            }
            else
            {
                node.X = vm.X;
                node.Y = vm.Y;
                node.PinValues = new Dictionary<string, object?>(vm.PinValues);
                node.Comment = vm.Comment;
                node.IsDisabled = vm.IsDisabled;
            }

            nextNodes.Add(node);
        }

        _document.Nodes.Clear();
        _document.Nodes.AddRange(nextNodes);

        var existingConnections = _document.Connections.ToDictionary(c => c.Id);
        var nextConnections = new List<FlowConnection>(Connections.Count);

        foreach (var vm in Connections)
        {
            if (!existingConnections.TryGetValue(vm.ConnectionId, out var connection))
                connection = vm.ToFlowConnection();

            nextConnections.Add(connection);
        }

        _document.Connections.Clear();
        _document.Connections.AddRange(nextConnections);
        _document.CanvasOffsetX = OffsetX;
        _document.CanvasOffsetY = OffsetY;
        _document.CanvasZoom = Zoom;
        _document.UpdatedAt = DateTime.UtcNow;
        return _document;
    }

    private void RebuildFromDocument(Guid? selectedNodeId = null, bool preserveViewport = false)
    {
        var previousSelected = selectedNodeId ?? SelectedNode?.InstanceId;

        Nodes.Clear();
        Connections.Clear();

        foreach (var node in _document.Nodes)
        {
            if (_registry.TryGet(node.TypeId, out var meta, out _) && meta is not null)
                Nodes.Add(new NodeViewModel(node, meta));
        }

        foreach (var conn in _document.Connections)
        {
            var sourceVm = Nodes.FirstOrDefault(n => n.InstanceId == conn.SourceNodeId);
            var targetVm = Nodes.FirstOrDefault(n => n.InstanceId == conn.TargetNodeId);
            if (sourceVm is not null && targetVm is not null)
                Connections.Add(new ConnectionViewModel(conn, sourceVm, targetVm));
        }

        if (!preserveViewport)
        {
            OffsetX = _document.CanvasOffsetX;
            OffsetY = _document.CanvasOffsetY;
            Zoom = _document.CanvasZoom;
        }

        if (previousSelected is Guid selectedId)
        {
            SelectNode(Nodes.FirstOrDefault(n => n.InstanceId == selectedId));
        }
    }

    // ── Load a FlowDocument ───────────────────────────────────────────────────

    public void LoadDocument(FlowDocument document)
    {
        _document = CloneDocument(document);
        _history.Clear();
        RebuildFromDocument(preserveViewport: false);
    }

    private static FlowDocument CloneDocument(FlowDocument document) => new()
    {
        Id = document.Id,
        Name = document.Name,
        Description = document.Description,
        CreatedAt = document.CreatedAt,
        UpdatedAt = document.UpdatedAt,
        SchemaVersion = document.SchemaVersion,
        EngineVersion = document.EngineVersion,
        MinimumEngineVersion = document.MinimumEngineVersion,
        Nodes = document.Nodes.Select(n => CloneNode(n, null)).ToList(),
        Connections = document.Connections.Select(CloneConnection).ToList(),
        CanvasOffsetX = document.CanvasOffsetX,
        CanvasOffsetY = document.CanvasOffsetY,
        CanvasZoom = document.CanvasZoom
    };

    private static FlowNode CloneNode(FlowNode node, Guid? newId = null) => new()
    {
        InstanceId = newId ?? node.InstanceId,
        TypeId = node.TypeId,
        X = node.X,
        Y = node.Y,
        PinValues = new Dictionary<string, object?>(node.PinValues),
        Comment = node.Comment,
        IsDisabled = node.IsDisabled
    };

    private static FlowConnection CloneConnection(FlowConnection connection) => new()
    {
        Id = connection.Id,
        SourceNodeId = connection.SourceNodeId,
        SourcePinId = connection.SourcePinId,
        TargetNodeId = connection.TargetNodeId,
        TargetPinId = connection.TargetPinId
    };

    private void TouchDocument() => _document.UpdatedAt = DateTime.UtcNow;

    public void ImportRecordedActions(List<RecordedAction> actions)
    {
        if (actions == null || actions.Count == 0) return;

        SyncVmPinValuesToDocument();

        _document.Nodes.Clear();
        _document.Connections.Clear();

        // 1. Optimize: group consecutive simple printable keys into a single string for type_text
        var optimized = new List<RecordedAction>();
        string textBuffer = "";
        int accumulatedDelay = 0;

        for (int i = 0; i < actions.Count; i++)
        {
            var act = actions[i];
            bool isPrintableChar = act.Type == ActionType.KeyPress && (act.KeyName.Length == 1 || act.KeyName == "Space");

            if (isPrintableChar)
            {
                accumulatedDelay += act.DelayMs;
                string charToAdd = act.KeyName == "Space" ? " " : act.KeyName.ToLowerInvariant();
                textBuffer += charToAdd;
            }
            else
            {
                if (textBuffer.Length > 0)
                {
                    optimized.Add(new RecordedAction(ActionType.KeyPress, 0, 0, accumulatedDelay, textBuffer));
                    textBuffer = "";
                    accumulatedDelay = 0;
                }
                optimized.Add(act);
            }
        }

        if (textBuffer.Length > 0)
        {
            optimized.Add(new RecordedAction(ActionType.KeyPress, 0, 0, accumulatedDelay, textBuffer));
        }

        // 2. Generate Graph
        double currentX = 100;
        double currentY = 150;
        Guid? previousDoneNodeId = null;

        foreach (var action in optimized)
        {
            // Only create wait blocks for delays >= 800ms
            if (action.DelayMs >= 800)
            {
                var waitNode = new FlowNode
                {
                    InstanceId = Guid.NewGuid(),
                    TypeId = "flow.wait",
                    X = currentX,
                    Y = currentY,
                    PinValues = new Dictionary<string, object?> { ["ms"] = action.DelayMs }
                };
                _document.Nodes.Add(waitNode);

                if (previousDoneNodeId != null)
                {
                    _document.Connections.Add(new FlowConnection
                    {
                        Id = Guid.NewGuid(),
                        SourceNodeId = previousDoneNodeId.Value,
                        SourcePinId = "done",
                        TargetNodeId = waitNode.InstanceId,
                        TargetPinId = "in"
                    });
                }
                previousDoneNodeId = waitNode.InstanceId;
                currentX += 300;
                if (currentX > 1500)
                {
                    currentX = 100;
                    currentY += 180;
                }
            }

            if (action.Type == ActionType.LeftClick || action.Type == ActionType.RightClick)
            {
                var moveNode = new FlowNode
                {
                    InstanceId = Guid.NewGuid(),
                    TypeId = "mouse.move",
                    X = currentX,
                    Y = currentY,
                    PinValues = new Dictionary<string, object?> { ["x"] = action.X, ["y"] = action.Y }
                };
                _document.Nodes.Add(moveNode);

                if (previousDoneNodeId != null)
                {
                    _document.Connections.Add(new FlowConnection
                    {
                        Id = Guid.NewGuid(),
                        SourceNodeId = previousDoneNodeId.Value,
                        SourcePinId = "done",
                        TargetNodeId = moveNode.InstanceId,
                        TargetPinId = "in"
                    });
                }
                previousDoneNodeId = moveNode.InstanceId;
                currentX += 300;
                if (currentX > 1500) { currentX = 100; currentY += 180; }

                string clickTypeId = action.Type == ActionType.LeftClick ? "mouse.left_click" : "mouse.right_click";
                var clickNode = new FlowNode
                {
                    InstanceId = Guid.NewGuid(),
                    TypeId = clickTypeId,
                    X = currentX,
                    Y = currentY
                };
                _document.Nodes.Add(clickNode);

                _document.Connections.Add(new FlowConnection
                {
                    Id = Guid.NewGuid(),
                    SourceNodeId = previousDoneNodeId.Value,
                    SourcePinId = "done",
                    TargetNodeId = clickNode.InstanceId,
                    TargetPinId = "in"
                });
                previousDoneNodeId = clickNode.InstanceId;
                currentX += 300;
                if (currentX > 1500) { currentX = 100; currentY += 180; }
            }
            else if (action.Type == ActionType.KeyPress)
            {
                // If the keyName length is > 1 and it's not a control key, it's grouped text
                bool isControlKey = action.KeyName == "Enter" || action.KeyName == "Space" || action.KeyName == "Backspace" ||
                                    action.KeyName == "Tab" || action.KeyName == "Esc" || action.KeyName == "Up" ||
                                    action.KeyName == "Down" || action.KeyName == "Left" || action.KeyName == "Right";

                if (action.KeyName.Length > 1 && !isControlKey)
                {
                    var typeTextNode = new FlowNode
                    {
                        InstanceId = Guid.NewGuid(),
                        TypeId = "keyboard.type_text",
                        X = currentX,
                        Y = currentY,
                        PinValues = new Dictionary<string, object?> { ["text"] = action.KeyName }
                    };
                    _document.Nodes.Add(typeTextNode);

                    if (previousDoneNodeId != null)
                    {
                        _document.Connections.Add(new FlowConnection
                        {
                            Id = Guid.NewGuid(),
                            SourceNodeId = previousDoneNodeId.Value,
                            SourcePinId = "done",
                            TargetNodeId = typeTextNode.InstanceId,
                            TargetPinId = "in"
                        });
                    }
                    previousDoneNodeId = typeTextNode.InstanceId;
                }
                else
                {
                    var keyNode = new FlowNode
                    {
                        InstanceId = Guid.NewGuid(),
                        TypeId = "keyboard.press_key",
                        X = currentX,
                        Y = currentY,
                        PinValues = new Dictionary<string, object?> { ["key"] = action.KeyName, ["times"] = 1 }
                    };
                    _document.Nodes.Add(keyNode);

                    if (previousDoneNodeId != null)
                    {
                        _document.Connections.Add(new FlowConnection
                        {
                            Id = Guid.NewGuid(),
                            SourceNodeId = previousDoneNodeId.Value,
                            SourcePinId = "done",
                            TargetNodeId = keyNode.InstanceId,
                            TargetPinId = "in"
                        });
                    }
                    previousDoneNodeId = keyNode.InstanceId;
                }
                currentX += 300;
                if (currentX > 1500) { currentX = 100; currentY += 180; }
            }
        }

        TouchDocument();
        RebuildFromDocument(preserveViewport: false);
    }
}
