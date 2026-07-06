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
            new NodePin { Id = "in",    Label = "In",        Direction = PinDirection.Input,  DataType = typeof(bool) },
            new NodePin { Id = "key",   Label = "Tecla",     Direction = PinDirection.Input,  DataType = typeof(string), DefaultValue = "A", InputType = PinInputType.KeyCapture },
            new NodePin { Id = "times", Label = "Vezes",     Direction = PinDirection.Input,  DataType = typeof(int),    DefaultValue = 1 },
            new NodePin { Id = "done",  Label = "Done",      Direction = PinDirection.Output, DataType = typeof(bool) }
        ]
    };
}

public class PressKeyExecutor : INodeExecutor
{
    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    private const int KEYEVENTF_KEYUP = 0x0002;

    public async Task<NodeExecutionResult> ExecuteAsync(
        FlowNode node,
        IExecutionContext context,
        IReadOnlyDictionary<string, object?> resolvedInputs)
    {
        string key = "A";

        if (resolvedInputs.TryGetValue("key", out var kVal) && kVal is string rk)
            key = rk;
        else if (node.PinValues.TryGetValue("key", out var sk) && sk is string skStr)
            key = skStr;

        int times = 1;
        object? rawTimes = null;
        if (resolvedInputs.TryGetValue("times", out var tVal))
            rawTimes = tVal;
        else if (node.PinValues.TryGetValue("times", out var st))
            rawTimes = st;

        if (rawTimes != null)
        {
            if (rawTimes is int iVal) times = iVal;
            else int.TryParse(rawTimes.ToString(), out times);
        }

        context.Log($"Pressionando tecla: {key} ({times} vez/vezes)");

        byte virtualKey = GetVirtualKeyCode(key);
        if (virtualKey != 0)
        {
            for (int i = 0; i < times; i++)
            {
                if (i > 0)
                    await Task.Delay(50); // delay curto entre pressionamentos consecutivos

                // Key Down
                keybd_event(virtualKey, 0, 0, UIntPtr.Zero);
                // Key Up
                keybd_event(virtualKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
        }

        return NodeExecutionResult.Success(new() { ["done"] = true });
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
            "CTRL" or "CONTROL" => 0x11,
            "SHIFT" => 0x10,
            "ALT" => 0x12,
            _ => 0
        };
    }
}
