using MacroKids.Core.Interfaces;
using MacroKids.Core.Models;

namespace MacroKids.Nodes.Variables;

public static class SetVariableMetadata
{
    public static readonly NodeMetadata Instance = new()
    {
        TypeId = "variables.set",
        Name = "Set Variable",
        Description = "Assigns a value to a named variable.",
        Category = NodeCategory.Variables,
        IconKey = "Variable",
        NodeVersion = new Version(1, 0, 0),
        Pins = [
            new NodePin { Id = "in",    Label = "In",    Direction = PinDirection.Input,  DataType = typeof(bool) },
            new NodePin { Id = "name",  Label = "Name",  Direction = PinDirection.Input,  DataType = typeof(string), DefaultValue = "myVar" },
            new NodePin { Id = "value", Label = "Value", Direction = PinDirection.Input,  DataType = typeof(string), DefaultValue = "" },
            new NodePin { Id = "done",  Label = "Done",  Direction = PinDirection.Output, DataType = typeof(bool) }
        ]
    };
}

public static class GetVariableMetadata
{
    public static readonly NodeMetadata Instance = new()
    {
        TypeId = "variables.get",
        Name = "Get Variable",
        Description = "Reads the current value of a named variable.",
        Category = NodeCategory.Variables,
        IconKey = "Variable",
        NodeVersion = new Version(1, 0, 0),
        Pins = [
            new NodePin { Id = "in",    Label = "In",    Direction = PinDirection.Input,  DataType = typeof(bool) },
            new NodePin { Id = "name",  Label = "Name",  Direction = PinDirection.Input,  DataType = typeof(string), DefaultValue = "myVar" },
            new NodePin { Id = "value", Label = "Value", Direction = PinDirection.Output, DataType = typeof(string) },
            new NodePin { Id = "done",  Label = "Done",  Direction = PinDirection.Output, DataType = typeof(bool) }
        ]
    };
}

public static class IncrementVariableMetadata
{
    public static readonly NodeMetadata Instance = new()
    {
        TypeId = "variables.increment",
        Name = "Increment",
        Description = "Adds a number to a named variable.",
        Category = NodeCategory.Variables,
        IconKey = "Variable",
        NodeVersion = new Version(1, 0, 0),
        Pins = [
            new NodePin { Id = "in",    Label = "In",    Direction = PinDirection.Input,  DataType = typeof(bool) },
            new NodePin { Id = "name",  Label = "Name",  Direction = PinDirection.Input,  DataType = typeof(string), DefaultValue = "counter" },
            new NodePin { Id = "by",    Label = "By",    Direction = PinDirection.Input,  DataType = typeof(int), DefaultValue = 1 },
            new NodePin { Id = "done",  Label = "Done",  Direction = PinDirection.Output, DataType = typeof(bool) }
        ]
    };
}

/// <summary>Stub executors — real logic will be wired in a later milestone.</summary>
public class SetVariableExecutor : INodeExecutor
{
    public Task<NodeExecutionResult> ExecuteAsync(FlowNode node, IExecutionContext context, IReadOnlyDictionary<string, object?> resolvedInputs)
        => Task.FromResult(NodeExecutionResult.Success(new() { ["done"] = true }));
}

public class GetVariableExecutor : INodeExecutor
{
    public Task<NodeExecutionResult> ExecuteAsync(FlowNode node, IExecutionContext context, IReadOnlyDictionary<string, object?> resolvedInputs)
        => Task.FromResult(NodeExecutionResult.Success(new() { ["value"] = null, ["done"] = true }));
}

public class IncrementVariableExecutor : INodeExecutor
{
    public Task<NodeExecutionResult> ExecuteAsync(FlowNode node, IExecutionContext context, IReadOnlyDictionary<string, object?> resolvedInputs)
        => Task.FromResult(NodeExecutionResult.Success(new() { ["done"] = true }));
}
