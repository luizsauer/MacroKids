namespace MacroKids.Core.Commands;

/// <summary>
/// Base contract for all reversible editor operations.
/// Every canvas action (create, delete, move, connect) must be encapsulated here
/// so that Undo/Redo works consistently across the entire editor.
/// </summary>
public interface IEditorCommand
{
    /// <summary>Human-readable description shown in the Undo/Redo menu.</summary>
    string Description { get; }

    /// <summary>Execute (or re-execute after an Undo) the operation.</summary>
    void Execute();

    /// <summary>Reverse the effect of <see cref="Execute"/>.</summary>
    void Undo();
}
