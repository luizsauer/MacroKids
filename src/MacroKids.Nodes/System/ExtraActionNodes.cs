using MacroKids.Core.Interfaces;
using MacroKids.Core.Models;

namespace MacroKids.Nodes.System;

public static class MouseScrollMetadata
{
    public static readonly NodeMetadata Instance = new()
    {
        TypeId = "mouse.scroll",
        Name = "Scroll",
        Description = "Scrolls the mouse wheel up or down by the given amount.",
        Category = NodeCategory.Mouse,
        IconKey = "Mouse",
        NodeVersion = new Version(1, 0, 0),
        Pins = [
            new NodePin { Id = "in",        Label = "In",        Direction = PinDirection.Input,  DataType = typeof(bool) },
            new NodePin { Id = "direction", Label = "Direction", Direction = PinDirection.Input,  DataType = typeof(string), DefaultValue = "down" },
            new NodePin { Id = "amount",    Label = "Amount",    Direction = PinDirection.Input,  DataType = typeof(int),    DefaultValue = 3 },
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
            new NodePin { Id = "in",   Label = "In",       Direction = PinDirection.Input,  DataType = typeof(bool) },
            new NodePin { Id = "key",  Label = "Key",      Direction = PinDirection.Input,  DataType = typeof(string), DefaultValue = "Ctrl" },
            new NodePin { Id = "ms",   Label = "Hold (ms)",Direction = PinDirection.Input,  DataType = typeof(int),    DefaultValue = 500 },
            new NodePin { Id = "done", Label = "Done",     Direction = PinDirection.Output, DataType = typeof(bool) }
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
            new NodePin { Id = "combo", Label = "Combo", Direction = PinDirection.Input,  DataType = typeof(string), DefaultValue = "Ctrl+C" },
            new NodePin { Id = "done",  Label = "Done",  Direction = PinDirection.Output, DataType = typeof(bool) }
        ]
    };
}

public class MouseScrollExecutor : INodeExecutor
{
    public Task<NodeExecutionResult> ExecuteAsync(FlowNode node, IExecutionContext ctx, IReadOnlyDictionary<string, object?> inputs)
        => Task.FromResult(NodeExecutionResult.Success(new() { ["done"] = true }));
}

public class DoubleClickExecutor : INodeExecutor
{
    public Task<NodeExecutionResult> ExecuteAsync(FlowNode node, IExecutionContext ctx, IReadOnlyDictionary<string, object?> inputs)
        => Task.FromResult(NodeExecutionResult.Success(new() { ["done"] = true }));
}

public class HoldKeyExecutor : INodeExecutor
{
    public Task<NodeExecutionResult> ExecuteAsync(FlowNode node, IExecutionContext ctx, IReadOnlyDictionary<string, object?> inputs)
        => Task.FromResult(NodeExecutionResult.Success(new() { ["done"] = true }));
}

public class ComboKeyExecutor : INodeExecutor
{
    public Task<NodeExecutionResult> ExecuteAsync(FlowNode node, IExecutionContext ctx, IReadOnlyDictionary<string, object?> inputs)
        => Task.FromResult(NodeExecutionResult.Success(new() { ["done"] = true }));
}
