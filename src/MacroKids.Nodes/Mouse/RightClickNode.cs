using System.Runtime.InteropServices;
using MacroKids.Core.Interfaces;
using MacroKids.Core.Models;

namespace MacroKids.Nodes.Mouse;

/// <summary>
/// Metadata definition for Right Click block.
/// </summary>
public static class RightClickMetadata
{
    public static readonly NodeMetadata Instance = new()
    {
        TypeId = "mouse.right_click",
        Name = "Clique Direito",
        Description = "Executa um clique com o botão direito na posição atual do cursor.",
        Category = NodeCategory.Mouse,
        IconKey = "Mouse",
        NodeVersion = new Version(1, 0, 0),
        Pins = [
            new NodePin { Id = "in", Label = "Entrada", Direction = PinDirection.Input, DataType = typeof(bool) },
            new NodePin { Id = "done", Label = "Concluído", Direction = PinDirection.Output, DataType = typeof(bool) }
        ]
    };
}

public class RightClickExecutor : INodeExecutor
{
    [DllImport("user32.dll")]
    private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

    private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
    private const int MOUSEEVENTF_RIGHTUP = 0x10;

    public Task<NodeExecutionResult> ExecuteAsync(
        FlowNode node,
        IExecutionContext context,
        IReadOnlyDictionary<string, object?> resolvedInputs)
    {
        context.Log("Executando clique direito do mouse");
        
        // Simulates down then up native mouse event
        mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
        mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);

        return Task.FromResult(NodeExecutionResult.Success(new() { ["done"] = true }));
    }
}
