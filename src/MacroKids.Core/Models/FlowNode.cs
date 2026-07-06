namespace MacroKids.Core.Models;

/// <summary>
/// A concrete instance of a node placed on the canvas.
/// Stores position, configuration values and runtime state — not execution logic.
/// </summary>
public class FlowNode
{
    /// <summary>Unique id of this instance on the canvas (GUID).</summary>
    public required Guid InstanceId { get; init; }

    /// <summary>
    /// The type identifier that links this instance to its registered <see cref="NodeMetadata"/>.
    /// Resolved at runtime via the node registry.
    /// </summary>
    public required string TypeId { get; init; }

    /// <summary>Position on the infinite canvas (in canvas units).</summary>
    public double X { get; set; }
    public double Y { get; set; }

    /// <summary>
    /// Serialized configuration values keyed by pin Id (only for input pins with static values).
    /// When a pin is connected to another node's output, this value is ignored at runtime.
    /// </summary>
    public Dictionary<string, object?> PinValues { get; set; } = [];

    /// <summary>Optional user-set label/comment displayed above the node.</summary>
    public string? Comment { get; set; }

    /// <summary>Whether the node is disabled (skipped during execution).</summary>
    public bool IsDisabled { get; set; } = false;
}
