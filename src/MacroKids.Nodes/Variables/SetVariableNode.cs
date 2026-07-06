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
            new NodePin { Id = "name",  Label = "Name",  Direction = PinDirection.Input,  DataType = typeof(string), DefaultValue = "myVar", InputType = PinInputType.Dropdown },
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
            new NodePin { Id = "name",  Label = "Name",  Direction = PinDirection.Input,  DataType = typeof(string), DefaultValue = "counter", InputType = PinInputType.Dropdown },
            new NodePin { Id = "by",    Label = "By",    Direction = PinDirection.Input,  DataType = typeof(int), DefaultValue = 1 },
            new NodePin { Id = "done",  Label = "Done",  Direction = PinDirection.Output, DataType = typeof(bool) }
        ]
    };
}

public class SetVariableExecutor : INodeExecutor
{
    public Task<NodeExecutionResult> ExecuteAsync(FlowNode node, IExecutionContext context, IReadOnlyDictionary<string, object?> resolvedInputs)
    {
        string name = "myVar";
        if (resolvedInputs.TryGetValue("name", out var nVal) && nVal is string sn)
            name = sn;
        else if (node.PinValues.TryGetValue("name", out var sVal) && sVal != null)
            name = sVal.ToString() ?? "myVar";

        object? value = null;
        if (resolvedInputs.TryGetValue("value", out var vVal))
            value = vVal;
        else if (node.PinValues.TryGetValue("value", out var sv))
            value = sv;

        context.Log($"Definindo variável '{name}' com o valor: {value}");
        context.SetVariable(name, value);

        return Task.FromResult(NodeExecutionResult.Success(new() { ["done"] = true }));
    }
}

public class GetVariableExecutor : INodeExecutor
{
    public Task<NodeExecutionResult> ExecuteAsync(FlowNode node, IExecutionContext context, IReadOnlyDictionary<string, object?> resolvedInputs)
    {
        string name = "myVar";
        if (resolvedInputs.TryGetValue("name", out var nVal) && nVal is string sn)
            name = sn;
        else if (node.PinValues.TryGetValue("name", out var sVal) && sVal != null)
            name = sVal.ToString() ?? "myVar";

        context.TryGetVariable(name, out var val);
        context.Log($"Obtendo variável '{name}': {val}");

        return Task.FromResult(NodeExecutionResult.Success(new() { 
            ["value"] = val,
            ["done"] = true 
        }));
    }
}

public class IncrementVariableExecutor : INodeExecutor
{
    public Task<NodeExecutionResult> ExecuteAsync(FlowNode node, IExecutionContext context, IReadOnlyDictionary<string, object?> resolvedInputs)
    {
        string name = "counter";
        if (resolvedInputs.TryGetValue("name", out var nVal) && nVal is string sn)
            name = sn;
        else if (node.PinValues.TryGetValue("name", out var sVal) && sVal != null)
            name = sVal.ToString() ?? "counter";

        int by = 1;
        object? rawBy = null;
        if (resolvedInputs.TryGetValue("by", out var byVal))
            rawBy = byVal;
        else if (node.PinValues.TryGetValue("by", out var sBy))
            rawBy = sBy;

        if (rawBy != null)
        {
            if (rawBy is int iVal) by = iVal;
            else int.TryParse(rawBy.ToString(), out by);
        }

        context.TryGetVariable(name, out var currentObj);
        double currentVal = 0;
        if (currentObj != null)
        {
            double.TryParse(currentObj.ToString(), out currentVal);
        }

        double newVal = currentVal + by;
        context.SetVariable(name, newVal);
        context.Log($"Incrementando variável '{name}' por {by}. Novo valor: {newVal}");

        return Task.FromResult(NodeExecutionResult.Success(new() { ["done"] = true }));
    }
}
