namespace MacroKids.Core.Commands;

/// <summary>
/// Manages the Undo/Redo history for all canvas operations.
/// Uses two stacks: one for past commands (undo) and one for undone commands (redo).
/// Any new command execution clears the redo stack.
/// </summary>
public sealed class CommandHistory
{
    private readonly Stack<IEditorCommand> _undoStack = new();
    private readonly Stack<IEditorCommand> _redoStack = new();
    private readonly int _maxHistorySize;

    public CommandHistory(int maxHistorySize = 100)
    {
        _maxHistorySize = maxHistorySize;
    }

    // ── State ────────────────────────────────────────────────────────────────

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public string? NextUndoDescription => CanUndo ? _undoStack.Peek().Description : null;
    public string? NextRedoDescription => CanRedo ? _redoStack.Peek().Description : null;

    // ── Events ────────────────────────────────────────────────────────────────

    public event EventHandler? HistoryChanged;

    // ── Operations ───────────────────────────────────────────────────────────

    /// <summary>
    /// Execute a command and push it onto the undo stack.
    /// Clears the redo stack — branching history is not supported.
    /// </summary>
    public void Execute(IEditorCommand command)
    {
        command.Execute();

        _redoStack.Clear();
        _undoStack.Push(command);

        // Trim history if it exceeds the limit
        if (_undoStack.Count > _maxHistorySize)
            TrimUndoStack();

        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Undo the most recent command.</summary>
    public void Undo()
    {
        if (!CanUndo)
            return;

        var command = _undoStack.Pop();
        command.Undo();
        _redoStack.Push(command);
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Re-execute the most recently undone command.</summary>
    public void Redo()
    {
        if (!CanRedo)
            return;

        var command = _redoStack.Pop();
        command.Execute();
        _undoStack.Push(command);
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Clear all history (e.g., after loading a new document).</summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private void TrimUndoStack()
    {
        // Rebuild the stack keeping only the most recent _maxHistorySize items
        var items = _undoStack.ToArray();
        _undoStack.Clear();
        foreach (var item in items.Take(_maxHistorySize).Reverse())
            _undoStack.Push(item);
    }
}
