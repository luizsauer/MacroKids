using System.Runtime.InteropServices;
using MacroKids.Core.Interfaces;
using MacroKids.Core.Models;

namespace MacroKids.Nodes.Keyboard;

/// <summary>
/// Metadata definition for Press Key block.
/// </summary>
public static class PressKeyMetadata
{
    public static readonly NodeMetadata Instance = new()
    {
        TypeId = "keyboard.press_key",
        Name = "Pressionar Tecla",
        Description = "Pressiona e solta uma tecla no teclado do computador.",
        Category = NodeCategory.Keyboard,
        IconKey = "Keyboard",
        NodeVersion = new Version(1, 0, 0),
        Pins = [
            new NodePin { Id = "key", Label = "Tecla (Ex: A, Enter)", Direction = PinDirection.Input, DataType = typeof(string), DefaultValue = "A" },
            new NodePin { Id = "done", Label = "Concluído", Direction = PinDirection.Output, DataType = typeof(bool) }
        ]
    };
}

public class PressKeyExecutor : INodeExecutor
{
    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    private const int KEYEVENTF_KEYUP = 0x0002;

    public Task<NodeExecutionResult> ExecuteAsync(
        FlowNode node,
        IExecutionContext context,
        IReadOnlyDictionary<string, object?> resolvedInputs)
    {
        string key = "A";

        if (resolvedInputs.TryGetValue("key", out var kVal) && kVal is string rk)
            key = rk;
        else if (node.PinValues.TryGetValue("key", out var sk) && sk is string skStr)
            key = skStr;

        context.Log($"Pressionando tecla: {key}");

        byte virtualKey = GetVirtualKeyCode(key);
        if (virtualKey != 0)
        {
            // Key Down
            keybd_event(virtualKey, 0, 0, UIntPtr.Zero);
            // Key Up
            keybd_event(virtualKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        return Task.FromResult(NodeExecutionResult.Success(new() { ["done"] = true }));
    }

    private static byte GetVirtualKeyCode(string key)
    {
        if (string.IsNullOrEmpty(key)) return 0;

        string cleanKey = key.Trim().ToUpperInvariant();
        if (cleanKey.Length == 1)
        {
            return (byte)cleanKey[0]; // Retorna o ASCII da letra/número
        }

        return cleanKey switch
        {
            "ENTER" => 0x0D,
            "SPACE" or "ESPAÇO" => 0x20,
            "BACKSPACE" => 0x08,
            "TAB" => 0x09,
            "ESCAPE" or "ESC" => 0x1B,
            "UP" or "CIMA" => 0x26,
            "DOWN" or "BAIXO" => 0x28,
            "LEFT" or "ESQUERDA" => 0x25,
            "RIGHT" or "DIREITA" => 0x27,
            _ => 0
        };
    }
}
