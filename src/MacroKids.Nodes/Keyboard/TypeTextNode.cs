using System.Runtime.InteropServices;
using MacroKids.Core.Interfaces;
using MacroKids.Core.Models;

namespace MacroKids.Nodes.Keyboard;

/// <summary>
/// Metadata definition for Type Text block.
/// </summary>
public static class TypeTextMetadata
{
    public static readonly NodeMetadata Instance = new()
    {
        TypeId = "keyboard.type_text",
        Name = "Digitar Texto",
        Description = "Digita uma frase ou palavra inteira simulando a digitação no teclado.",
        Category = NodeCategory.Keyboard,
        IconKey = "Keyboard",
        NodeVersion = new Version(1, 0, 0),
        Pins = [
            new NodePin { Id = "in", Label = "Entrada", Direction = PinDirection.Input, DataType = typeof(bool) },
            new NodePin { Id = "text", Label = "Texto para digitar", Direction = PinDirection.Input, DataType = typeof(string), DefaultValue = "Olá" },
            new NodePin { Id = "done", Label = "Concluído", Direction = PinDirection.Output, DataType = typeof(bool) }
        ]
    };
}

public class TypeTextExecutor : INodeExecutor
{
    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern short VkKeyScan(char ch);

    private const int KEYEVENTF_KEYUP = 0x0002;

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
            short vkAndShift = VkKeyScan(c);
            byte vk = (byte)(vkAndShift & 0xff);
            bool shift = (vkAndShift & 0x100) > 0;

            if (shift)
            {
                keybd_event(0x10, 0, 0, UIntPtr.Zero); // SHIFT down
            }

            keybd_event(vk, 0, 0, UIntPtr.Zero); // Key down
            keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // Key up

            if (shift)
            {
                keybd_event(0x10, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // SHIFT up
            }

            // Small delay between key strokes to simulate realistic typing
            await Task.Delay(30);
        }

        return NodeExecutionResult.Success(new() { ["done"] = true });
    }
}
