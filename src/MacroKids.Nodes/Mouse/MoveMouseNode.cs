using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MacroKids.Core.Interfaces;
using MacroKids.Core.Models;

namespace MacroKids.Nodes.Mouse;

public static class MoveMouseMetadata
{
    public static readonly NodeMetadata Instance = new()
    {
        TypeId = "mouse.move",
        Name = "Mover Mouse",
        Description = "Move o cursor do mouse para a coordenada (X, Y) na tela (suporta múltiplos monitores).",
        Category = NodeCategory.Mouse,
        IconKey = "Mouse",
        NodeVersion = new Version(1, 0, 0),
        Pins = [
            new NodePin { Id = "in", Label = "Entrada", Direction = PinDirection.Input, DataType = typeof(bool) },
            new NodePin { Id = "x", Label = "Posição X", Direction = PinDirection.Input, DataType = typeof(int), DefaultValue = 0 },
            new NodePin { Id = "y", Label = "Posição Y", Direction = PinDirection.Input, DataType = typeof(int), DefaultValue = 0 },
            new NodePin { Id = "done", Label = "Concluído", Direction = PinDirection.Output, DataType = typeof(bool) }
        ]
    };
}

public class MoveMouseExecutor : INodeExecutor
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

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    private const int INPUT_MOUSE = 0;
    private const uint MOUSEEVENTF_MOVE = 0x0001;
    private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
    private const uint MOUSEEVENTF_VIRTUALDESKTOP = 0x4000;

    // Métricas para múltiplos monitores (Virtual Screen)
    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;

    public Task<NodeExecutionResult> ExecuteAsync(
        FlowNode node,
        IExecutionContext context,
        IReadOnlyDictionary<string, object?> resolvedInputs)
    {
        int x = 0;
        int y = 0;

        if (resolvedInputs.TryGetValue("x", out var xVal) && xVal is int rx)
            x = rx;
        else if (node.PinValues.TryGetValue("x", out var sx) && sx is int sxInt)
            x = sxInt;

        if (resolvedInputs.TryGetValue("y", out var yVal) && yVal is int ry)
            y = ry;
        else if (node.PinValues.TryGetValue("y", out var sy) && sy is int syInt)
            y = syInt;

        context.Log($"Movendo cursor do mouse para ({x}, {y})");

        // 1. Move via API de cursor do Windows para garantir suporte a UI/Janelas clássicas
        SetCursorPos(x, y);

        // 2. Converte para coordenadas absolutas virtuais da tela completa (múltiplos monitores)
        int virtualLeft = GetSystemMetrics(SM_XVIRTUALSCREEN);
        int virtualTop = GetSystemMetrics(SM_YVIRTUALSCREEN);
        int virtualWidth = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        int virtualHeight = GetSystemMetrics(SM_CYVIRTUALSCREEN);

        // Se falhar ou retornar 0, usa fallback para monitor principal
        if (virtualWidth <= 0) virtualWidth = GetSystemMetrics(0); // SM_CXSCREEN
        if (virtualHeight <= 0) virtualHeight = GetSystemMetrics(1); // SM_CYSCREEN

        int absoluteX = ((x - virtualLeft) * 65536) / virtualWidth;
        int absoluteY = ((y - virtualTop) * 65536) / virtualHeight;

        // 3. Move via SendInput para compatibilidade a nível de driver físico com jogos 3D
        var input = new INPUT
        {
            type = INPUT_MOUSE,
            u = new InputUnion
            {
                mi = new MOUSEINPUT
                {
                    dx = absoluteX,
                    dy = absoluteY,
                    mouseData = 0,
                    dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESKTOP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));

        return Task.FromResult(NodeExecutionResult.Success(new() { ["done"] = true }));
    }
}
