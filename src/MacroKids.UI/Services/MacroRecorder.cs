using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using MacroKids.Core.Models;

namespace MacroKids.UI.Services;

public static class MacroRecorder
{
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    private static CancellationTokenSource? _cts;
    private static readonly List<RecordedAction> _recordedActions = [];

    public static bool IsRecording { get; private set; }

    public static void Start()
    {
        if (IsRecording) return;
        IsRecording = true;
        _recordedActions.Clear();
        _cts = new CancellationTokenSource();

        var token = _cts.Token;
        Task.Run(() => RecordLoopAsync(token), token);
    }

    public static List<RecordedAction> Stop()
    {
        if (!IsRecording) return [];
        IsRecording = false;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        return new List<RecordedAction>(_recordedActions);
    }

    private static async Task RecordLoopAsync(CancellationToken token)
    {
        DateTime lastActionTime = DateTime.UtcNow;
        bool isLeftDown = false;
        bool isRightDown = false;
        bool[] keyStates = new bool[256];

        // Aguarda meio segundo para evitar gravar o clique no próprio botão iniciar
        await Task.Delay(500, token);
        lastActionTime = DateTime.UtcNow;

        while (!token.IsCancellationRequested)
        {
            // 1. Monitora clique esquerdo do mouse
            short leftState = GetAsyncKeyState(0x01);
            bool currentLeft = (leftState & 0x8000) != 0;
            if (currentLeft && !isLeftDown)
            {
                GetCursorPos(out var p);
                var now = DateTime.UtcNow;
                var delay = (int)(now - lastActionTime).TotalMilliseconds;
                lastActionTime = now;

                _recordedActions.Add(new RecordedAction(ActionType.LeftClick, p.X, p.Y, delay));
            }
            isLeftDown = currentLeft;

            // 2. Monitora clique direito do mouse
            short rightState = GetAsyncKeyState(0x02);
            bool currentRight = (rightState & 0x8000) != 0;
            if (currentRight && !isRightDown)
            {
                GetCursorPos(out var p);
                var now = DateTime.UtcNow;
                var delay = (int)(now - lastActionTime).TotalMilliseconds;
                lastActionTime = now;

                _recordedActions.Add(new RecordedAction(ActionType.RightClick, p.X, p.Y, delay));
            }
            isRightDown = currentRight;

            // 3. Monitora teclas de 0x08 (Backspace) até 0x90
            for (int vk = 0x08; vk <= 0x90; vk++)
            {
                // Ignora clicks de mouse
                if (vk == 0x01 || vk == 0x02) continue;

                short state = GetAsyncKeyState(vk);
                bool pressed = (state & 0x8000) != 0;

                if (pressed && !keyStates[vk])
                {
                    var now = DateTime.UtcNow;
                    var delay = (int)(now - lastActionTime).TotalMilliseconds;
                    lastActionTime = now;

                    string keyName = GetKeyName(vk);
                    if (!string.IsNullOrEmpty(keyName))
                    {
                        _recordedActions.Add(new RecordedAction(ActionType.KeyPress, 0, 0, delay, keyName));
                    }
                }
                keyStates[vk] = pressed;
            }

            await Task.Delay(15, token);
        }
    }

    private static string GetKeyName(int vk)
    {
        if (vk >= 0x30 && vk <= 0x39) return ((char)vk).ToString(); // 0-9
        if (vk >= 0x41 && vk <= 0x5A) return ((char)vk).ToString(); // A-Z
        return vk switch
        {
            0x0D => "Enter",
            0x20 => "Space",
            0x08 => "Backspace",
            0x09 => "Tab",
            0x1B => "Esc",
            0x25 => "Left",
            0x26 => "Up",
            0x27 => "Right",
            0x28 => "Down",
            _ => ""
        };
    }
}
