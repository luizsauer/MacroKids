namespace MacroKids.Core.Models;

/// <summary>
/// Immutable descriptor that defines everything the UI needs to render and interact with a node type.
/// Node implementations decorate themselves (or use a factory) to provide this metadata.
/// The UI NEVER has direct knowledge of a specific node type — it only reads NodeMetadata.
/// </summary>
public class NodeMetadata
{
    /// <summary>Unique type identifier (e.g., "keyboard.press_key"). Must be stable across versions.</summary>
    public required string TypeId { get; init; }

    /// <summary>Friendly display name shown in the palette and on the node header.</summary>
    public required string Name { get; init; }

    /// <summary>Short description shown in the palette tooltip.</summary>
    public required string Description { get; init; }

    /// <summary>Category used for grouping and color-coding in the sidebar.</summary>
    public required NodeCategory Category { get; init; }

    /// <summary>
    /// Icon identifier. Convention: use Material Design icon names (e.g., "keyboard", "mouse").
    /// The UI resolves icons from the resource dictionary using this key.
    /// </summary>
    public required string IconKey { get; init; }

    /// <summary>
    /// Optional override hex color. When null, the UI uses the category default color.
    /// </summary>
    public string? ColorOverride { get; init; }

    /// <summary>All input and output pins, in the order they should appear on the node.</summary>
    public required IReadOnlyList<NodePin> Pins { get; init; }

    /// <summary>Optional URL to open when the user clicks the "Help" button on the node.</summary>
    public string? HelpUrl { get; init; }

    /// <summary>Semantic version of this node type. Used for migration when loading older projects.</summary>
    public required Version NodeVersion { get; init; }

    /// <summary>Author name — "MacroKids" for built-in nodes, plugin author for extensions.</summary>
    public string Author { get; init; } = "MacroKids";

    // Convenience helpers
    public IEnumerable<NodePin> Inputs  => Pins.Where(p => p.Direction == PinDirection.Input);
    public IEnumerable<NodePin> Outputs => Pins.Where(p => p.Direction == PinDirection.Output);
}
