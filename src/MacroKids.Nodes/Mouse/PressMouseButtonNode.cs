using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MacroKids.Core.Interfaces;
using MacroKids.Core.Models;

namespace MacroKids.Nodes.Mouse;

public static class PressMouseButtonMetadata
{
    public static readonly NodeMetadata Instance = new()
    {
        TypeId = "mouse.press_button",
        Name = "Pressionar Botão Mouse",
        Description = "Pressiona o botão esquerdo ou direito do mouse e o mantém pressionado indefinidamente (para arrastes).",
        Category = NodeCategory.Mouse,
        IconKey = "Mouse",
        NodeVersion = new Version(1, 0, 0),
        Pins = [
            new NodePin { Id = "in", Label = "Entrada", Direction = PinDirection.Input, DataType = typeof(bool) },
            new NodePin { 
                Id = "button", 
                Label = "Botão", 
                Direction = PinDirection.Input, 
                DataType = typeof(string), 
                InputType = PinInputType.Dropdown, 
                Options = ["Left", "Right"], 
                DefaultValue = "Left" 
            },
            new NodePin { Id = "done", Label = "Concluído", Direction = PinDirection.Output, DataType = typeof(bool) }
        ]
    };
}

public class PressMouseButtonExecutor : INodeExecutor
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
        [FieldOffset(0)] public MOUSEINPUT mi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    private const int INPUT_MOUSE = 0;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;

    public Task<NodeExecutionResult> ExecuteAsync(
        FlowNode node,
        IExecutionContext context,
        IReadOnlyDictionary<string, object?> resolvedInputs)
    {
        string button = "Left";
        if (resolvedInputs.TryGetValue("button", out var btnVal) && btnVal is string b)
            button = b;
        else if (node.PinValues.TryGetValue("button", out var sb) && sb is string sbStr)
            button = sbStr;

        context.Log($"Pressionando (segurando) botão {button} do mouse...");

        uint downFlag = MOUSEEVENTF_LEFTDOWN;
        if (button.Trim().ToLowerInvariant() == "right")
        {
            downFlag = MOUSEEVENTF_RIGHTDOWN;
        }

        var input = new INPUT
        {
            type = INPUT_MOUSE,
            u = new InputUnion
            {
                mi = new MOUSEINPUT
                {
                    dx = 0,
                    dy = 0,
                    mouseData = 0,
                    dwFlags = downFlag,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));

        return Task.FromResult(NodeExecutionResult.Success(new() { ["done"] = true }));
    }
}
