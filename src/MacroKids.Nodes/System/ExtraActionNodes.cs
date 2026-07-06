using System.Runtime.InteropServices;
using MacroKids.Core.Interfaces;
using MacroKids.Core.Models;

namespace MacroKids.Nodes.System;

public static class MouseScrollMetadata
{
    public static readonly NodeMetadata Instance = new()
    {
        TypeId = "mouse.scroll",
        Name = "Scroll",
        Description = "Rola a roda do mouse para cima ou para baixo.",
        Category = NodeCategory.Mouse,
        IconKey = "Mouse",
        NodeVersion = new Version(1, 0, 0),
        Pins = [
            new NodePin { Id = "in",        Label = "In",        Direction = PinDirection.Input,  DataType = typeof(bool) },
            new NodePin { Id = "direction", Label = "Direção",   Direction = PinDirection.Input,  DataType = typeof(string), DefaultValue = "down", InputType = PinInputType.Dropdown, Options = ["up", "down"] },
            new NodePin { Id = "amount",    Label = "Quantidade",Direction = PinDirection.Input,  DataType = typeof(int),    DefaultValue = 3 },
            new NodePin { Id = "done",      Label = "Done",      Direction = PinDirection.Output, DataType = typeof(bool) }
        ]
    };
}

public static class DoubleClickMetadata
{
    public static readonly NodeMetadata Instance = new()
    {
        TypeId = "mouse.double_click",
        Name = "Double Click",
        Description = "Performs a double left click at the current mouse position.",
        Category = NodeCategory.Mouse,
        IconKey = "Mouse",
        NodeVersion = new Version(1, 0, 0),
        Pins = [
            new NodePin { Id = "in",   Label = "In",   Direction = PinDirection.Input,  DataType = typeof(bool) },
            new NodePin { Id = "done", Label = "Done", Direction = PinDirection.Output, DataType = typeof(bool) }
        ]
    };
}

public static class HoldKeyMetadata
{
    public static readonly NodeMetadata Instance = new()
    {
        TypeId = "keyboard.hold_key",
        Name = "Hold Key",
        Description = "Holds a key down for a specified duration in milliseconds.",
        Category = NodeCategory.Keyboard,
        IconKey = "Keyboard",
        NodeVersion = new Version(1, 0, 0),
        Pins = [
            new NodePin { Id = "in",    Label = "In",       Direction = PinDirection.Input,  DataType = typeof(bool) },
            new NodePin { Id = "key",   Label = "Tecla",    Direction = PinDirection.Input,  DataType = typeof(string), DefaultValue = "Ctrl", InputType = PinInputType.KeyCapture },
            new NodePin { Id = "ms",    Label = "Hold (ms)",Direction = PinDirection.Input,  DataType = typeof(int),    DefaultValue = 500 },
            new NodePin { Id = "times", Label = "Vezes",    Direction = PinDirection.Input,  DataType = typeof(int),    DefaultValue = 1 },
            new NodePin { Id = "done",  Label = "Done",     Direction = PinDirection.Output, DataType = typeof(bool) }
        ]
    };
}

public static class ComboKeyMetadata
{
    public static readonly NodeMetadata Instance = new()
    {
        TypeId = "keyboard.combo",
        Name = "Key Combo",
        Description = "Presses a key combination such as Ctrl+C.",
        Category = NodeCategory.Keyboard,
        IconKey = "Keyboard",
        NodeVersion = new Version(1, 0, 0),
        Pins = [
            new NodePin { Id = "in",    Label = "In",    Direction = PinDirection.Input,  DataType = typeof(bool) },
            new NodePin { Id = "combo", Label = "Combo", Direction = PinDirection.Input,  DataType = typeof(string), DefaultValue = "Ctrl+C", InputType = PinInputType.KeyCapture },
            new NodePin { Id = "done",  Label = "Done",  Direction = PinDirection.Output, DataType = typeof(bool) }
        ]
    };
}

public class MouseScrollExecutor : INodeExecutor
{
    [DllImport("user32.dll")]
    private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

    private const int MOUSEEVENTF_WHEEL = 0x0800;

    public Task<NodeExecutionResult> ExecuteAsync(FlowNode node, IExecutionContext ctx, IReadOnlyDictionary<string, object?> inputs)
    {
        string dir = "down";
        if (inputs.TryGetValue("direction", out var dirVal) && dirVal is string sDir)
            dir = sDir;
        else if (node.PinValues.TryGetValue("direction", out var sVal) && sVal != null)
            dir = sVal.ToString() ?? "down";

        int amount = 3;
        if (inputs.TryGetValue("amount", out var amVal) && amVal is int rAm)
            amount = rAm;
        else if (node.PinValues.TryGetValue("amount", out var sAm) && sAm is int sAmInt)
            amount = sAmInt;

        ctx.Log($"Executando scroll do mouse: {dir} {amount}x");

        // 120 is WHEEL_DELTA. Positive is scroll forward/up, negative is backward/down.
        int clickAmount = 120 * amount;
        if (dir.Trim().ToLowerInvariant() == "down")
            clickAmount = -clickAmount;

        mouse_event(MOUSEEVENTF_WHEEL, 0, 0, clickAmount, 0);

        return Task.FromResult(NodeExecutionResult.Success(new() { ["done"] = true }));
    }
}

public class DoubleClickExecutor : INodeExecutor
{
    [DllImport("user32.dll")]
    private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

    private const int MOUSEEVENTF_LEFTDOWN = 0x02;
    private const int MOUSEEVENTF_LEFTUP = 0x04;

    public async Task<NodeExecutionResult> ExecuteAsync(FlowNode node, IExecutionContext ctx, IReadOnlyDictionary<string, object?> inputs)
    {
        ctx.Log("Executando duplo clique esquerdo");
        
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        await Task.Delay(100);
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);

        return NodeExecutionResult.Success(new() { ["done"] = true });
    }
}

public class HoldKeyExecutor : INodeExecutor
{
    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    private const int KEYEVENTF_KEYUP = 0x0002;

    public async Task<NodeExecutionResult> ExecuteAsync(FlowNode node, IExecutionContext ctx, IReadOnlyDictionary<string, object?> inputs)
    {
        string key = "Ctrl";
        if (inputs.TryGetValue("key", out var kVal) && kVal is string rk)
            key = rk;
        else if (node.PinValues.TryGetValue("key", out var sk) && sk is string skStr)
            key = skStr;

        int ms = 500;
        if (inputs.TryGetValue("ms", out var mVal) && mVal is int rm)
            ms = rm;
        else if (node.PinValues.TryGetValue("ms", out var sm) && sm is int smInt)
            ms = smInt;

        int times = 1;
        if (inputs.TryGetValue("times", out var tVal) && tVal is int rt)
            times = rt;
        else if (node.PinValues.TryGetValue("times", out var st) && st is int stInt)
            times = stInt;

        ctx.Log($"Segurando tecla {key} por {ms}ms ({times} vez/vezes)...");

        byte virtualKey = GetVirtualKeyCode(key);
        if (virtualKey != 0)
        {
            for (int i = 0; i < times; i++)
            {
                if (i > 0)
                    await Task.Delay(100);

                keybd_event(virtualKey, 0, 0, UIntPtr.Zero); // Down
                await Task.Delay(ms);
                keybd_event(virtualKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // Up
            }
        }

        return NodeExecutionResult.Success(new() { ["done"] = true });
    }

    private static byte GetVirtualKeyCode(string key)
    {
        if (string.IsNullOrEmpty(key)) return 0;
        string cleanKey = key.Trim().ToUpperInvariant();
        if (cleanKey.Length == 1) return (byte)cleanKey[0];

        return cleanKey switch
        {
            "CTRL" or "CONTROL" => 0x11,
            "SHIFT" => 0x10,
            "ALT" => 0x12,
            "ENTER" => 0x0D,
            "SPACE" or "ESPAÇO" => 0x20,
            "BACKSPACE" => 0x08,
            "TAB" => 0x09,
            "ESCAPE" or "ESC" => 0x1B,
            "UP" or "CIMA" => 0x26,
            "DOWN" or "BAIXO" => 0x28,
            "LEFT" or "ESQUERDA" => 0x25,
            "RIGHT" or "DIREITA" => 0x27,
            _ => 0
        };
    }
}

public class ComboKeyExecutor : INodeExecutor
{
    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    private const int KEYEVENTF_KEYUP = 0x0002;

    public async Task<NodeExecutionResult> ExecuteAsync(FlowNode node, IExecutionContext ctx, IReadOnlyDictionary<string, object?> inputs)
    {
        string combo = "Ctrl+C";
        if (inputs.TryGetValue("combo", out var cVal) && cVal is string rc)
            combo = rc;
        else if (node.PinValues.TryGetValue("combo", out var sk) && sk is string skStr)
            combo = skStr;
        else if (inputs.TryGetValue("combo", out var cValObj) && cValObj != null)
            combo = cValObj.ToString() ?? "Ctrl+C";

        ctx.Log($"Executando atalho de teclado: {combo}");

        var keys = combo.Split('+').Select(k => k.Trim().ToUpperInvariant()).ToList();
        var pressedVirtualKeys = new List<byte>();

        try
        {
            foreach (var keyName in keys)
            {
                byte vk = GetVirtualKeyCode(keyName);
                if (vk != 0)
                {
                    keybd_event(vk, 0, 0, UIntPtr.Zero); // Down
                    pressedVirtualKeys.Add(vk);
                    await Task.Delay(10);
                }
            }
        }
        finally
        {
            // Release in reverse order
            pressedVirtualKeys.Reverse();
            foreach (var vk in pressedVirtualKeys)
            {
                keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // Up
                await Task.Delay(10);
            }
        }

        return NodeExecutionResult.Success(new() { ["done"] = true });
    }

    private static byte GetVirtualKeyCode(string key)
    {
        if (string.IsNullOrEmpty(key)) return 0;
        string cleanKey = key.Trim().ToUpperInvariant();
        if (cleanKey.Length == 1) return (byte)cleanKey[0];

        return cleanKey switch
        {
            "CTRL" or "CONTROL" => 0x11,
            "SHIFT" => 0x10,
            "ALT" => 0x12,
            "ENTER" => 0x0D,
            "SPACE" or "ESPAÇO" => 0x20,
            "BACKSPACE" => 0x08,
            "TAB" => 0x09,
            "ESCAPE" or "ESC" => 0x1B,
            "UP" or "CIMA" => 0x26,
            "DOWN" or "BAIXO" => 0x28,
            "LEFT" or "ESQUERDA" => 0x25,
            "RIGHT" or "DIREITA" => 0x27,
            _ => 0
        };
    }
}
