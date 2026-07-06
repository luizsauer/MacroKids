using MacroKids.Core.Interfaces;
using MacroKids.Core.Models;

namespace MacroKids.Nodes.Logic;

public static class IfConditionMetadata
{
    public static readonly NodeMetadata Instance = new()
    {
        TypeId = "logic.if",
        Name = "If / Condition",
        Description = "Avalia uma condição e desvia o fluxo para True ou False.",
        Category = NodeCategory.Logic,
        IconKey = "Logic",
        NodeVersion = new Version(1, 0, 0),
        Pins = [
            new NodePin { Id = "in",        Label = "In",        Direction = PinDirection.Input,  DataType = typeof(bool) },
            new NodePin { Id = "condition", Label = "Condição",  Direction = PinDirection.Input,  DataType = typeof(string), DefaultValue = "var > 0" },
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
        Description = "Repete a execução do fluxo interno um número fixo de vezes.",
        Category = NodeCategory.Loops,
        IconKey = "Loop",
        NodeVersion = new Version(1, 0, 0),
        Pins = [
            new NodePin { Id = "in",     Label = "In",     Direction = PinDirection.Input,  DataType = typeof(bool) },
            new NodePin { Id = "times",  Label = "Vezes",  Direction = PinDirection.Input,  DataType = typeof(int),  DefaultValue = 5 },
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
        Description = "Itera sobre cada item de uma lista.",
        Category = NodeCategory.Loops,
        IconKey = "Loop",
        NodeVersion = new Version(1, 0, 0),
        Pins = [
            new NodePin { Id = "in",   Label = "In",   Direction = PinDirection.Input,  DataType = typeof(bool) },
            new NodePin { Id = "list", Label = "Lista",Direction = PinDirection.Input,  DataType = typeof(string), DefaultValue = "myList", InputType = PinInputType.Dropdown },
            new NodePin { Id = "item", Label = "Item", Direction = PinDirection.Output, DataType = typeof(bool) },
            new NodePin { Id = "done", Label = "Done", Direction = PinDirection.Output, DataType = typeof(bool) }
        ]
    };
}

public static class ForLoopMetadata
{
    public static readonly NodeMetadata Instance = new()
    {
        TypeId = "logic.for",
        Name = "For Loop",
        Description = "Loop numérico com limite inicial, final e incremento.",
        Category = NodeCategory.Loops,
        IconKey = "Loop",
        NodeVersion = new Version(1, 0, 0),
        Pins = [
            new NodePin { Id = "in",    Label = "In",      Direction = PinDirection.Input,  DataType = typeof(bool) },
            new NodePin { Id = "start", Label = "Início",  Direction = PinDirection.Input,  DataType = typeof(int), DefaultValue = 0 },
            new NodePin { Id = "end",   Label = "Fim",     Direction = PinDirection.Input,  DataType = typeof(int), DefaultValue = 10 },
            new NodePin { Id = "step",  Label = "Passo",   Direction = PinDirection.Input,  DataType = typeof(int), DefaultValue = 1 },
            new NodePin { Id = "index", Label = "Índice",  Direction = PinDirection.Output, DataType = typeof(int) },
            new NodePin { Id = "loop",  Label = "Loop",    Direction = PinDirection.Output, DataType = typeof(bool) },
            new NodePin { Id = "done",  Label = "Done",    Direction = PinDirection.Output, DataType = typeof(bool) }
        ]
    };
}

public static class WhileLoopMetadata
{
    public static readonly NodeMetadata Instance = new()
    {
        TypeId = "logic.while",
        Name = "While Loop",
        Description = "Repete o fluxo enquanto a condição especificada for verdadeira.",
        Category = NodeCategory.Loops,
        IconKey = "Loop",
        NodeVersion = new Version(1, 0, 0),
        Pins = [
            new NodePin { Id = "in",        Label = "In",        Direction = PinDirection.Input,  DataType = typeof(bool) },
            new NodePin { Id = "condition", Label = "Condição",  Direction = PinDirection.Input,  DataType = typeof(string), DefaultValue = "var < 10" },
            new NodePin { Id = "loop",      Label = "Loop",      Direction = PinDirection.Output, DataType = typeof(bool) },
            new NodePin { Id = "done",      Label = "Done",      Direction = PinDirection.Output, DataType = typeof(bool) }
        ]
    };
}

public class IfConditionExecutor : INodeExecutor
{
    public Task<NodeExecutionResult> ExecuteAsync(FlowNode node, IExecutionContext context, IReadOnlyDictionary<string, object?> resolvedInputs)
    {
        string condition = "var > 0";
        if (resolvedInputs.TryGetValue("condition", out var cVal) && cVal is string rc)
            condition = rc;
        else if (node.PinValues.TryGetValue("condition", out var sc) && sc is string scStr)
            condition = scStr;

        context.Log($"Avaliando condição If: {condition}");

        bool evaluationResult = EvaluateCondition(condition, context);

        context.Log($"Resultado da condição If: {evaluationResult}");

        if (evaluationResult)
            return Task.FromResult(NodeExecutionResult.Success(new() { ["true"] = true }));
        else
            return Task.FromResult(NodeExecutionResult.Success(new() { ["false"] = true }));
    }

    public static bool EvaluateCondition(string condition, IExecutionContext context)
    {
        try
        {
            var parts = condition.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 3)
            {
                var varName = parts[0];
                var op = parts[1];
                var targetStr = parts[2];

                if (context.TryGetVariable(varName, out var actualVal))
                {
                    double actualDouble = Convert.ToDouble(actualVal ?? 0);
                    double targetDouble = Convert.ToDouble(targetStr);

                    return op switch
                    {
                        ">" => actualDouble > targetDouble,
                        ">=" => actualDouble >= targetDouble,
                        "<" => actualDouble < targetDouble,
                        "<=" => actualDouble <= targetDouble,
                        "==" => actualDouble == targetDouble,
                        "!=" => actualDouble != targetDouble,
                        _ => false
                    };
                }
            }
            return !string.IsNullOrWhiteSpace(condition);
        }
        catch
        {
            return false;
        }
    }
}

public class RepeatLoopExecutor : INodeExecutor
{
    public Task<NodeExecutionResult> ExecuteAsync(FlowNode node, IExecutionContext context, IReadOnlyDictionary<string, object?> resolvedInputs)
    {
        // A lógica real é processada no FlowExecutor.cs
        return Task.FromResult(NodeExecutionResult.Success(new() { ["done"] = true }));
    }
}

public class ForEachExecutor : INodeExecutor
{
    public Task<NodeExecutionResult> ExecuteAsync(FlowNode node, IExecutionContext context, IReadOnlyDictionary<string, object?> resolvedInputs)
    {
        // A lógica real é processada no FlowExecutor.cs
        return Task.FromResult(NodeExecutionResult.Success(new() { ["done"] = true }));
    }
}

public class ForLoopExecutor : INodeExecutor
{
    public Task<NodeExecutionResult> ExecuteAsync(FlowNode node, IExecutionContext context, IReadOnlyDictionary<string, object?> resolvedInputs)
    {
        // A lógica real é processada no FlowExecutor.cs
        return Task.FromResult(NodeExecutionResult.Success(new() { ["done"] = true }));
    }
}

public class WhileLoopExecutor : INodeExecutor
{
    public Task<NodeExecutionResult> ExecuteAsync(FlowNode node, IExecutionContext context, IReadOnlyDictionary<string, object?> resolvedInputs)
    {
        // A lógica real é processada no FlowExecutor.cs
        return Task.FromResult(NodeExecutionResult.Success(new() { ["done"] = true }));
    }
}
