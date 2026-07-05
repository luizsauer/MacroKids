namespace MacroKids.Core.Interfaces;

/// <summary>
/// Execution context passed to every node during a flow run.
/// Provides access to variables, event publishing and cancellation.
/// </summary>
public interface IExecutionContext
{
    /// <summary>Token that signals the user pressed Stop.</summary>
    CancellationToken CancellationToken { get; }

    // ── Variables ─────────────────────────────────────────────────────────────
    void SetVariable(string name, object? value);
    object? GetVariable(string name);
    bool TryGetVariable(string name, out object? value);
    IReadOnlyDictionary<string, object?> AllVariables { get; }

    // ── Logging ───────────────────────────────────────────────────────────────
    void Log(string message, LogLevel level = LogLevel.Info);

    // ── Events ────────────────────────────────────────────────────────────────
    IEventBus EventBus { get; }
}

/// <summary>Log severity levels for execution output.</summary>
public enum LogLevel { Debug, Info, Warning, Error }
