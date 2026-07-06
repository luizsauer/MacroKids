using System.Collections.Generic;
using System.Threading.Tasks;
using MacroKids.Core.Interfaces;
using MacroKids.Core.Interop;
using MacroKids.Core.Models;
using MacroKids.Core.Services;

namespace MacroKids.Nodes.Mouse;

public static class LeftClickMetadata
{
    public static readonly NodeMetadata Instance = new()
    {
        TypeId = "mouse.left_click",
        Name = "Clique Esquerdo",
        Description = "Executa um clique com o botão esquerdo na coordenada (X, Y) ou na posição atual se for -1.",
        Category = NodeCategory.Mouse,
        IconKey = "Mouse",
        NodeVersion = new Version(1, 2, 0),
        Pins = [
            new NodePin { Id = "in", Label = "Entrada", Direction = PinDirection.Input, DataType = typeof(bool) },
            new NodePin { Id = "x", Label = "Posição X", Direction = PinDirection.Input, DataType = typeof(int), DefaultValue = -1 },
            new NodePin { Id = "y", Label = "Posição Y", Direction = PinDirection.Input, DataType = typeof(int), DefaultValue = -1 },
            new NodePin { Id = "done", Label = "Concluído", Direction = PinDirection.Output, DataType = typeof(bool) }
        ]
    };
}

public class LeftClickExecutor : INodeExecutor
{
    public Task<NodeExecutionResult> ExecuteAsync(
        FlowNode node,
        IExecutionContext context,
        IReadOnlyDictionary<string, object?> resolvedInputs)
    {
        int x = PinValueReader.GetInt(resolvedInputs, node.PinValues, "x", -1);
        int y = PinValueReader.GetInt(resolvedInputs, node.PinValues, "y", -1);

        context.Log(PinValueReader.HasExplicitCoordinates(x, y)
            ? $"Clique esquerdo em ({x}, {y})"
            : "Clique esquerdo na posição atual do mouse");

        NativeInput.ClickLeft(x, y);
        return Task.FromResult(NodeExecutionResult.Success(new() { ["done"] = true }));
    }
}
