using CommunityToolkit.Mvvm.ComponentModel;
using MacroKids.Core.Models;

namespace MacroKids.NodeEditor.ViewModels;

/// <summary>
/// Observable ViewModel for a single node instance on the canvas.
/// Bridges <see cref="FlowNode"/> (data) and <see cref="NodeMetadata"/> (type descriptor)
/// into a form the canvas control can bind to and animate.
/// </summary>
public sealed partial class NodeViewModel : ObservableObject
{
    // ── Identity ─────────────────────────────────────────────────────────────
    public Guid InstanceId { get; }
    public NodeMetadata Metadata { get; }

    // ── Position ─────────────────────────────────────────────────────────────
    [ObservableProperty] private double _x;
    [ObservableProperty] private double _y;

    // ── State ─────────────────────────────────────────────────────────────────
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private bool _isExecuting;   // glows green while engine runs it
    [ObservableProperty] private bool _hasError;      // glows red after NodeErrorEvent
    [ObservableProperty] private bool _isDisabled;
    [ObservableProperty] private string? _comment;

    /// <summary>
    /// Static pin values (editable inline on the node).
    /// Connected pins override these at runtime via upstream outputs.
    /// </summary>
    public Dictionary<string, object?> PinValues { get; } = [];

    public NodeViewModel(FlowNode node, NodeMetadata metadata)
    {
        InstanceId = node.InstanceId;
        Metadata   = metadata;
        X          = node.X;
        Y          = node.Y;
        IsDisabled = node.IsDisabled;
        Comment    = node.Comment;

        foreach (var kv in node.PinValues)
            PinValues[kv.Key] = kv.Value;
    }

    /// <summary>Project the current VM state back into a <see cref="FlowNode"/> for serialization.</summary>
    public FlowNode ToFlowNode() => new()
    {
        InstanceId = InstanceId,
        TypeId     = Metadata.TypeId,
        X          = X,
        Y          = Y,
        IsDisabled = IsDisabled,
        Comment    = Comment
    };
}
