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

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

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

        // Remove o clique final de parada (LeftClick/RightClick no final da lista)
        if (_recordedActions.Count > 0)
        {
            int lastIndex = _recordedActions.Count - 1;
            while (lastIndex >= 0)
            {
                var act = _recordedActions[lastIndex];
                if (act.Type == ActionType.LeftClick || act.Type == ActionType.RightClick)
                {
                    _recordedActions.RemoveAt(lastIndex);
                    // Se houver um movimento para esse clique logo antes, remove também
                    if (lastIndex - 1 >= 0 && _recordedActions[lastIndex - 1].Type == ActionType.Move)
                    {
                        _recordedActions.RemoveAt(lastIndex - 1);
                    }
                    break;
                }
                lastIndex--;
            }
        }

        return new List<RecordedAction>(_recordedActions);
    }

    private static async Task RecordLoopAsync(CancellationToken token)
    {
        DateTime lastActionTime = DateTime.UtcNow;
        
        // Window state
        string lastActiveWindowTitle = "";

        // Mouse states
        bool isLeftDown = false;
        POINT leftDownPos = new POINT();
        DateTime leftDownTime = DateTime.UtcNow;

        bool isRightDown = false;
        POINT rightDownPos = new POINT();
        DateTime rightDownTime = DateTime.UtcNow;

        // Keyboard states
        bool[] keyStates = new bool[256];
        DateTime[] keyPressTimes = new DateTime[256];

        // Aguarda meio segundo para evitar gravar o clique no próprio botão iniciar
        await Task.Delay(500, token);
        lastActionTime = DateTime.UtcNow;

        while (!token.IsCancellationRequested)
        {
            // 0. Monitora foco em nova janela baseando-se no título
            IntPtr currentActiveWindow = GetForegroundWindow();
            if (currentActiveWindow != IntPtr.Zero)
            {
                var sb = new System.Text.StringBuilder(256);
                if (GetWindowText(currentActiveWindow, sb, 256) > 0)
                {
                    string winTitle = sb.ToString();
                    if (winTitle != lastActiveWindowTitle &&
                        !winTitle.Contains("MacroKids", StringComparison.OrdinalIgnoreCase) && 
                        !string.IsNullOrWhiteSpace(winTitle))
                    {
                        var now = DateTime.UtcNow;
                        var delay = (int)(now - lastActionTime).TotalMilliseconds;
                        lastActionTime = now;

                        _recordedActions.Add(new RecordedAction(ActionType.KeyPress, 0, 0, delay, "WINDOW_FOCUS:" + winTitle));
                        lastActiveWindowTitle = winTitle;
                    }
                }
            }

            // 1. Monitora clique esquerdo do mouse (Down / Up / Drag)
            short leftState = GetAsyncKeyState(0x01);
            bool currentLeft = (leftState & 0x8000) != 0;
            if (currentLeft && !isLeftDown)
            {
                GetCursorPos(out leftDownPos);
                leftDownTime = DateTime.UtcNow;
            }
            else if (!currentLeft && isLeftDown)
            {
                GetCursorPos(out var leftUpPos);
                var now = DateTime.UtcNow;
                var duration = (int)(now - leftDownTime).TotalMilliseconds;
                var delay = (int)(leftDownTime - lastActionTime).TotalMilliseconds;
                lastActionTime = now;

                // Verifica se houve arraste significativo (> 15 pixels)
                int deltaX = Math.Abs(leftUpPos.X - leftDownPos.X);
                int deltaY = Math.Abs(leftUpPos.Y - leftDownPos.Y);
                if (deltaX > 15 || deltaY > 15)
                {
                    // Registra sequência de arrastar: Mover A -> Pressionar -> Mover B -> Soltar
                    _recordedActions.Add(new RecordedAction(ActionType.Move, leftDownPos.X, leftDownPos.Y, delay));
                    _recordedActions.Add(new RecordedAction(ActionType.LeftClick, leftDownPos.X, leftDownPos.Y, 50, "PRESS"));
                    
                    // Delay do arraste
                    _recordedActions.Add(new RecordedAction(ActionType.Move, leftUpPos.X, leftUpPos.Y, duration));
                    _recordedActions.Add(new RecordedAction(ActionType.LeftClick, leftUpPos.X, leftUpPos.Y, 50, "RELEASE"));
                }
                else
                {
                    // Clique simples
                    _recordedActions.Add(new RecordedAction(ActionType.LeftClick, leftDownPos.X, leftDownPos.Y, delay));
                }
            }
            isLeftDown = currentLeft;

            // 2. Monitora clique direito do mouse
            short rightState = GetAsyncKeyState(0x02);
            bool currentRight = (rightState & 0x8000) != 0;
            if (currentRight && !isRightDown)
            {
                GetCursorPos(out rightDownPos);
                rightDownTime = DateTime.UtcNow;
            }
            else if (!currentRight && isRightDown)
            {
                GetCursorPos(out var rightUpPos);
                var now = DateTime.UtcNow;
                var duration = (int)(now - rightDownTime).TotalMilliseconds;
                var delay = (int)(rightDownTime - lastActionTime).TotalMilliseconds;
                lastActionTime = now;

                int deltaX = Math.Abs(rightUpPos.X - rightDownPos.X);
                int deltaY = Math.Abs(rightUpPos.Y - rightDownPos.Y);
                if (deltaX > 15 || deltaY > 15)
                {
                    _recordedActions.Add(new RecordedAction(ActionType.Move, rightDownPos.X, rightDownPos.Y, delay));
                    _recordedActions.Add(new RecordedAction(ActionType.RightClick, rightDownPos.X, rightDownPos.Y, 50, "PRESS"));
                    
                    _recordedActions.Add(new RecordedAction(ActionType.Move, rightUpPos.X, rightUpPos.Y, duration));
                    _recordedActions.Add(new RecordedAction(ActionType.RightClick, rightUpPos.X, rightUpPos.Y, 50, "RELEASE"));
                }
                else
                {
                    _recordedActions.Add(new RecordedAction(ActionType.RightClick, rightDownPos.X, rightDownPos.Y, delay));
                }
            }
            isRightDown = currentRight;

            // 3. Monitora teclas de 0x08 (Backspace) até 0x90 (Tecla Z e outras)
            for (int vk = 0x08; vk <= 0x90; vk++)
            {
                // Ignora clicks de mouse
                if (vk == 0x01 || vk == 0x02) continue;

                short state = GetAsyncKeyState(vk);
                bool pressed = (state & 0x8000) != 0;

                if (pressed && !keyStates[vk])
                {
                    keyPressTimes[vk] = DateTime.UtcNow;
                }
                else if (!pressed && keyStates[vk])
                {
                    var now = DateTime.UtcNow;
                    var duration = (int)(now - keyPressTimes[vk]).TotalMilliseconds;
                    var delay = (int)(keyPressTimes[vk] - lastActionTime).TotalMilliseconds;
                    lastActionTime = now;

                    string keyName = GetKeyName(vk);
                    if (!string.IsNullOrEmpty(keyName))
                    {
                        if (duration >= 300)
                        {
                            // Se a tecla ficou segurada por mais de 300ms, salva a duracao em Y
                            _recordedActions.Add(new RecordedAction(ActionType.KeyPress, 0, duration, delay, keyName));
                        }
                        else
                        {
                            // Clique simples
                            _recordedActions.Add(new RecordedAction(ActionType.KeyPress, 0, 0, delay, keyName));
                        }
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
            0x2E => "Delete",
            _ => ""
        };
    }
}
