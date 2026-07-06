using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MacroKids.Core.Interfaces;
using MacroKids.Core.Models;

namespace MacroKids.Nodes.Mouse;

public static class ReleaseMouseButtonMetadata
{
    public static readonly NodeMetadata Instance = new()
    {
        TypeId = "mouse.release_button",
        Name = "Soltar Botão Mouse",
        Description = "Solta o botão esquerdo ou direito do mouse que foi previamente pressionado.",
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

public class ReleaseMouseButtonExecutor : INodeExecutor
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
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;

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

        context.Log($"Soltando botão {button} do mouse...");

        uint upFlag = MOUSEEVENTF_LEFTUP;
        if (button.Trim().ToLowerInvariant() == "right")
        {
            upFlag = MOUSEEVENTF_RIGHTUP;
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
                    dwFlags = upFlag,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));

        return Task.FromResult(NodeExecutionResult.Success(new() { ["done"] = true }));
    }
}
