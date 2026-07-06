using MacroKids.Core.Interfaces;
using MacroKids.Core.Models;

namespace MacroKids.Nodes.Logic;

public static class IfConditionMetadata
{
    public static readonly NodeMetadata Instance = new()
    {
        TypeId = "logic.if",
        Name = "If / Condition",
        Description = "Evaluates a condition and routes to the true or false branch.",
        Category = NodeCategory.Logic,
        IconKey = "Logic",
        NodeVersion = new Version(1, 0, 0),
        Pins = [
            new NodePin { Id = "in",        Label = "In",        Direction = PinDirection.Input,  DataType = typeof(bool) },
            new NodePin { Id = "condition", Label = "Condition", Direction = PinDirection.Input,  DataType = typeof(string), DefaultValue = "var > 0" },
            new NodePin { Id = "true",      Label = "True",      Direction = PinDirection.Output, DataType = typeof(bool) },
            new NodePin { Id = "false",     Label = "False",     Direction = PinDirection.Output, DataType = typeof(bool) }
        ]
    };
}

public static class RepeatLoopMetadata
{
    public static readonly NodeMetadata Instance = new()
    {
        TypeId = "logic.repeat",
        Name = "Repeat",
        Description = "Repeats the inner flow a fixed number of times.",
        Category = NodeCategory.Loops,
        IconKey = "Loop",
        NodeVersion = new Version(1, 0, 0),
        Pins = [
            new NodePin { Id = "in",     Label = "In",     Direction = PinDirection.Input,  DataType = typeof(bool) },
            new NodePin { Id = "times",  Label = "Times",  Direction = PinDirection.Input,  DataType = typeof(int),  DefaultValue = 5 },
            new NodePin { Id = "loop",   Label = "Loop",   Direction = PinDirection.Output, DataType = typeof(bool) },
            new NodePin { Id = "done",   Label = "Done",   Direction = PinDirection.Output, DataType = typeof(bool) }
        ]
    };
}

public static class ForEachMetadata
{
    public static readonly NodeMetadata Instance = new()
    {
        TypeId = "logic.foreach",
        Name = "For Each",
        Description = "Iterates over each item in a list variable.",
        Category = NodeCategory.Loops,
        IconKey = "Loop",
        NodeVersion = new Version(1, 0, 0),
        Pins = [
            new NodePin { Id = "in",   Label = "In",   Direction = PinDirection.Input,  DataType = typeof(bool) },
            new NodePin { Id = "list", Label = "List", Direction = PinDirection.Input,  DataType = typeof(string), DefaultValue = "myList" },
            new NodePin { Id = "item", Label = "Item", Direction = PinDirection.Output, DataType = typeof(bool) },
            new NodePin { Id = "done", Label = "Done", Direction = PinDirection.Output, DataType = typeof(bool) }
        ]
    };
}

public class IfConditionExecutor : INodeExecutor
{
    public Task<NodeExecutionResult> ExecuteAsync(FlowNode node, IExecutionContext context, IReadOnlyDictionary<string, object?> resolvedInputs)
        => Task.FromResult(NodeExecutionResult.Success(new() { ["true"] = true }));
}

public class RepeatLoopExecutor : INodeExecutor
{
    public Task<NodeExecutionResult> ExecuteAsync(FlowNode node, IExecutionContext context, IReadOnlyDictionary<string, object?> resolvedInputs)
        => Task.FromResult(NodeExecutionResult.Success(new() { ["done"] = true }));
}

public class ForEachExecutor : INodeExecutor
{
    public Task<NodeExecutionResult> ExecuteAsync(FlowNode node, IExecutionContext context, IReadOnlyDictionary<string, object?> resolvedInputs)
        => Task.FromResult(NodeExecutionResult.Success(new() { ["done"] = true }));
}
