using MacroKids.Core.Interfaces;
using MacroKids.Core.Models;

namespace MacroKids.Nodes.Flow;

/// <summary>
/// Metadata definition for the Wait / Delay visual block.
/// </summary>
public static class WaitMetadata
{
    public static readonly NodeMetadata Instance = new()
    {
        TypeId = "flow.wait",
        Name = "Aguardar",
        Description = "Espera uma quantidade de tempo em milissegundos antes de ir para o próximo bloco.",
        Category = NodeCategory.Loops,
        IconKey = "Timer",
        NodeVersion = new Version(1, 0, 0),
        Pins = [
            new NodePin { Id = "ms", Label = "Tempo (ms)", Direction = PinDirection.Input, DataType = typeof(int), DefaultValue = 1000 },
            new NodePin { Id = "done", Label = "Concluído", Direction = PinDirection.Output, DataType = typeof(bool) }
        ]
    };
}

/// <summary>
/// Executor to suspend run progress asynchronously.
/// </summary>
public class WaitExecutor : INodeExecutor
{
    public async Task<NodeExecutionResult> ExecuteAsync(
        FlowNode node,
        IExecutionContext context,
        IReadOnlyDictionary<string, object?> resolvedInputs)
    {
        int ms = 1000;

        if (resolvedInputs != null && resolvedInputs.TryGetValue("ms", out var msVal) && msVal is int rms)
            ms = rms;
        else if (node != null && node.PinValues.TryGetValue("ms", out var sms) && sms is int smsInt)
            ms = smsInt;

        if (context != null)
        {
            context.Log($"Aguardando por {ms} milissegundos...");
            
            try
            {
                await Task.Delay(ms, context.CancellationToken);
            }
            catch (OperationCanceledException)
            {
                context.Log("Espera cancelada pelo usuário.");
                throw;
            }
        }

        return NodeExecutionResult.Success(new() { ["done"] = true });
    }
}
