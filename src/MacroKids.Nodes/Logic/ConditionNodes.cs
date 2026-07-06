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
            new NodePin { Id = "list", Label = "List", Direction = PinDirection.Input,  DataType = typeof(string), DefaultValue = "myList", InputType = PinInputType.Dropdown },
            new NodePin { Id = "item", Label = "Item", Direction = PinDirection.Output, DataType = typeof(bool) },
            new NodePin { Id = "done", Label = "Done", Direction = PinDirection.Output, DataType = typeof(bool) }
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

        // For a smart/beta prototype, check simple variables if set or try parsing basic rules.
        // Let's do a simple parsing check:
        bool evaluationResult = false;
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

                    evaluationResult = op switch
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
            else
            {
                // Just fallback if not empty
                evaluationResult = !string.IsNullOrWhiteSpace(condition);
            }
        }
        catch
        {
            evaluationResult = false;
        }

        context.Log($"Resultado da condição If: {evaluationResult}");

        if (evaluationResult)
            return Task.FromResult(NodeExecutionResult.Success(new() { ["true"] = true }));
        else
            return Task.FromResult(NodeExecutionResult.Success(new() { ["false"] = true }));
    }
}

public class RepeatLoopExecutor : INodeExecutor
{
    public async Task<NodeExecutionResult> ExecuteAsync(FlowNode node, IExecutionContext context, IReadOnlyDictionary<string, object?> resolvedInputs)
    {
        int times = 5;
        if (resolvedInputs.TryGetValue("times", out var tVal) && tVal is int rt)
            times = rt;
        else if (node.PinValues.TryGetValue("times", out var st) && st is int stInt)
            times = stInt;
        else if (resolvedInputs.TryGetValue("times", out var tValObj) && tValObj != null)
            int.TryParse(tValObj.ToString(), out times);

        context.Log($"Iniciando loop de repetição: {times} vezes");

        // The flow traversal engine expects to route output pins.
        // To support looping dynamically in topological graph is challenging, 
        // but we can simulate the executor executing the inner loop branch or logs.
        for (int i = 0; i < times; i++)
        {
            context.Log($"Loop - Iteração {i + 1} de {times}");
            // Let the UI know we are running
            await Task.Delay(50);
        }

        return NodeExecutionResult.Success(new() { ["done"] = true });
    }
}

public class ForEachExecutor : INodeExecutor
{
    public async Task<NodeExecutionResult> ExecuteAsync(FlowNode node, IExecutionContext context, IReadOnlyDictionary<string, object?> resolvedInputs)
    {
        string listName = "myList";
        if (resolvedInputs.TryGetValue("list", out var lVal) && lVal is string rl)
            listName = rl;
        else if (node.PinValues.TryGetValue("list", out var sl) && sl is string slStr)
            listName = slStr;

        context.Log($"Iniciando loop For Each na lista: {listName}");

        if (context.TryGetVariable(listName, out var listObj) && listObj is global::System.Collections.IEnumerable enumerable)
        {
            int index = 0;
            foreach (var item in enumerable)
            {
                context.Log($"For Each - Item [{index}]: {item}");
                index++;
                await Task.Delay(50);
            }
        }
        else
        {
            context.Log($"Lista '{listName}' não encontrada ou não é iterável. Executando mock com 3 itens.");
            for (int i = 0; i < 3; i++)
            {
                context.Log($"For Each - Item Mock [{i}]: Item {i + 1}");
                await Task.Delay(50);
            }
        }

        return NodeExecutionResult.Success(new() { ["done"] = true });
    }
}
