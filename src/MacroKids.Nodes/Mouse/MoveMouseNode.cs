using System.Collections.Generic;
using System.Threading.Tasks;
using MacroKids.Core.Interfaces;
using MacroKids.Core.Interop;
using MacroKids.Core.Models;
using MacroKids.Core.Services;

namespace MacroKids.Nodes.Mouse;

public static class MoveMouseMetadata
{
    public static readonly NodeMetadata Instance = new()
    {
        TypeId = "mouse.move",
        Name = "Mover Mouse",
        Description = "Move o cursor do mouse para a coordenada (X, Y) na tela (suporta múltiplos monitores).",
        Category = NodeCategory.Mouse,
        IconKey = "Mouse",
        NodeVersion = new Version(1, 1, 0),
        Pins = [
            new NodePin { Id = "in", Label = "Entrada", Direction = PinDirection.Input, DataType = typeof(bool) },
            new NodePin { Id = "x", Label = "Posição X", Direction = PinDirection.Input, DataType = typeof(int), DefaultValue = 0 },
            new NodePin { Id = "y", Label = "Posição Y", Direction = PinDirection.Input, DataType = typeof(int), DefaultValue = 0 },
            new NodePin { Id = "done", Label = "Concluído", Direction = PinDirection.Output, DataType = typeof(bool) }
        ]
    };
}

public class MoveMouseExecutor : INodeExecutor
{
    public Task<NodeExecutionResult> ExecuteAsync(
        FlowNode node,
        IExecutionContext context,
        IReadOnlyDictionary<string, object?> resolvedInputs)
    {
        int x = PinValueReader.GetInt(resolvedInputs, node.PinValues, "x", 0);
        int y = PinValueReader.GetInt(resolvedInputs, node.PinValues, "y", 0);

        context.Log($"Movendo cursor do mouse para ({x}, {y})");
        NativeInput.MoveMouse(x, y);
        return Task.FromResult(NodeExecutionResult.Success(new() { ["done"] = true }));
    }
}
