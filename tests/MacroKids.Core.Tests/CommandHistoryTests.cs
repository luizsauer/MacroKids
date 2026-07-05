using MacroKids.Core.Commands;
using MacroKids.Core.Models;

namespace MacroKids.Core.Tests;

public class CommandHistoryTests
{
    private static FlowDocument CreateDocument() => new()
    {
        Id            = Guid.NewGuid(),
        Name          = "Test",
        CreatedAt     = DateTime.UtcNow,
        UpdatedAt     = DateTime.UtcNow,
        EngineVersion = "0.1.0"
    };

    private static FlowNode CreateNode(string typeId = "test.node") => new()
    {
        InstanceId = Guid.NewGuid(),
        TypeId     = typeId,
        X          = 100,
        Y          = 200
    };

    // ── CommandHistory ────────────────────────────────────────────────────────

    [Fact]
    public void Execute_AddsToUndoStack()
    {
        var history = new CommandHistory();
        var doc  = CreateDocument();
        var node = CreateNode();
        var cmd  = new CreateNodeCommand(doc, node);

        history.Execute(cmd);

        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);
    }

    [Fact]
    public void Undo_MovesCommandToRedoStack()
    {
        var history = new CommandHistory();
        var doc  = CreateDocument();
        var node = CreateNode();

        history.Execute(new CreateNodeCommand(doc, node));
        history.Undo();

        Assert.False(history.CanUndo);
        Assert.True(history.CanRedo);
    }

    [Fact]
    public void Redo_ReExecutesCommand()
    {
        var history = new CommandHistory();
        var doc  = CreateDocument();
        var node = CreateNode();

        history.Execute(new CreateNodeCommand(doc, node));
        history.Undo();
        history.Redo();

        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.Contains(node, doc.Nodes);
    }

    [Fact]
    public void NewExecute_ClearsRedoStack()
    {
        var history = new CommandHistory();
        var doc   = CreateDocument();
        var node1 = CreateNode("n1");
        var node2 = CreateNode("n2");

        history.Execute(new CreateNodeCommand(doc, node1));
        history.Undo();
        history.Execute(new CreateNodeCommand(doc, node2)); // clears redo

        Assert.False(history.CanRedo);
    }

    // ── CreateNodeCommand ─────────────────────────────────────────────────────

    [Fact]
    public void CreateNode_Execute_AddsNodeToDocument()
    {
        var doc  = CreateDocument();
        var node = CreateNode();
        var cmd  = new CreateNodeCommand(doc, node);

        cmd.Execute();

        Assert.Contains(node, doc.Nodes);
    }

    [Fact]
    public void CreateNode_Undo_RemovesNodeFromDocument()
    {
        var doc  = CreateDocument();
        var node = CreateNode();
        var cmd  = new CreateNodeCommand(doc, node);

        cmd.Execute();
        cmd.Undo();

        Assert.DoesNotContain(node, doc.Nodes);
    }

    // ── MoveNodeCommand ───────────────────────────────────────────────────────

    [Fact]
    public void MoveNode_Execute_UpdatesPosition()
    {
        var doc  = CreateDocument();
        var node = CreateNode();
        doc.Nodes.Add(node);

        var cmd = new MoveNodeCommand(doc, [(node, 500, 600)]);
        cmd.Execute();

        Assert.Equal(500, node.X);
        Assert.Equal(600, node.Y);
    }

    [Fact]
    public void MoveNode_Undo_RestoresOriginalPosition()
    {
        var doc  = CreateDocument();
        var node = CreateNode(); // X=100, Y=200
        doc.Nodes.Add(node);

        var cmd = new MoveNodeCommand(doc, [(node, 500, 600)]);
        cmd.Execute();
        cmd.Undo();

        Assert.Equal(100, node.X);
        Assert.Equal(200, node.Y);
    }

    // ── ConnectPinsCommand ────────────────────────────────────────────────────

    [Fact]
    public void ConnectPins_Execute_AddsConnection()
    {
        var doc    = CreateDocument();
        var conn   = new FlowConnection
        {
            Id           = Guid.NewGuid(),
            SourceNodeId = Guid.NewGuid(),
            SourcePinId  = "out",
            TargetNodeId = Guid.NewGuid(),
            TargetPinId  = "in"
        };

        var cmd = new ConnectPinsCommand(doc, conn);
        cmd.Execute();

        Assert.Contains(conn, doc.Connections);
    }

    [Fact]
    public void ConnectPins_Undo_RemovesConnection()
    {
        var doc  = CreateDocument();
        var conn = new FlowConnection
        {
            Id           = Guid.NewGuid(),
            SourceNodeId = Guid.NewGuid(),
            SourcePinId  = "out",
            TargetNodeId = Guid.NewGuid(),
            TargetPinId  = "in"
        };

        var cmd = new ConnectPinsCommand(doc, conn);
        cmd.Execute();
        cmd.Undo();

        Assert.DoesNotContain(conn, doc.Connections);
    }
}
