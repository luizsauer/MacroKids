using System.Collections.Generic;
using System.Threading.Tasks;
using MacroKids.Core.Interfaces;
using MacroKids.Core.Interop;
using MacroKids.Core.Models;
using MacroKids.Core.Services;

namespace MacroKids.Nodes.Keyboard;

public static class PressKeyMetadata
{
    public static readonly NodeMetadata Instance = new()
    {
        TypeId = "keyboard.press_key",
        Name = "Pressionar Tecla",
        Description = "Pressiona e solta uma tecla no teclado do computador.",
        Category = NodeCategory.Keyboard,
        IconKey = "Keyboard",
        NodeVersion = new Version(1, 2, 0),
        Pins = [
            new NodePin { Id = "in",    Label = "In",        Direction = PinDirection.Input,  DataType = typeof(bool) },
            new NodePin { Id = "key",   Label = "Tecla",     Direction = PinDirection.Input,  DataType = typeof(string), DefaultValue = "A", InputType = PinInputType.KeyCapture },
            new NodePin { Id = "times", Label = "Vezes",     Direction = PinDirection.Input,  DataType = typeof(int),    DefaultValue = 1 },
            new NodePin { Id = "done",  Label = "Done",      Direction = PinDirection.Output, DataType = typeof(bool) }
        ]
    };
}

public class PressKeyExecutor : INodeExecutor
{
    public async Task<NodeExecutionResult> ExecuteAsync(
        FlowNode node,
        IExecutionContext context,
        IReadOnlyDictionary<string, object?> resolvedInputs)
    {
        string key = "Enter";
        if (resolvedInputs.TryGetValue("key", out var kVal) && kVal is string rk)
            key = rk;
        else if (node.PinValues.TryGetValue("key", out var sk) && sk is string skStr)
            key = skStr;

        int times = PinValueReader.GetInt(resolvedInputs, node.PinValues, "times", 1);
        context.Log($"Pressionando tecla: {key} ({times} vez/vezes)");

        byte virtualKey = KeyboardMapper.GetVirtualKeyCode(key);
        if (virtualKey != 0)
        {
            for (int i = 0; i < times; i++)
            {
                if (i > 0)
                    await Task.Delay(50);

                NativeInput.PressKey(virtualKey);
            }
        }

        return NodeExecutionResult.Success(new() { ["done"] = true });
    }
}
