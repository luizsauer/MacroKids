using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using MacroKids.Core.Models;
using MacroKids.Core.Services;

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

    private static readonly HashSet<int> IgnoredVirtualKeys =
    [
        0x00, 0x01, 0x02, 0x04, 0x05, 0x06, 0x07 // mouse / reserved
    ];

    public static bool IsRecording { get; private set; }
    public static bool IsPaused { get; private set; }

    public static int ActionCount => _recordedActions.Count;

    public static void Start(bool clearExisting = true)
    {
        if (IsRecording) return;
        IsRecording = true;
        IsPaused = false;
        
        if (clearExisting)
        {
            _recordedActions.Clear();
        }

        _cts = new CancellationTokenSource();
 
        var token = _cts.Token;
        Task.Run(() => RecordLoopAsync(token), token);
    }

    public static void Pause()
    {
        IsPaused = true;
    }

    public static void Resume()
    {
        IsPaused = false;
    }

    public static List<RecordedAction> Stop()
    {
        if (!IsRecording) return [];
        IsRecording = false;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        if (_recordedActions.Count > 0)
        {
            int lastIndex = _recordedActions.Count - 1;
            while (lastIndex >= 0)
            {
                var act = _recordedActions[lastIndex];
                if (act.Type == ActionType.LeftClick || act.Type == ActionType.RightClick)
                {
                    _recordedActions.RemoveAt(lastIndex);
                    if (lastIndex - 1 >= 0 && _recordedActions[lastIndex - 1].Type == ActionType.Move)
                        _recordedActions.RemoveAt(lastIndex - 1);
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
        string lastActiveWindowTitle = "";

        bool isLeftDown = false;
        POINT leftDownPos = new POINT();
        DateTime leftDownTime = DateTime.UtcNow;

        bool isRightDown = false;
        POINT rightDownPos = new POINT();
        DateTime rightDownTime = DateTime.UtcNow;

        bool[] keyStates = new bool[256];
        DateTime[] keyPressTimes = new DateTime[256];

        await Task.Delay(500, token);
        lastActionTime = DateTime.UtcNow;

        while (!token.IsCancellationRequested)
        {
            if (IsPaused)
            {
                await Task.Delay(100, token);
                lastActionTime = DateTime.UtcNow; // Evita acumular delay gigante enquanto pausado!
                continue;
            }

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

                int deltaX = Math.Abs(leftUpPos.X - leftDownPos.X);
                int deltaY = Math.Abs(leftUpPos.Y - leftDownPos.Y);
                if (deltaX > 15 || deltaY > 15)
                {
                    _recordedActions.Add(new RecordedAction(ActionType.Move, leftDownPos.X, leftDownPos.Y, delay));
                    _recordedActions.Add(new RecordedAction(ActionType.LeftClick, leftDownPos.X, leftDownPos.Y, 50, "PRESS"));
                    _recordedActions.Add(new RecordedAction(ActionType.Move, leftUpPos.X, leftUpPos.Y, duration));
                    _recordedActions.Add(new RecordedAction(ActionType.LeftClick, leftUpPos.X, leftUpPos.Y, 50, "RELEASE"));
                }
                else
                {
                    _recordedActions.Add(new RecordedAction(ActionType.LeftClick, leftDownPos.X, leftDownPos.Y, delay));
                }
            }
            isLeftDown = currentLeft;

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

            for (int vk = 0x08; vk <= 0xFE; vk++)
            {
                if (IgnoredVirtualKeys.Contains(vk))
                    continue;

                short state = GetAsyncKeyState(vk);
                bool pressed = (state & 0x8000) != 0;

                if (pressed && !keyStates[vk])
                    keyPressTimes[vk] = DateTime.UtcNow;
                else if (!pressed && keyStates[vk])
                {
                    var now = DateTime.UtcNow;
                    var duration = (int)(now - keyPressTimes[vk]).TotalMilliseconds;
                    var delay = (int)(keyPressTimes[vk] - lastActionTime).TotalMilliseconds;
                    lastActionTime = now;

                    string keyName = KeyboardMapper.GetKeyName((byte)vk);
                    if (string.IsNullOrEmpty(keyName))
                        continue;

                    int holdMs = duration >= KeyboardMapper.HoldThresholdMs ? duration : 0;
                    _recordedActions.Add(new RecordedAction(ActionType.KeyPress, 0, holdMs, delay, keyName));
                }

                keyStates[vk] = pressed;
            }

            await Task.Delay(15, token);
        }
    }
}
