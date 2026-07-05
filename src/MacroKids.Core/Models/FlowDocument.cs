namespace MacroKids.Core.Models;

/// <summary>
/// The root document of a MacroKids project. Saved as project.json inside a .mkproject archive.
/// </summary>
public class FlowDocument
{
    // ── Identity ──────────────────────────────────────────────────────────────
    public required Guid Id { get; init; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public required DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; set; }

    // ── Versioning (GPT suggestion #12) ──────────────────────────────────────
    /// <summary>Schema version of this document format. Used for migrations.</summary>
    public int SchemaVersion { get; init; } = 1;

    /// <summary>Engine version that created this document (e.g., "1.0.0").</summary>
    public required string EngineVersion { get; init; }

    /// <summary>Minimum engine version required to open this document.</summary>
    public string MinimumEngineVersion { get; init; } = "1.0.0";

    // ── Graph ─────────────────────────────────────────────────────────────────
    public List<FlowNode> Nodes { get; init; } = [];
    public List<FlowConnection> Connections { get; init; } = [];

    // ── Canvas State ──────────────────────────────────────────────────────────
    /// <summary>Saved canvas viewport (zoom + pan offset) for restoring the view on open.</summary>
    public double CanvasOffsetX { get; set; }
    public double CanvasOffsetY { get; set; }
    public double CanvasZoom { get; set; } = 1.0;
}
