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
            new NodePin { Id = "in", Label = "Entrada", Direction = PinDirection.Input, DataType = typeof(bool) },
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
        object? rawMs = null;

        if (resolvedInputs != null && resolvedInputs.TryGetValue("ms", out var msVal))
            rawMs = msVal;
        else if (node != null && node.PinValues.TryGetValue("ms", out var sms))
            rawMs = sms;

        if (rawMs != null)
        {
            if (rawMs is int iVal) ms = iVal;
            else if (rawMs is double dVal) ms = (int)dVal;
            else int.TryParse(rawMs.ToString(), out ms);
        }

        if (context != null)
        {
            context.Log($"Aguardando por {ms} milissegundos...");
            
            try
            {
                if (ms >= 1000)
                {
                    int remaining = ms;
                    while (remaining > 0)
                    {
                        context.CancellationToken.ThrowIfCancellationRequested();
                        
                        double secs = Math.Round(remaining / 1000.0, 1);
                        context.EventBus.Publish(new MacroKids.Core.Events.NodeStatusUpdatedEvent(
                            Guid.Empty, node.InstanceId, $"Aguardando {secs:0.0}s..."));

                        int chunk = Math.Min(remaining, 200);
                        await Task.Delay(chunk, context.CancellationToken);
                        remaining -= chunk;
                    }
                }
                else
                {
                    context.EventBus.Publish(new MacroKids.Core.Events.NodeStatusUpdatedEvent(
                        Guid.Empty, node.InstanceId, $"Aguardando {ms}ms..."));
                    await Task.Delay(ms, context.CancellationToken);
                }
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
