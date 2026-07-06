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
    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);

    private const int INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
    private const uint KEYEVENTF_KEYUP = 0x0002;
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

        byte virtualKey = MacroKids.Core.Services.KeyboardMapper.GetVirtualKeyCode(key);
        if (virtualKey != 0)
        {
            ushort scanCode = (ushort)MapVirtualKey(virtualKey, 0);

            bool isExtended = (virtualKey >= 0x21 && virtualKey <= 0x2F) || (virtualKey >= 0x25 && virtualKey <= 0x28);
            uint downFlags = isExtended ? KEYEVENTF_EXTENDEDKEY : 0;
            uint upFlags = KEYEVENTF_KEYUP | (isExtended ? KEYEVENTF_EXTENDEDKEY : 0);

            for (int i = 0; i < times; i++)
            {
                if (i > 0)
                    await Task.Delay(50); // delay curto entre pressionamentos consecutivos

                // Key Down
                var inputDown = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = (ushort)virtualKey,
                            wScan = scanCode,
                            dwFlags = downFlags,
                            time = 0,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                };
                SendInput(1, new[] { inputDown }, Marshal.SizeOf(typeof(INPUT)));

                // Pequeno atraso para simular o pressionamento real
                await Task.Delay(30);

                // Key Up
                var inputUp = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = (ushort)virtualKey,
                            wScan = scanCode,
                            dwFlags = upFlags,
                            time = 0,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                };
                SendInput(1, new[] { inputUp }, Marshal.SizeOf(typeof(INPUT)));
            }
        }

        return NodeExecutionResult.Success(new() { ["done"] = true });
    }
}
