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

    // Get color brush dynamically matching source node category
    public System.Windows.Media.Brush StrokeBrush
    {
        get
        {
            if (IsAnimating) return System.Windows.Media.Brushes.Yellow;

            return _sourceNode.Metadata.Category switch
            {
                NodeCategory.Keyboard => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x25, 0x63, 0xEB)),
                NodeCategory.Mouse => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x16, 0xA3, 0x4A)),
                NodeCategory.Loops => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x15, 0x80, 0x3D)),
                NodeCategory.Events => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xDB, 0x27, 0x77)),
                _ => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x7C, 0x3A, 0xED))
            };
        }
    }

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
