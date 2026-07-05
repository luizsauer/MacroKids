namespace MacroKids.Core.Models;

/// <summary>
/// Represents a directed wire connecting an output pin of one node to an input pin of another.
/// </summary>
public class FlowConnection
{
    public required Guid Id { get; init; }

    public required Guid SourceNodeId { get; init; }
    public required string SourcePinId { get; init; }

    public required Guid TargetNodeId { get; init; }
    public required string TargetPinId { get; init; }
}
