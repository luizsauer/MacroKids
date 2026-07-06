using System.Runtime.InteropServices;
using MacroKids.Core.Interfaces;
using MacroKids.Core.Models;

namespace MacroKids.Nodes.Mouse;

/// <summary>
/// Metadata definition for the Move Mouse visual block.
/// </summary>
public static class MoveMouseMetadata
{
    public static readonly NodeMetadata Instance = new()
    {
        TypeId = "mouse.move",
        Name = "Mover Mouse",
        Description = "Move o cursor do mouse para a coordenada (X, Y) na tela.",
        Category = NodeCategory.Mouse,
        IconKey = "Mouse",
        NodeVersion = new Version(1, 0, 0),
        Pins = [
            new NodePin { Id = "x", Label = "Posição X", Direction = PinDirection.Input, DataType = typeof(int), DefaultValue = 0 },
            new NodePin { Id = "y", Label = "Posição Y", Direction = PinDirection.Input, DataType = typeof(int), DefaultValue = 0 },
            new NodePin { Id = "done", Label = "Concluído", Direction = PinDirection.Output, DataType = typeof(bool) }
        ]
    };
}

/// <summary>
/// Executor to simulate cursor movement.
/// </summary>
public class MoveMouseExecutor : INodeExecutor
{
    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    public Task<NodeExecutionResult> ExecuteAsync(
        FlowNode node,
        IExecutionContext context,
        IReadOnlyDictionary<string, object?> resolvedInputs)
    {
        int x = 0;
        int y = 0;

        if (resolvedInputs.TryGetValue("x", out var xVal) && xVal is int rx)
            x = rx;
        else if (node.PinValues.TryGetValue("x", out var sx) && sx is int sxInt)
            x = sxInt;

        if (resolvedInputs.TryGetValue("y", out var yVal) && yVal is int ry)
            y = ry;
        else if (node.PinValues.TryGetValue("y", out var sy) && sy is int syInt)
            y = syInt;

        context.Log($"Movendo cursor do mouse para ({x}, {y})");

        // Native Windows cursor placement
        SetCursorPos(x, y);

        return Task.FromResult(NodeExecutionResult.Success(new() { ["done"] = true }));
    }
}
