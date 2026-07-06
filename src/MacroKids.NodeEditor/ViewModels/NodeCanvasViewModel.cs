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
            EngineVersion = "0.1.3-dev"
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

    /// <summary>Place a new node on the canvas at the given canvas coordinates.</summary>
    public void AddNode(string typeId, double canvasX, double canvasY)
    {
        if (!_registry.TryGet(typeId, out var metadata, out _) || metadata is null)
            return;

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

        _history.Execute(new DeleteNodeCommand(_document, [flowNode]));
        TouchDocument();
        ClearSelection();
        RebuildFromDocument(preserveViewport: true);
    }

    private bool HasSelection => SelectedNode is not null;

    // ── Move ─────────────────────────────────────────────────────────────────

    public void MoveNode(NodeViewModel vm, double newX, double newY)
    {
        var flowNode = _document.Nodes.FirstOrDefault(n => n.InstanceId == vm.InstanceId);
        if (flowNode is null)
            return;

        _history.Execute(new MoveNodeCommand(_document, [(flowNode, newX, newY)]));
        TouchDocument();
        RebuildFromDocument(vm.InstanceId, preserveViewport: true);
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
        RebuildFromDocument(preserveViewport: true);
    }

    public void DisconnectConnection(Guid connectionId)
    {
        var connection = _document.Connections.FirstOrDefault(c => c.Id == connectionId);
        if (connection is null)
            return;

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

        var minX = Nodes.Min(n => n.X);
        var minY = Nodes.Min(n => n.Y);
        OffsetX = -minX + 80;
        OffsetY = -minY + 80;
        Zoom = 1.0;
    }

    [RelayCommand]
    private void ToggleGrid() => IsGridVisible = !IsGridVisible;

    // ── Viewport helpers ─────────────────────────────────────────────────────

    public void SetZoom(double newZoom) =>
        Zoom = Math.Clamp(newZoom, MinZoom, MaxZoom);

    public void ZoomIn() => SetZoom(Zoom * 1.15);
    public void ZoomOut() => SetZoom(Zoom / 1.15);
    public void ZoomReset() { Zoom = 1.0; OffsetX = 0; OffsetY = 0; }

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
        Nodes = document.Nodes.Select(CloneNode).ToList(),
        Connections = document.Connections.Select(CloneConnection).ToList(),
        CanvasOffsetX = document.CanvasOffsetX,
        CanvasOffsetY = document.CanvasOffsetY,
        CanvasZoom = document.CanvasZoom
    };

    private static FlowNode CloneNode(FlowNode node) => new()
    {
        InstanceId = node.InstanceId,
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
}
