using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MacroKids.Core.Interfaces;
using MacroKids.Core.Models;

namespace MacroKids.Nodes.Mouse;

public static class LeftClickMetadata
{
    public static readonly NodeMetadata Instance = new()
    {
        TypeId = "mouse.left_click",
        Name = "Clique Esquerdo",
        Description = "Executa um clique com o botão esquerdo na coordenada (X, Y) ou na posição atual se for -1.",
        Category = NodeCategory.Mouse,
        IconKey = "Mouse",
        NodeVersion = new Version(1, 1, 0),
        Pins = [
            new NodePin { Id = "in", Label = "Entrada", Direction = PinDirection.Input, DataType = typeof(bool) },
            new NodePin { Id = "x", Label = "Posição X", Direction = PinDirection.Input, DataType = typeof(int), DefaultValue = -1 },
            new NodePin { Id = "y", Label = "Posição Y", Direction = PinDirection.Input, DataType = typeof(int), DefaultValue = -1 },
            new NodePin { Id = "done", Label = "Concluído", Direction = PinDirection.Output, DataType = typeof(bool) }
        ]
    };
}

public class LeftClickExecutor : INodeExecutor
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

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    private const int INPUT_MOUSE = 0;
    private const uint MOUSEEVENTF_MOVE = 0x0001;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
    private const uint MOUSEEVENTF_VIRTUALDESKTOP = 0x4000;

    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;

    public async Task<NodeExecutionResult> ExecuteAsync(
        FlowNode node,
        IExecutionContext context,
        IReadOnlyDictionary<string, object?> resolvedInputs)
    {
        int x = -1;
        if (resolvedInputs.TryGetValue("x", out var xVal) && xVal is int rx)
            x = rx;
        else if (node.PinValues.TryGetValue("x", out var sx) && sx is int sxInt)
            x = sxInt;

        int y = -1;
        if (resolvedInputs.TryGetValue("y", out var yVal) && yVal is int ry)
            y = ry;
        else if (node.PinValues.TryGetValue("y", out var sy) && sy is int syInt)
            y = syInt;

        int targetX = x;
        int targetY = y;

        // Se uma coordenada válida foi passada, move o mouse primeiro
        if (x >= 0 && y >= 0)
        {
            context.Log($"Movendo mouse para ({x}, {y}) antes de clicar...");
            SetCursorPos(x, y);

            // Mapeamento absoluto virtual para o movimento por SendInput (compatível com jogos 3D)
            int virtualLeft = GetSystemMetrics(SM_XVIRTUALSCREEN);
            int virtualTop = GetSystemMetrics(SM_YVIRTUALSCREEN);
            int virtualWidth = GetSystemMetrics(SM_CXVIRTUALSCREEN);
            int virtualHeight = GetSystemMetrics(SM_CYVIRTUALSCREEN);
            if (virtualWidth <= 0) virtualWidth = GetSystemMetrics(0);
            if (virtualHeight <= 0) virtualHeight = GetSystemMetrics(1);

            int absX = ((x - virtualLeft) * 65536) / virtualWidth;
            int absY = ((y - virtualTop) * 65536) / virtualHeight;

            var moveInput = new INPUT
            {
                type = INPUT_MOUSE,
                u = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        dx = absX,
                        dy = absY,
                        dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESKTOP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
            SendInput(1, new[] { moveInput }, Marshal.SizeOf(typeof(INPUT)));

            // Delay estratégico (hover delay) de 100ms para permitir que a engine do jogo processe a transição da coordenada!
            await Task.Delay(100);
        }
        else
        {
            // Posição atual do mouse
            GetCursorPos(out var p);
            targetX = p.X;
            targetY = p.Y;
        }

        context.Log($"Executando clique esquerdo em ({targetX}, {targetY})");

        int vL = GetSystemMetrics(SM_XVIRTUALSCREEN);
        int vT = GetSystemMetrics(SM_YVIRTUALSCREEN);
        int vW = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        int vH = GetSystemMetrics(SM_CYVIRTUALSCREEN);
        if (vW <= 0) vW = GetSystemMetrics(0);
        if (vH <= 0) vH = GetSystemMetrics(1);

        int absoluteX = ((targetX - vL) * 65536) / vW;
        int absoluteY = ((targetY - vT) * 65536) / vH;

        var inputDown = new INPUT
        {
            type = INPUT_MOUSE,
            u = new InputUnion
            {
                mi = new MOUSEINPUT
                {
                    dx = absoluteX,
                    dy = absoluteY,
                    mouseData = 0,
                    dwFlags = MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESKTOP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        var inputUp = new INPUT
        {
            type = INPUT_MOUSE,
            u = new InputUnion
            {
                mi = new MOUSEINPUT
                {
                    dx = absoluteX,
                    dy = absoluteY,
                    mouseData = 0,
                    dwFlags = MOUSEEVENTF_LEFTUP | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESKTOP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        SendInput(1, new[] { inputDown }, Marshal.SizeOf(typeof(INPUT)));
        await Task.Delay(40);
        SendInput(1, new[] { inputUp }, Marshal.SizeOf(typeof(INPUT)));

        return NodeExecutionResult.Success(new() { ["done"] = true });
    }
}
