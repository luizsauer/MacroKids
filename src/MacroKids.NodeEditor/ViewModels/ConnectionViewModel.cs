using System.Windows;
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

    private readonly NodeViewModel _sourceNode;
    private readonly NodeViewModel _targetNode;

    public Point StartPoint => _sourceNode.GetOutputPinPoint(SourcePinId);
    public Point EndPoint => _targetNode.GetInputPinPoint(TargetPinId);

    public ConnectionViewModel(FlowConnection connection, NodeViewModel sourceNode, NodeViewModel targetNode)
    {
        ConnectionId = connection.Id;
        SourceNodeId = connection.SourceNodeId;
        SourcePinId  = connection.SourcePinId;
        TargetNodeId = connection.TargetNodeId;
        TargetPinId  = connection.TargetPinId;
        _sourceNode  = sourceNode;
        _targetNode  = targetNode;

        // Listen to node movements to re-calculate Bezier endpoints dynamically
        _sourceNode.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(NodeViewModel.X) || e.PropertyName == nameof(NodeViewModel.Y)) OnPropertyChanged(nameof(StartPoint)); };
        _targetNode.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(NodeViewModel.X) || e.PropertyName == nameof(NodeViewModel.Y)) OnPropertyChanged(nameof(EndPoint)); };
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
