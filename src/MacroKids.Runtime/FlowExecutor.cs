using MacroKids.Core.Events;
using MacroKids.Core.Interfaces;
using MacroKids.Core.Models;
using MacroKids.Core.Services;

namespace MacroKids.Runtime;

/// <summary>
/// Engine that traverses a <see cref="FlowDocument"/> graph following active execution paths (flow pins).
/// Supports branching (If), loops (Repeat, ForEach), and sequential execution.
/// </summary>
public sealed class FlowExecutor
{
    private readonly INodeRegistry _registry;
    private readonly IEventBus _eventBus;

    private CancellationTokenSource? _cts;
    private SemaphoreSlim? _pauseSemaphore;
    private readonly HashSet<Guid> _currentlyExecutingNodes = [];

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

    /// <summary>Start executing the flow by tracing active flow connections.</summary>
    public async Task RunAsync(FlowDocument document)
    {
        if (IsRunning)
            return;

        _cts           = new CancellationTokenSource();
        _pauseSemaphore = new SemaphoreSlim(1, 1);
        IsRunning      = true;
        IsPaused       = false;
        _currentlyExecutingNodes.Clear();

        var startedAt = DateTime.UtcNow;
        _eventBus.Publish(new ExecutionStartedEvent(document.Id, startedAt));

        try
        {
            var context = new ExecutionContext(_eventBus, _cts.Token);
            var outputCache = new Dictionary<(Guid, string), object?>();

            // 1. Find root nodes (start points).
            // A node is a root if it has a flow input pin "in" but no incoming connections to it.
            // Or if it doesn't have any flow input pins at all.
            var flowInputPinIds = new HashSet<Guid>();
            foreach (var conn in document.Connections)
            {
                // If it connects to a flow pin (usually "in")
                if (conn.TargetPinId == "in")
                {
                    flowInputPinIds.Add(conn.TargetNodeId);
                }
            }

            var startNodes = document.Nodes
                .Where(n => !flowInputPinIds.Contains(n.InstanceId) && !n.IsDisabled)
                .ToList();

            if (startNodes.Count == 0 && document.Nodes.Any())
            {
                // Fallback: pick the first non-disabled node
                var firstNode = document.Nodes.FirstOrDefault(n => !n.IsDisabled);
                if (firstNode != null)
                {
                    startNodes.Add(firstNode);
                }
            }

            context.Log($"Iniciando execução do fluxo. {startNodes.Count} ponto(s) de partida encontrado(s).");

            // Execute all starting paths
            foreach (var node in startNodes)
            {
                await ExecuteBranchAsync(node.InstanceId, context, outputCache, document);
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

    /// <summary>
    /// Executes a node and recursively follows all triggered flow output connections.
    /// Special nodes (If, Repeat, ForEach) are orchestrated directly.
    /// </summary>
    private async Task ExecuteBranchAsync(
        Guid nodeId,
        ExecutionContext context,
        Dictionary<(Guid, string), object?> outputCache,
        FlowDocument document)
    {
        _cts!.Token.ThrowIfCancellationRequested();

        // Find the node structure
        var node = document.Nodes.FirstOrDefault(n => n.InstanceId == nodeId);
        if (node == null || node.IsDisabled)
            return;

        // Prevent infinite stack recursion on loops that aren't properly managed
        if (!_currentlyExecutingNodes.Add(nodeId))
        {
            // Already executing in this stack branch (detected recursive dependency)
            return;
        }

        List<FlowConnection> flowConns = [];
        List<string> activeOutputs = [];
        NodeExecutionResult? result = null;

        try
        {
            // Wait if paused (Step mode releases the semaphore once)
            await _pauseSemaphore!.WaitAsync(_cts.Token);
            _pauseSemaphore.Release();

            // ─── Direct Orchestration for Structural Nodes ───
            if (node.TypeId == "logic.repeat")
            {
                await ExecuteRepeatLoopAsync(node, context, outputCache, document);
                return;
            }
            if (node.TypeId == "logic.foreach")
            {
                await ExecuteForEachLoopAsync(node, context, outputCache, document);
                return;
            }
            if (node.TypeId == "logic.for")
            {
                await ExecuteForLoopAsync(node, context, outputCache, document);
                return;
            }
            if (node.TypeId == "logic.while")
            {
                await ExecuteWhileLoopAsync(node, context, outputCache, document);
                return;
            }

            // ─── Standard Node Execution ───
            if (!_registry.TryGet(node.TypeId, out _, out var executor) || executor is null)
            {
                _eventBus.Publish(new NodeSkippedEvent(
                    document.Id, node.InstanceId, node.TypeId,
                    $"TypeId '{node.TypeId}' não registrado"));
                return;
            }

            _eventBus.Publish(new NodeStartedEvent(document.Id, node.InstanceId, node.TypeId));
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var resolvedInputs = ResolveInputs(node, document, outputCache);
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
                _eventBus.Publish(new NodeErrorEvent(document.Id, node.InstanceId, node.TypeId, ex));
                context.Log($"Erro em '{node.TypeId}': {ex.Message}", LogLevel.Error);
                throw;
            }

            sw.Stop();

            if (result.IsSuccess)
            {
                foreach (var kv in result.OutputValues)
                    outputCache[(node.InstanceId, kv.Key)] = kv.Value;

                _eventBus.Publish(new NodeCompletedEvent(
                    document.Id, node.InstanceId, node.TypeId,
                    sw.Elapsed, result.OutputValues));
            }
            else
            {
                var ex = result.Exception ?? new Exception(result.ErrorMessage);
                _eventBus.Publish(new NodeErrorEvent(document.Id, node.InstanceId, node.TypeId, ex));
                throw ex;
            }

            // Inline delays
            int inlineDelay = MacroKids.Core.Services.PinValueReader.GetInt(
                resolvedInputs, node.PinValues, "delay", 0);
            if (inlineDelay > 0)
                await Task.Delay(inlineDelay, _cts.Token);

            if (StepDelayMs > 0)
                await Task.Delay(StepDelayMs, _cts.Token);

            // Follow active flow output pins
            activeOutputs = result.OutputValues
                .Where(kv => kv.Value is bool b && b)
                .Select(kv => kv.Key)
                .ToList();

            flowConns = document.Connections.Where(c => c.SourceNodeId == node.InstanceId).ToList();
        }
        finally
        {
            _currentlyExecutingNodes.Remove(nodeId);
        }

        // Execute downstream connections outside of the recursion prevention lock
        foreach (var conn in flowConns)
        {
            // If the pin is active in the result, or if it is the "done" pin and no explicit boolean was returned
            bool isPinTriggered = activeOutputs.Contains(conn.SourcePinId) || 
                                 (conn.SourcePinId == "done" && result != null && !result.OutputValues.ContainsKey("done"));

            if (isPinTriggered)
            {
                await Task.Yield(); // Avoid deep synchronous stack trace on long flows
                await ExecuteBranchAsync(conn.TargetNodeId, context, outputCache, document);
            }
        }
    }

    /// <summary>
    /// Custom execution for logic.repeat that executes the inner loop branch X times.
    /// </summary>
    private async Task ExecuteRepeatLoopAsync(
        FlowNode node,
        ExecutionContext context,
        Dictionary<(Guid, string), object?> outputCache,
        FlowDocument document)
    {
        _eventBus.Publish(new NodeStartedEvent(document.Id, node.InstanceId, node.TypeId));
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var resolvedInputs = ResolveInputs(node, document, outputCache);
        int times = 5;
        object? rawTimes = null;
        if (resolvedInputs.TryGetValue("times", out var tVal))
            rawTimes = tVal;
        else if (node.PinValues.TryGetValue("times", out var st))
            rawTimes = st;

        if (rawTimes != null)
        {
            if (rawTimes is int iVal) times = iVal;
            else if (rawTimes is double dVal) times = (int)dVal;
            else int.TryParse(rawTimes.ToString(), out times);
        }

        context.Log($"Iniciando loop de repetição: {times} vezes");

        var loopConn = document.Connections.FirstOrDefault(c => c.SourceNodeId == node.InstanceId && c.SourcePinId == "loop");

        for (int i = 0; i < times; i++)
        {
            _cts!.Token.ThrowIfCancellationRequested();
            context.Log($"Loop - Iteração {i + 1} de {times}");
            _eventBus.Publish(new NodeStatusUpdatedEvent(document.Id, node.InstanceId, $"Iteração {i + 1} / {times}"));

            if (loopConn != null)
            {
                // Execute the loop body branch
                await ExecuteBranchAsync(loopConn.TargetNodeId, context, outputCache, document);
            }

            await Task.Delay(10, _cts.Token);
        }

        sw.Stop();
        var outputs = new Dictionary<string, object?> { ["done"] = true };
        _eventBus.Publish(new NodeCompletedEvent(document.Id, node.InstanceId, node.TypeId, sw.Elapsed, outputs));

        var doneConn = document.Connections.FirstOrDefault(c => c.SourceNodeId == node.InstanceId && c.SourcePinId == "done");
        if (doneConn != null)
        {
            await ExecuteBranchAsync(doneConn.TargetNodeId, context, outputCache, document);
        }
    }

    /// <summary>
    /// Custom execution for logic.foreach that iterates over items of a list.
    /// </summary>
    private async Task ExecuteForEachLoopAsync(
        FlowNode node,
        ExecutionContext context,
        Dictionary<(Guid, string), object?> outputCache,
        FlowDocument document)
    {
        _eventBus.Publish(new NodeStartedEvent(document.Id, node.InstanceId, node.TypeId));
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var resolvedInputs = ResolveInputs(node, document, outputCache);
        string listName = "myList";
        if (resolvedInputs.TryGetValue("list", out var lVal) && lVal is string rl)
            listName = rl;
        else if (node.PinValues.TryGetValue("list", out var sl) && sl is string slStr)
            listName = slStr;

        context.Log($"Iniciando loop For Each na lista: {listName}");

        System.Collections.IEnumerable enumerable;
        if (context.TryGetVariable(listName, out var listObj) && listObj is System.Collections.IEnumerable en)
        {
            enumerable = en;
        }
        else
        {
            context.Log($"Lista '{listName}' não encontrada. Usando lista temporária mock.");
            enumerable = new List<object> { "Item 1", "Item 2", "Item 3" };
        }

        var itemConn = document.Connections.FirstOrDefault(c => c.SourceNodeId == node.InstanceId && c.SourcePinId == "item");

        int index = 0;
        foreach (var item in enumerable)
        {
            _cts!.Token.ThrowIfCancellationRequested();
            context.Log($"For Each - Item [{index}]: {item}");
            _eventBus.Publish(new NodeStatusUpdatedEvent(document.Id, node.InstanceId, $"Item {index + 1}"));

            // Put current item value in output cache
            outputCache[(node.InstanceId, "item")] = item;

            if (itemConn != null)
            {
                await ExecuteBranchAsync(itemConn.TargetNodeId, context, outputCache, document);
            }

            index++;
            await Task.Delay(10, _cts.Token);
        }

        sw.Stop();
        var outputs = new Dictionary<string, object?> { ["done"] = true };
        _eventBus.Publish(new NodeCompletedEvent(document.Id, node.InstanceId, node.TypeId, sw.Elapsed, outputs));

        var doneConn = document.Connections.FirstOrDefault(c => c.SourceNodeId == node.InstanceId && c.SourcePinId == "done");
        if (doneConn != null)
        {
            await ExecuteBranchAsync(doneConn.TargetNodeId, context, outputCache, document);
        }
    }

    private async Task ExecuteForLoopAsync(
        FlowNode node,
        ExecutionContext context,
        Dictionary<(Guid, string), object?> outputCache,
        FlowDocument document)
    {
        _eventBus.Publish(new NodeStartedEvent(document.Id, node.InstanceId, node.TypeId));
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var resolvedInputs = ResolveInputs(node, document, outputCache);
        
        int start = 0;
        int end = 10;
        int step = 1;

        if (resolvedInputs.TryGetValue("start", out var sVal))
        {
            if (sVal is int si) start = si;
            else int.TryParse(sVal.ToString(), out start);
        }
        else if (node.PinValues.TryGetValue("start", out var sp))
        {
            if (sp is int si) start = si;
            else int.TryParse(sp.ToString(), out start);
        }

        if (resolvedInputs.TryGetValue("end", out var eVal))
        {
            if (eVal is int ei) end = ei;
            else int.TryParse(eVal.ToString(), out end);
        }
        else if (node.PinValues.TryGetValue("end", out var ep))
        {
            if (ep is int ei) end = ei;
            else int.TryParse(ep.ToString(), out end);
        }

        if (resolvedInputs.TryGetValue("step", out var stVal))
        {
            if (stVal is int sti) step = sti;
            else int.TryParse(stVal.ToString(), out step);
        }
        else if (node.PinValues.TryGetValue("step", out var stp))
        {
            if (stp is int sti) step = sti;
            else int.TryParse(stp.ToString(), out step);
        }

        context.Log($"Iniciando For Loop: de {start} até {end} (passo {step})");

        var loopConn = document.Connections.FirstOrDefault(c => c.SourceNodeId == node.InstanceId && c.SourcePinId == "loop");

        int index = start;
        while ((step > 0 && index < end) || (step < 0 && index > end))
        {
            _cts!.Token.ThrowIfCancellationRequested();
            context.Log($"For Loop - Índice: {index}");
            _eventBus.Publish(new NodeStatusUpdatedEvent(document.Id, node.InstanceId, $"Índice: {index}"));

            outputCache[(node.InstanceId, "index")] = index;

            if (loopConn != null)
            {
                await ExecuteBranchAsync(loopConn.TargetNodeId, context, outputCache, document);
            }

            index += step;
            await Task.Delay(10, _cts.Token);
        }

        sw.Stop();
        var outputs = new Dictionary<string, object?> { ["done"] = true };
        _eventBus.Publish(new NodeCompletedEvent(document.Id, node.InstanceId, node.TypeId, sw.Elapsed, outputs));

        var doneConn = document.Connections.FirstOrDefault(c => c.SourceNodeId == node.InstanceId && c.SourcePinId == "done");
        if (doneConn != null)
        {
            await ExecuteBranchAsync(doneConn.TargetNodeId, context, outputCache, document);
        }
    }

    private async Task ExecuteWhileLoopAsync(
        FlowNode node,
        ExecutionContext context,
        Dictionary<(Guid, string), object?> outputCache,
        FlowDocument document)
    {
        _eventBus.Publish(new NodeStartedEvent(document.Id, node.InstanceId, node.TypeId));
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var resolvedInputs = ResolveInputs(node, document, outputCache);
        string condition = "var < 10";

        if (resolvedInputs.TryGetValue("condition", out var cVal) && cVal is string rc)
            condition = rc;
        else if (node.PinValues.TryGetValue("condition", out var sc) && sc is string scStr)
            condition = scStr;

        context.Log($"Iniciando While Loop com a condição: {condition}");

        var loopConn = document.Connections.FirstOrDefault(c => c.SourceNodeId == node.InstanceId && c.SourcePinId == "loop");

        int iterations = 0;
        const int maxSafeIterations = 10000;

        while (EvaluateCondition(condition, context))
        {
            _cts!.Token.ThrowIfCancellationRequested();
            iterations++;

            if (iterations > maxSafeIterations)
            {
                context.Log("While Loop - Proteção de loop infinito ativada (máximo 10000 iterações)", LogLevel.Warning);
                break;
            }

            context.Log($"While Loop - Iteração {iterations}");

            if (loopConn != null)
            {
                await ExecuteBranchAsync(loopConn.TargetNodeId, context, outputCache, document);
            }

            await Task.Delay(10, _cts.Token);
        }

        sw.Stop();
        var outputs = new Dictionary<string, object?> { ["done"] = true };
        _eventBus.Publish(new NodeCompletedEvent(document.Id, node.InstanceId, node.TypeId, sw.Elapsed, outputs));

        var doneConn = document.Connections.FirstOrDefault(c => c.SourceNodeId == node.InstanceId && c.SourcePinId == "done");
        if (doneConn != null)
        {
            await ExecuteBranchAsync(doneConn.TargetNodeId, context, outputCache, document);
        }
    }

    /// <summary>Stop execution immediately.</summary>
    public void Stop()
    {
        if (IsPaused)
            Resume();

        _cts?.Cancel();
    }

    /// <summary>Pause execution after the current node completes.</summary>
    public void Pause()
    {
        if (!IsRunning || IsPaused || _pauseSemaphore is null)
            return;

        _pauseSemaphore.Wait(0); // acquire — next WaitAsync in ExecuteBranchAsync will block
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

    private static IReadOnlyDictionary<string, object?> ResolveInputs(
        FlowNode node,
        FlowDocument document,
        IReadOnlyDictionary<(Guid, string), object?> outputCache)
    {
        var resolved = new Dictionary<string, object?>();

        foreach (var kv in node.PinValues)
        {
            resolved[kv.Key] = UnboxJsonElement(kv.Value);
        }

        foreach (var conn in document.Connections.Where(c => c.TargetNodeId == node.InstanceId))
        {
            var key = (conn.SourceNodeId, conn.SourcePinId);
            if (outputCache.TryGetValue(key, out var upstreamValue))
                resolved[conn.TargetPinId] = UnboxJsonElement(upstreamValue);
        }

        return resolved;
    }

    private static object? UnboxJsonElement(object? value) => PinValueReader.Unbox(value);

    private static bool EvaluateCondition(string condition, ExecutionContext context)
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
