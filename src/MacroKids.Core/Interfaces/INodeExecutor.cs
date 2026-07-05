using MacroKids.Core.Models;

namespace MacroKids.Core.Interfaces;

/// <summary>
/// Result produced by a node after execution.
/// Carries the output pin values and execution status.
/// </summary>
public sealed class NodeExecutionResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public Exception? Exception { get; init; }

    /// <summary>
    /// Values produced by output pins, keyed by pin Id.
    /// Passed by the executor to downstream nodes.
    /// </summary>
    public IReadOnlyDictionary<string, object?> OutputValues { get; init; }
        = new Dictionary<string, object?>();

    public static NodeExecutionResult Success(
        Dictionary<string, object?>? outputs = null) =>
        new()
        {
            IsSuccess    = true,
            OutputValues = outputs ?? new Dictionary<string, object?>()
        };

    public static NodeExecutionResult Failure(string message, Exception? ex = null) =>
        new()
        {
            IsSuccess    = false,
            ErrorMessage = message,
            Exception    = ex
        };

    public static NodeExecutionResult Failure(Exception ex) =>
        Failure(ex.Message, ex);
}

/// <summary>
/// Contract that every executable node must implement.
/// Responsible solely for the logic of a single node — no canvas or UI knowledge.
/// </summary>
public interface INodeExecutor
{
    /// <summary>
    /// Execute the node's action asynchronously.
    /// </summary>
    /// <param name="node">The canvas instance (holds static pin values and position).</param>
    /// <param name="context">
    /// Execution context providing variables, logging, cancellation and event publishing.
    /// </param>
    /// <param name="resolvedInputs">
    /// Input pin values already resolved by the engine (static values + upstream outputs merged).
    /// </param>
    Task<NodeExecutionResult> ExecuteAsync(
        FlowNode node,
        IExecutionContext context,
        IReadOnlyDictionary<string, object?> resolvedInputs);
}
