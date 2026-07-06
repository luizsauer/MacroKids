using System.Collections.Generic;
using System.Threading.Tasks;
using MacroKids.Core.Interfaces;
using MacroKids.Core.Interop;
using MacroKids.Core.Models;

namespace MacroKids.Nodes.Keyboard;

public static class TypeTextMetadata
{
    public static readonly NodeMetadata Instance = new()
    {
        TypeId = "keyboard.type_text",
        Name = "Digitar Texto",
        Description = "Digita uma frase ou palavra inteira simulando a digitação no teclado.",
        Category = NodeCategory.Keyboard,
        IconKey = "Keyboard",
        NodeVersion = new Version(1, 1, 0),
        Pins = [
            new NodePin { Id = "in", Label = "Entrada", Direction = PinDirection.Input, DataType = typeof(bool) },
            new NodePin { Id = "text", Label = "Texto para digitar", Direction = PinDirection.Input, DataType = typeof(string), DefaultValue = "Olá" },
            new NodePin { Id = "done", Label = "Concluído", Direction = PinDirection.Output, DataType = typeof(bool) }
        ]
    };
}

public class TypeTextExecutor : INodeExecutor
{
    public async Task<NodeExecutionResult> ExecuteAsync(
        FlowNode node,
        IExecutionContext context,
        IReadOnlyDictionary<string, object?> resolvedInputs)
    {
        string text = "Olá";

        if (resolvedInputs.TryGetValue("text", out var tVal) && tVal is string rt)
            text = rt;
        else if (node.PinValues.TryGetValue("text", out var st) && st is string stStr)
            text = stStr;

        context.Log($"Digitando texto: {text}");

        foreach (char c in text)
        {
            NativeInput.TypeUnicodeChar(c);
            await Task.Delay(40);
        }

        return NodeExecutionResult.Success(new() { ["done"] = true });
    }
}
