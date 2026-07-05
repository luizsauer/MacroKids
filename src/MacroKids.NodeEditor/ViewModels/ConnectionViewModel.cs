using CommunityToolkit.Mvvm.ComponentModel;
using MacroKids.Core.Models;

namespace MacroKids.NodeEditor.ViewModels;

/// <summary>
/// Observable ViewModel for a single wire (connection) between two node pins.
/// Holds references to the source and target ViewModels so the canvas
/// can compute Bézier control points from live X/Y positions.
/// </summary>
public sealed partial class ConnectionViewModel : ObservableObject
{
    public Guid ConnectionId  { get; }
    public Guid SourceNodeId  { get; }
    public string SourcePinId { get; }
    public Guid TargetNodeId  { get; }
    public string TargetPinId { get; }

    [ObservableProperty] private bool _isAnimating; // true while flow is executing through this wire

    public ConnectionViewModel(FlowConnection connection)
    {
        ConnectionId = connection.Id;
        SourceNodeId = connection.SourceNodeId;
        SourcePinId  = connection.SourcePinId;
        TargetNodeId = connection.TargetNodeId;
        TargetPinId  = connection.TargetPinId;
    }

    public FlowConnection ToFlowConnection() => new()
    {
        Id           = ConnectionId,
        SourceNodeId = SourceNodeId,
        SourcePinId  = SourcePinId,
        TargetNodeId = TargetNodeId,
        TargetPinId  = TargetPinId
    };
}
