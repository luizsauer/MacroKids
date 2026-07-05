using MacroKids.Core.Interfaces;
using MacroKids.Core.Models;

namespace MacroKids.Core.Events;

// ── Execution lifecycle events (published by FlowExecutor) ───────────────────

public record ExecutionStartedEvent(Guid FlowId, DateTime StartedAt)
    : IExecutionEvent;

public record ExecutionCompletedEvent(Guid FlowId, TimeSpan Duration)
    : IExecutionEvent;

public record ExecutionStoppedByUserEvent(Guid FlowId)
    : IExecutionEvent;

public record ExecutionFailedEvent(Guid FlowId, Exception Error)
    : IExecutionEvent;

// ── Node-level events ────────────────────────────────────────────────────────

public record NodeStartedEvent(Guid FlowId, Guid NodeInstanceId, string TypeId)
    : IExecutionEvent;

public record NodeCompletedEvent(
    Guid FlowId,
    Guid NodeInstanceId,
    string TypeId,
    TimeSpan Duration,
    IReadOnlyDictionary<string, object?> OutputValues)
    : IExecutionEvent;

public record NodeSkippedEvent(Guid FlowId, Guid NodeInstanceId, string TypeId, string Reason)
    : IExecutionEvent;

public record NodeErrorEvent(Guid FlowId, Guid NodeInstanceId, string TypeId, Exception Error)
    : IExecutionEvent;

// ── Variable events ───────────────────────────────────────────────────────────

public record VariableChangedEvent(string Name, object? OldValue, object? NewValue)
    : IExecutionEvent;

// ── Debug / step events ───────────────────────────────────────────────────────

public record ExecutionPausedEvent(Guid FlowId, Guid AtNodeInstanceId)
    : IExecutionEvent;

public record ExecutionResumedEvent(Guid FlowId)
    : IExecutionEvent;

public record LogMessageEvent(string Message, LogLevel Level, DateTime Timestamp)
    : IExecutionEvent;
