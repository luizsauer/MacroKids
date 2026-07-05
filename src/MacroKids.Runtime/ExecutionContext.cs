using MacroKids.Core.Events;
using MacroKids.Core.Interfaces;

namespace MacroKids.Runtime;

/// <summary>
/// Concrete execution context passed to every node during a flow run.
/// Holds the variable dictionary, provides logging via the event bus,
/// and exposes the cancellation token from the executor.
/// </summary>
public sealed class ExecutionContext : IExecutionContext
{
    private readonly Dictionary<string, object?> _variables = [];
    private readonly object _variablesLock = new();

    public ExecutionContext(IEventBus eventBus, CancellationToken cancellationToken)
    {
        EventBus          = eventBus;
        CancellationToken = cancellationToken;
    }

    // ── IExecutionContext ────────────────────────────────────────────────────

    public CancellationToken CancellationToken { get; }
    public IEventBus EventBus { get; }

    public IReadOnlyDictionary<string, object?> AllVariables
    {
        get
        {
            lock (_variablesLock)
                return new Dictionary<string, object?>(_variables);
        }
    }

    // ── Variables ─────────────────────────────────────────────────────────────

    public void SetVariable(string name, object? value)
    {
        object? oldValue;
        lock (_variablesLock)
        {
            _variables.TryGetValue(name, out oldValue);
            _variables[name] = value;
        }

        EventBus.Publish(new VariableChangedEvent(name, oldValue, value));
    }

    public object? GetVariable(string name)
    {
        lock (_variablesLock)
            return _variables.TryGetValue(name, out var val) ? val : null;
    }

    public bool TryGetVariable(string name, out object? value)
    {
        lock (_variablesLock)
            return _variables.TryGetValue(name, out value);
    }

    // ── Logging ───────────────────────────────────────────────────────────────

    public void Log(string message, LogLevel level = LogLevel.Info) =>
        EventBus.Publish(new LogMessageEvent(message, level, DateTime.UtcNow));
}
