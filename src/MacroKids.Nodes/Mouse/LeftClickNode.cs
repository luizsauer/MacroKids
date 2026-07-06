using System.Runtime.InteropServices;
using MacroKids.Core.Interfaces;
using MacroKids.Core.Models;

namespace MacroKids.Nodes.Mouse;

/// <summary>
/// Metadata definition for Left Click block.
/// </summary>
public static class LeftClickMetadata
{
    public static readonly NodeMetadata Instance = new()
    {
        TypeId = "mouse.left_click",
        Name = "Clique Esquerdo",
        Description = "Executa um clique com o botão esquerdo na posição atual do cursor.",
        Category = NodeCategory.Mouse,
        IconKey = "Mouse",
        NodeVersion = new Version(1, 0, 0),
        Pins = [
            new NodePin { Id = "in", Label = "Entrada", Direction = PinDirection.Input, DataType = typeof(bool) },
            new NodePin { Id = "done", Label = "Concluído", Direction = PinDirection.Output, DataType = typeof(bool) }
        ]
    };
}

public class LeftClickExecutor : INodeExecutor
{
    [DllImport("user32.dll")]
    private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

    private const int MOUSEEVENTF_LEFTDOWN = 0x02;
    private const int MOUSEEVENTF_LEFTUP = 0x04;

    public Task<NodeExecutionResult> ExecuteAsync(
        FlowNode node,
        IExecutionContext context,
        IReadOnlyDictionary<string, object?> resolvedInputs)
    {
        context.Log("Executando clique esquerdo do mouse");
        
        // Simulates down then up native mouse event
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);

        return Task.FromResult(NodeExecutionResult.Success(new() { ["done"] = true }));
    }
}
