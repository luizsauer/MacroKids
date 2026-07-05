using MacroKids.Core.Models;

namespace MacroKids.Core.Commands;

/// <summary>Creates a new node instance on the canvas.</summary>
public sealed class CreateNodeCommand : IEditorCommand
{
    private readonly FlowDocument _document;
    private readonly FlowNode _node;

    public CreateNodeCommand(FlowDocument document, FlowNode node)
    {
        _document = document;
        _node = node;
    }

    public string Description => $"Criar nó '{_node.TypeId}'";

    public void Execute() => _document.Nodes.Add(_node);

    public void Undo()
    {
        _document.Nodes.Remove(_node);
        // Also remove any connections involving this node
        _document.Connections.RemoveAll(
            c => c.SourceNodeId == _node.InstanceId || c.TargetNodeId == _node.InstanceId);
    }
}

/// <summary>Deletes one or more node instances from the canvas.</summary>
public sealed class DeleteNodeCommand : IEditorCommand
{
    private readonly FlowDocument _document;
    private readonly IReadOnlyList<FlowNode> _nodes;
    private readonly List<FlowConnection> _removedConnections = [];

    public DeleteNodeCommand(FlowDocument document, IEnumerable<FlowNode> nodes)
    {
        _document = document;
        _nodes = nodes.ToList();
    }

    public string Description =>
        _nodes.Count == 1
            ? $"Deletar nó '{_nodes[0].TypeId}'"
            : $"Deletar {_nodes.Count} nós";

    public void Execute()
    {
        _removedConnections.Clear();
        var nodeIds = _nodes.Select(n => n.InstanceId).ToHashSet();

        // Capture connections before removing them
        _removedConnections.AddRange(
            _document.Connections.Where(
                c => nodeIds.Contains(c.SourceNodeId) || nodeIds.Contains(c.TargetNodeId)));

        _document.Connections.RemoveAll(
            c => nodeIds.Contains(c.SourceNodeId) || nodeIds.Contains(c.TargetNodeId));

        foreach (var node in _nodes)
            _document.Nodes.Remove(node);
    }

    public void Undo()
    {
        _document.Nodes.AddRange(_nodes);
        _document.Connections.AddRange(_removedConnections);
    }
}

/// <summary>Moves one or more node instances to a new position on the canvas.</summary>
public sealed class MoveNodeCommand : IEditorCommand
{
    private record NodePosition(Guid InstanceId, double OldX, double OldY, double NewX, double NewY);

    private readonly FlowDocument _document;
    private readonly List<NodePosition> _moves;

    public MoveNodeCommand(
        FlowDocument document,
        IEnumerable<(FlowNode Node, double NewX, double NewY)> moves)
    {
        _document = document;
        _moves = moves
            .Select(m => new NodePosition(m.Node.InstanceId, m.Node.X, m.Node.Y, m.NewX, m.NewY))
            .ToList();
    }

    public string Description =>
        _moves.Count == 1 ? "Mover nó" : $"Mover {_moves.Count} nós";

    public void Execute() => ApplyPositions(useNew: true);
    public void Undo()    => ApplyPositions(useNew: false);

    private void ApplyPositions(bool useNew)
    {
        var nodeMap = _document.Nodes.ToDictionary(n => n.InstanceId);
        foreach (var move in _moves)
        {
            if (!nodeMap.TryGetValue(move.InstanceId, out var node))
                continue;

            node.X = useNew ? move.NewX : move.OldX;
            node.Y = useNew ? move.NewY : move.OldY;
        }
    }
}

/// <summary>Connects two pins (output → input) with a new wire.</summary>
public sealed class ConnectPinsCommand : IEditorCommand
{
    private readonly FlowDocument _document;
    private readonly FlowConnection _connection;

    public ConnectPinsCommand(FlowDocument document, FlowConnection connection)
    {
        _document = document;
        _connection = connection;
    }

    public string Description => "Conectar pins";

    public void Execute() => _document.Connections.Add(_connection);
    public void Undo()    => _document.Connections.Remove(_connection);
}

/// <summary>Removes an existing wire between two pins.</summary>
public sealed class DisconnectPinsCommand : IEditorCommand
{
    private readonly FlowDocument _document;
    private readonly FlowConnection _connection;

    public DisconnectPinsCommand(FlowDocument document, FlowConnection connection)
    {
        _document = document;
        _connection = connection;
    }

    public string Description => "Desconectar pins";

    public void Execute() => _document.Connections.Remove(_connection);
    public void Undo()    => _document.Connections.Add(_connection);
}
