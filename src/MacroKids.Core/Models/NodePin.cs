namespace MacroKids.Core.Models;

/// <summary>
/// Defines the direction of a pin on a node.
/// </summary>
public enum PinDirection
{
    Input,
    Output
}

/// <summary>
/// Represents a single connection point (port) on a node — either an input or an output.
/// The UI uses this to render the pin dot and label; it never hard-codes per-node knowledge.
/// </summary>
public class NodePin
{
    /// <summary>Unique identifier within the node (e.g., "x", "y", "completed").</summary>
    public required string Id { get; init; }

    /// <summary>Human-readable label shown next to the pin.</summary>
    public required string Label { get; init; }

    /// <summary>Whether this is an input or output pin.</summary>
    public required PinDirection Direction { get; init; }

    /// <summary>The CLR type expected/produced by this pin (e.g., typeof(int), typeof(string)).</summary>
    public required Type DataType { get; init; }

    /// <summary>Optional default value when no connection is made to an input pin.</summary>
    public object? DefaultValue { get; init; }

    /// <summary>Whether this pin must be connected for the node to execute.</summary>
    public bool IsRequired { get; init; } = false;

    /// <summary>Optional tooltip shown in the UI.</summary>
    public string? Tooltip { get; init; }

    /// <summary>
    /// True for execution/flow-control pins (Id is "in" or "done" with DataType bool).
    /// These are rendered as side connectors and are not shown as inline textbox fields.
    /// </summary>
    public bool IsFlowPin => DataType == typeof(bool) && (Id == "in" || Id == "done");
}
