using System.Runtime.InteropServices;
using MacroKids.Core.Services;

namespace MacroKids.Core.Interop;

/// <summary>
/// Centraliza chamadas Win32 de mouse/teclado com fallbacks para desktop e jogos.
/// </summary>
public static class NativeInput
{
    private const int InputMouse = 0;
    private const int InputKeyboard = 1;

    private const uint MouseEventFMove = 0x0001;
    private const uint MouseEventFLeftDown = 0x0002;
    private const uint MouseEventFLeftUp = 0x0004;
    private const uint MouseEventFRightDown = 0x0008;
    private const uint MouseEventFRightUp = 0x0010;
    private const uint MouseEventFAbsolute = 0x8000;
    private const uint MouseEventFVirtualDesktop = 0x4000;

    private const uint KeyEventFExtendedKey = 0x0001;
    private const uint KeyEventFKeyUp = 0x0002;
    private const uint KeyEventFScanCode = 0x0008;
    private const uint KeyEventFUnicode = 0x0004;

    private const int SmXVirtualScreen = 76;
    private const int SmYVirtualScreen = 77;
    private const int SmCxVirtualScreen = 78;
    private const int SmCyVirtualScreen = 79;

    private static readonly int InputStructSize = Marshal.SizeOf<INPUT>();

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
        [FieldOffset(0)] public KEYBDINPUT ki;
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
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
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
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);

    public static void MoveMouse(int x, int y)
    {
        SetCursorPos(x, y);

        ToAbsolute(x, y, out int absX, out int absY);
        var move = new INPUT
        {
            type = InputMouse,
            u = new InputUnion
            {
                mi = new MOUSEINPUT
                {
                    dx = absX,
                    dy = absY,
                    dwFlags = MouseEventFMove | MouseEventFAbsolute | MouseEventFVirtualDesktop,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        if (SendInput(1, [move], InputStructSize) == 0)
            mouse_event(MouseEventFMove | MouseEventFAbsolute, (uint)absX, (uint)absY, 0, UIntPtr.Zero);
    }

    public static void ClickLeft(int x = -1, int y = -1)
    {
        Click(x, y, left: true);
    }

    public static void ClickRight(int x = -1, int y = -1)
    {
        Click(x, y, left: false);
    }

    private static void Click(int x, int y, bool left)
    {
        // -1,-1 = posição atual; coordenadas negativas são válidas (multi-monitor)
        bool hasCoords = PinValueReader.HasExplicitCoordinates(x, y);
        if (hasCoords)
        {
            MoveMouse(x, y);
            Thread.Sleep(80);
        }

        GetCursorPos(out var cursor);
        int targetX = hasCoords ? x : cursor.X;
        int targetY = hasCoords ? y : cursor.Y;

        uint downFlag = left ? MouseEventFLeftDown : MouseEventFRightDown;
        uint upFlag = left ? MouseEventFLeftUp : MouseEventFRightUp;

        // 1) Clique absoluto — necessário para jogos (Roblox)
        ToAbsolute(targetX, targetY, out int absX, out int absY);
        uint sentDown = SendAbsoluteMouse(downFlag, absX, absY);
        Thread.Sleep(40);
        uint sentUp = SendAbsoluteMouse(upFlag, absX, absY);

        if (sentDown > 0 && sentUp > 0)
            return;

        // 2) Clique relativo — desktop (Bloco de Notas)
        var relDown = new INPUT
        {
            type = InputMouse,
            u = new InputUnion
            {
                mi = new MOUSEINPUT
                {
                    dx = 0,
                    dy = 0,
                    dwFlags = downFlag,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };
        var relUp = new INPUT
        {
            type = InputMouse,
            u = new InputUnion
            {
                mi = new MOUSEINPUT
                {
                    dx = 0,
                    dy = 0,
                    dwFlags = upFlag,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        if (SendInput(1, [relDown], InputStructSize) > 0 &&
            SendInput(1, [relUp], InputStructSize) > 0)
            return;

        // 3) Fallback legado
        mouse_event(downFlag, 0, 0, 0, UIntPtr.Zero);
        Thread.Sleep(40);
        mouse_event(upFlag, 0, 0, 0, UIntPtr.Zero);
    }

    public static void PressKey(byte virtualKey)
    {
        KeyDown(virtualKey);
        Thread.Sleep(30);
        KeyUp(virtualKey);
    }

    public static void KeyDown(byte virtualKey)
    {
        ushort scanCode = (ushort)MapVirtualKey(virtualKey, 0);
        bool isExtended = (virtualKey >= 0x21 && virtualKey <= 0x2F) || (virtualKey >= 0x25 && virtualKey <= 0x28);

        uint flags = KeyEventFScanCode | (isExtended ? KeyEventFExtendedKey : 0);
        var input = new INPUT
        {
            type = InputKeyboard,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = scanCode,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        if (SendInput(1, [input], InputStructSize) > 0)
            return;

        flags = isExtended ? KeyEventFExtendedKey : 0;
        var vkInput = new INPUT
        {
            type = InputKeyboard,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = virtualKey,
                    wScan = scanCode,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        if (SendInput(1, [vkInput], InputStructSize) == 0)
            keybd_event(virtualKey, 0, 0, UIntPtr.Zero);
    }

    public static void KeyUp(byte virtualKey)
    {
        ushort scanCode = (ushort)MapVirtualKey(virtualKey, 0);
        bool isExtended = (virtualKey >= 0x21 && virtualKey <= 0x2F) || (virtualKey >= 0x25 && virtualKey <= 0x28);

        uint flags = KeyEventFScanCode | KeyEventFKeyUp | (isExtended ? KeyEventFExtendedKey : 0);
        var input = new INPUT
        {
            type = InputKeyboard,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = scanCode,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        if (SendInput(1, [input], InputStructSize) > 0)
            return;

        flags = KeyEventFKeyUp | (isExtended ? KeyEventFExtendedKey : 0);
        var vkInput = new INPUT
        {
            type = InputKeyboard,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = virtualKey,
                    wScan = scanCode,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        if (SendInput(1, [vkInput], InputStructSize) == 0)
            keybd_event(virtualKey, 0, KeyEventFKeyUp, UIntPtr.Zero);
    }

    public static void TypeUnicodeChar(char c)
    {
        var down = new INPUT
        {
            type = InputKeyboard,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = c,
                    dwFlags = KeyEventFUnicode,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };
        var up = new INPUT
        {
            type = InputKeyboard,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = c,
                    dwFlags = KeyEventFUnicode | KeyEventFKeyUp,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        if (SendInput(1, [down], InputStructSize) == 0)
            return;

        SendInput(1, [up], InputStructSize);
    }

    private static uint SendAbsoluteMouse(uint flags, int absX, int absY)
    {
        var input = new INPUT
        {
            type = InputMouse,
            u = new InputUnion
            {
                mi = new MOUSEINPUT
                {
                    dx = absX,
                    dy = absY,
                    mouseData = 0,
                    dwFlags = flags | MouseEventFAbsolute | MouseEventFVirtualDesktop,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        return SendInput(1, [input], InputStructSize);
    }

    private static void ToAbsolute(int x, int y, out int absX, out int absY)
    {
        int virtualLeft = GetSystemMetrics(SmXVirtualScreen);
        int virtualTop = GetSystemMetrics(SmYVirtualScreen);
        int virtualWidth = GetSystemMetrics(SmCxVirtualScreen);
        int virtualHeight = GetSystemMetrics(SmCyVirtualScreen);

        if (virtualWidth <= 0) virtualWidth = GetSystemMetrics(0);
        if (virtualHeight <= 0) virtualHeight = GetSystemMetrics(1);

        absX = ((x - virtualLeft) * 65536) / virtualWidth;
        absY = ((y - virtualTop) * 65536) / virtualHeight;
    }
}
