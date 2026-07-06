using MacroKids.Core.Events;
using MacroKids.Core.Interfaces;
using MacroKids.Core.Models;

namespace MacroKids.Runtime;

/// <summary>
/// Engine that traverses a <see cref="FlowDocument"/> graph and executes each node in order.
/// Publishes lifecycle events via <see cref="IEventBus"/> so the UI can visualize execution
/// in real time without any direct coupling to this class.
/// </summary>
public sealed class FlowExecutor
{
    private readonly INodeRegistry _registry;
    private readonly IEventBus _eventBus;

    private CancellationTokenSource? _cts;
    private SemaphoreSlim? _pauseSemaphore;

    public FlowExecutor(INodeRegistry registry, IEventBus eventBus)
    {
        _registry  = registry;
        _eventBus  = eventBus;
    }

    // ── State ─────────────────────────────────────────────────────────────────

    public bool IsRunning { get; private set; }
    public bool IsPaused  { get; private set; }

    /// <summary>
    /// Delay (ms) injected between node executions.
    /// 0 = full speed; higher values slow down the visual for learning/debug.
    /// </summary>
    public int StepDelayMs { get; set; } = 0;

    // ── Control ───────────────────────────────────────────────────────────────

    /// <summary>Start executing the flow asynchronously.</summary>
    public async Task RunAsync(FlowDocument document)
    {
        if (IsRunning)
            return;

        _cts           = new CancellationTokenSource();
        _pauseSemaphore = new SemaphoreSlim(1, 1);
        IsRunning      = true;
        IsPaused       = false;

        var startedAt = DateTime.UtcNow;
        _eventBus.Publish(new ExecutionStartedEvent(document.Id, startedAt));

        try
        {
            var context = new ExecutionContext(_eventBus, _cts.Token);
            var orderedNodes = TopologicalSort(document);

            // Track output values keyed by (nodeInstanceId, pinId)
            var outputCache = new Dictionary<(Guid, string), object?>();

            foreach (var node in orderedNodes)
            {
                _cts.Token.ThrowIfCancellationRequested();

                // Wait if paused (Step mode releases the semaphore once)
                await _pauseSemaphore.WaitAsync(_cts.Token);
                _pauseSemaphore.Release();

                if (node.IsDisabled)
                {
                    _eventBus.Publish(new NodeSkippedEvent(
                        document.Id, node.InstanceId, node.TypeId, "Node is disabled"));
                    continue;
                }

                if (!_registry.TryGet(node.TypeId, out _, out var executor) || executor is null)
                {
                    _eventBus.Publish(new NodeSkippedEvent(
                        document.Id, node.InstanceId, node.TypeId,
                        $"TypeId '{node.TypeId}' not registered"));
                    continue;
                }

                _eventBus.Publish(new NodeStartedEvent(
                    document.Id, node.InstanceId, node.TypeId));

                var sw = System.Diagnostics.Stopwatch.StartNew();

                // Resolve input values (static values + upstream outputs)
                var resolvedInputs = ResolveInputs(node, document, outputCache);

                NodeExecutionResult result;
                try
                {
                    result = await executor.ExecuteAsync(node, context, resolvedInputs);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    _eventBus.Publish(new NodeErrorEvent(
                        document.Id, node.InstanceId, node.TypeId, ex));
                    context.Log($"Error in '{node.TypeId}': {ex.Message}", LogLevel.Error);
                    break; // stop execution on unhandled node error
                }

                sw.Stop();

                if (result.IsSuccess)
                {
                    // Cache outputs for downstream nodes
                    foreach (var kv in result.OutputValues)
                        outputCache[(node.InstanceId, kv.Key)] = kv.Value;

                    _eventBus.Publish(new NodeCompletedEvent(
                        document.Id, node.InstanceId, node.TypeId,
                        sw.Elapsed, result.OutputValues));
                }
                else
                {
                    _eventBus.Publish(new NodeErrorEvent(
                        document.Id, node.InstanceId, node.TypeId,
                        result.Exception ?? new Exception(result.ErrorMessage)));
                    break;
                }

                // Apply the node's custom inline delay if specified
                int inlineDelay = 0;
                if (node.PinValues.TryGetValue("delay", out var dVal))
                {
                    if (dVal is int id) inlineDelay = id;
                    else if (dVal != null) int.TryParse(dVal.ToString(), out inlineDelay);
                }
                if (inlineDelay > 0)
                {
                    await Task.Delay(inlineDelay, _cts.Token);
                }

                if (StepDelayMs > 0)
                    await Task.Delay(StepDelayMs, _cts.Token);
            }

            _eventBus.Publish(new ExecutionCompletedEvent(
                document.Id, DateTime.UtcNow - startedAt));
        }
        catch (OperationCanceledException)
        {
            _eventBus.Publish(new ExecutionStoppedByUserEvent(document.Id));
        }
        catch (Exception ex)
        {
            _eventBus.Publish(new ExecutionFailedEvent(document.Id, ex));
        }
        finally
        {
            IsRunning = false;
            IsPaused  = false;
            _cts.Dispose();
            _cts = null;
        }
    }

    /// <summary>Stop execution immediately.</summary>
    public void Stop() => _cts?.Cancel();

    /// <summary>Pause execution after the current node completes.</summary>
    public void Pause()
    {
        if (!IsRunning || IsPaused || _pauseSemaphore is null)
            return;

        _pauseSemaphore.Wait(0); // acquire — next WaitAsync in RunAsync will block
        IsPaused = true;

        if (_cts is not null)
            _eventBus.Publish(new ExecutionPausedEvent(Guid.Empty, Guid.Empty));
    }

    /// <summary>Resume paused execution.</summary>
    public void Resume()
    {
        if (!IsPaused || _pauseSemaphore is null)
            return;

        IsPaused = false;
        _pauseSemaphore.Release();
        _eventBus.Publish(new ExecutionResumedEvent(Guid.Empty));
    }

    // ── Graph helpers ─────────────────────────────────────────────────────────

    /// <summary>
    /// Topological sort (Kahn's algorithm) — returns nodes in execution order.
    /// Nodes with no incoming connections come first (trigger/event nodes).
    /// </summary>
    private static List<FlowNode> TopologicalSort(FlowDocument document)
    {
        var inDegree = document.Nodes.ToDictionary(n => n.InstanceId, _ => 0);

        foreach (var conn in document.Connections)
            inDegree[conn.TargetNodeId]++;

        var queue  = new Queue<FlowNode>(document.Nodes.Where(n => inDegree[n.InstanceId] == 0));
        var result = new List<FlowNode>();

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            result.Add(node);

            foreach (var downstream in document.Connections
                .Where(c => c.SourceNodeId == node.InstanceId)
                .Select(c => document.Nodes.FirstOrDefault(n => n.InstanceId == c.TargetNodeId))
                .OfType<FlowNode>())
            {
                inDegree[downstream.InstanceId]--;
                if (inDegree[downstream.InstanceId] == 0)
                    queue.Enqueue(downstream);
            }
        }

        return result;
    }

    /// <summary>
    /// Resolves the effective input values for a node:
    /// - First uses the static PinValues from the node instance.
    /// - Then overlays any values produced by upstream connected nodes.
    /// </summary>
    private static IReadOnlyDictionary<string, object?> ResolveInputs(
        FlowNode node,
        FlowDocument document,
        IReadOnlyDictionary<(Guid, string), object?> outputCache)
    {
        var resolved = new Dictionary<string, object?>(node.PinValues);

        foreach (var conn in document.Connections.Where(c => c.TargetNodeId == node.InstanceId))
        {
            var key = (conn.SourceNodeId, conn.SourcePinId);
            if (outputCache.TryGetValue(key, out var upstreamValue))
                resolved[conn.TargetPinId] = upstreamValue;
        }

        return resolved;
    }
}
