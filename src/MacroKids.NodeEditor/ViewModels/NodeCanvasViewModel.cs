using System.Collections.ObjectModel;
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

    public NodeCanvasViewModel(INodeRegistry registry, CommandHistory history)
    {
        _registry = registry;
        _history  = history;

        _history.HistoryChanged += (_, _) =>
        {
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
        };
    }

    // ── Collections ───────────────────────────────────────────────────────────

    public ObservableCollection<NodeViewModel>      Nodes       { get; } = [];
    public ObservableCollection<ConnectionViewModel> Connections { get; } = [];

    // ── Viewport ──────────────────────────────────────────────────────────────

    [ObservableProperty] private double _zoom      = 1.0;
    [ObservableProperty] private double _offsetX   = 0.0;
    [ObservableProperty] private double _offsetY   = 0.0;

    public const double MinZoom = 0.1;
    public const double MaxZoom = 5.0;

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

    // ── Node operations ───────────────────────────────────────────────────────

    /// <summary>Place a new node on the canvas at the given canvas coordinates.</summary>
    public void AddNode(string typeId, double canvasX, double canvasY)
    {
        if (!_registry.TryGet(typeId, out var metadata, out _) || metadata is null)
            return;

        var flowNode = new FlowNode
        {
            InstanceId = Guid.NewGuid(),
            TypeId     = typeId,
            X          = canvasX,
            Y          = canvasY
        };

        var document = BuildFlowDocument(); // snapshot of current state
        var cmd      = new CreateNodeCommand(document, flowNode);

        _history.Execute(cmd);

        // Sync VM from updated document
        var vm = new NodeViewModel(flowNode, metadata);
        Nodes.Add(vm);
        SelectNode(vm);
    }

    /// <summary>Delete currently selected node.</summary>
    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void DeleteSelected()
    {
        if (SelectedNode is null) return;

        var document = BuildFlowDocument();
        var flowNode = document.Nodes.FirstOrDefault(n => n.InstanceId == SelectedNode.InstanceId);
        if (flowNode is null) return;

        var cmd = new DeleteNodeCommand(document, [flowNode]);
        _history.Execute(cmd);

        Nodes.Remove(SelectedNode);
        // Remove orphaned connections
        var orphaned = Connections
            .Where(c => c.SourceNodeId == SelectedNode.InstanceId ||
                        c.TargetNodeId == SelectedNode.InstanceId)
            .ToList();
        foreach (var conn in orphaned)
            Connections.Remove(conn);

        ClearSelection();
    }

    private bool HasSelection => SelectedNode is not null;

    // ── Move ─────────────────────────────────────────────────────────────────

    public void MoveNode(NodeViewModel vm, double newX, double newY)
    {
        var document = BuildFlowDocument();
        var flowNode = document.Nodes.FirstOrDefault(n => n.InstanceId == vm.InstanceId);
        if (flowNode is null) return;

        var cmd = new MoveNodeCommand(document, [(flowNode, newX, newY)]);
        _history.Execute(cmd);

        vm.X = newX;
        vm.Y = newY;
    }

    // ── Connect pins ──────────────────────────────────────────────────────────

    public void ConnectPins(
        Guid sourceNodeId, string sourcePinId,
        Guid targetNodeId, string targetPinId)
    {
        // Prevent duplicate connections on the same target pin
        if (Connections.Any(c =>
                c.TargetNodeId == targetNodeId &&
                c.TargetPinId  == targetPinId))
            return;

        var sourceVm = Nodes.FirstOrDefault(n => n.InstanceId == sourceNodeId);
        var targetVm = Nodes.FirstOrDefault(n => n.InstanceId == targetNodeId);
        if (sourceVm == null || targetVm == null) return;

        var connection = new FlowConnection
        {
            Id           = Guid.NewGuid(),
            SourceNodeId = sourceNodeId,
            SourcePinId  = sourcePinId,
            TargetNodeId = targetNodeId,
            TargetPinId  = targetPinId
        };

        var document = BuildFlowDocument();
        var cmd      = new ConnectPinsCommand(document, connection);
        _history.Execute(cmd);

        Connections.Add(new ConnectionViewModel(connection, sourceVm, targetVm));
    }

    // ── Viewport helpers ─────────────────────────────────────────────────────

    public void SetZoom(double newZoom) =>
        Zoom = Math.Clamp(newZoom, MinZoom, MaxZoom);

    public void ZoomIn()  => SetZoom(Zoom * 1.15);
    public void ZoomOut() => SetZoom(Zoom / 1.15);
    public void ZoomReset() { Zoom = 1.0; OffsetX = 0; OffsetY = 0; }

    /// <summary>Center the viewport so all nodes are visible.</summary>
    public void FitToContent()
    {
        if (!Nodes.Any()) { ZoomReset(); return; }

        var minX = Nodes.Min(n => n.X);
        var minY = Nodes.Min(n => n.Y);
        OffsetX = -minX + 80;
        OffsetY = -minY + 80;
        Zoom    = 1.0;
    }

    // ── Undo / Redo ───────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanUndoAction))]
    private void Undo() { _history.Undo(); RebuildFromDocument(); }

    [RelayCommand(CanExecute = nameof(CanRedoAction))]
    private void Redo() { _history.Redo(); RebuildFromDocument(); }

    private bool CanUndoAction => _history.CanUndo;
    private bool CanRedoAction => _history.CanRedo;

    // ── Document sync ─────────────────────────────────────────────────────────

    private FlowDocument BuildFlowDocument() => new()
    {
        Id            = Guid.NewGuid(),
        Name          = "Untitled",
        CreatedAt     = DateTime.UtcNow,
        UpdatedAt     = DateTime.UtcNow,
        EngineVersion = "0.1.0",
        Nodes         = [.. Nodes.Select(vm => vm.ToFlowNode())],
        Connections   = [.. Connections.Select(vm => vm.ToFlowConnection())]
    };

    private void RebuildFromDocument()
    {
        // After Undo/Redo, rebuilt VMs from the updated document
        // In a full implementation this syncs Nodes and Connections collections
        // For now, this is a placeholder — Phase 3 will implement full sync
    }

    // ── Load a FlowDocument ───────────────────────────────────────────────────

    public void LoadDocument(FlowDocument document)
    {
        Nodes.Clear();
        Connections.Clear();

        foreach (var node in document.Nodes)
        {
            if (_registry.TryGet(node.TypeId, out var meta, out _) && meta is not null)
                Nodes.Add(new NodeViewModel(node, meta));
        }

        foreach (var conn in document.Connections)
        {
            var sourceVm = Nodes.FirstOrDefault(n => n.InstanceId == conn.SourceNodeId);
            var targetVm = Nodes.FirstOrDefault(n => n.InstanceId == conn.TargetNodeId);
            if (sourceVm != null && targetVm != null)
                Connections.Add(new ConnectionViewModel(conn, sourceVm, targetVm));
        }

        OffsetX = document.CanvasOffsetX;
        OffsetY = document.CanvasOffsetY;
        Zoom    = document.CanvasZoom;
    }
}
