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
        Description = "Holds one or more keys down for a specified duration (e.g. W+A for diagonal movement).",
        Category = NodeCategory.Keyboard,
        IconKey = "Keyboard",
        NodeVersion = new Version(1, 1, 0),
        Pins = [
            new NodePin { Id = "in",    Label = "In",       Direction = PinDirection.Input,  DataType = typeof(bool) },
            new NodePin { Id = "key",   Label = "Tecla(s)", Direction = PinDirection.Input,  DataType = typeof(string), DefaultValue = "W+A", InputType = PinInputType.Text },
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
    public async Task<NodeExecutionResult> ExecuteAsync(FlowNode node, IExecutionContext ctx, IReadOnlyDictionary<string, object?> inputs)
    {
        string key = "W+A";
        if (inputs.TryGetValue("key", out var kv) && kv is string ks)
            key = ks;
        else if (node.PinValues.TryGetValue("key", out var sk) && sk is string skStr)
            key = skStr;
        else if (inputs.TryGetValue("key", out var raw) && raw != null)
            key = raw.ToString() ?? key;
        else if (node.PinValues.TryGetValue("key", out var rawPin) && rawPin != null)
            key = rawPin.ToString() ?? key;

        int ms = MacroKids.Core.Services.PinValueReader.GetInt(inputs, node.PinValues, "ms", 500);
        int times = MacroKids.Core.Services.PinValueReader.GetInt(inputs, node.PinValues, "times", 1);

        var virtualKeys = MacroKids.Core.Services.KeyboardMapper.ParseKeyTokens(key);
        if (virtualKeys.Count == 0)
        {
            ctx.Log($"Aviso: nenhuma tecla reconhecida em '{key}'. Use formatos como W+A, WA ou Ctrl+Shift+A.", MacroKids.Core.Interfaces.LogLevel.Warning);
            return NodeExecutionResult.Success(new() { ["done"] = true });
        }

        string keySummary = string.Join("+", virtualKeys.Select(vk => MacroKids.Core.Services.KeyboardMapper.GetKeyName(vk)).Where(n => n.Length > 0));
        ctx.Log($"Segurando tecla(s) {keySummary} por {ms}ms ({times} vez/vezes)...");

        for (int i = 0; i < times; i++)
        {
            if (i > 0)
                await Task.Delay(100);

            foreach (byte vk in virtualKeys)
                MacroKids.Core.Interop.NativeInput.KeyDown(vk);

            await Task.Delay(ms);

            for (int k = virtualKeys.Count - 1; k >= 0; k--)
                MacroKids.Core.Interop.NativeInput.KeyUp(virtualKeys[k]);
        }

        return NodeExecutionResult.Success(new() { ["done"] = true });
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

        var pressedVirtualKeys = MacroKids.Core.Services.KeyboardMapper.ParseKeyTokens(combo).ToList();

        try
        {
            foreach (byte vk in pressedVirtualKeys)
            {
                keybd_event(vk, 0, 0, UIntPtr.Zero);
                await Task.Delay(10);
            }
        }
        finally
        {
            pressedVirtualKeys.Reverse();
            foreach (byte vk in pressedVirtualKeys)
            {
                keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                await Task.Delay(10);
            }
        }

        return NodeExecutionResult.Success(new() { ["done"] = true });
    }
}
