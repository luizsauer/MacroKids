using System.Windows;
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

    public Point GetInputPinPoint(string pinId)
    {
        // Estimate vertical position of the input pin based on its index using LINQ conversion
        int index = Metadata.Inputs.ToList().FindIndex(p => p.Id == pinId);
        if (index < 0) index = 0;

        // Offset based on template header (approx 36px) + margin spaces (approx 26px each)
        double posY = Y + 48 + (index * 26);
        return new Point(X + 5, posY);
    }

    public Point GetOutputPinPoint(string pinId)
    {
        // Estimate vertical position of the output pin based on its index using LINQ conversion
        int index = Metadata.Outputs.ToList().FindIndex(p => p.Id == pinId);
        if (index < 0) index = 0;

        // Visual header + input area offset + output index spacing
        double inputOffset = Metadata.Inputs.Count() * 26;
        double posY = Y + 48 + inputOffset + (index * 26);
        return new Point(X + 185, posY);
    }
}
