using System.Collections.ObjectModel;
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

    public ObservableCollection<NodePinViewModel> InputPins { get; } = [];
    public ObservableCollection<NodePinViewModel> OutputPins { get; } = [];

    /// <summary>Only execution/flow input pins (Id="in", DataType=bool). Shown as side connectors.</summary>
    public ObservableCollection<NodePinViewModel> FlowInputPins { get; } = [];

    /// <summary>Data input pins (parameters). Shown as inline label+textbox rows.</summary>
    public ObservableCollection<NodePinViewModel> DataInputPins { get; } = [];

    /// <summary>Only execution/flow output pins (Id="done", DataType=bool). Shown as side connectors.</summary>
    public ObservableCollection<NodePinViewModel> FlowOutputPins { get; } = [];

    public string DisplayName => MacroKids.Core.Translator.Get($"Node_{Metadata.TypeId}_Name", Metadata.Name);
    public string DisplayDescription => MacroKids.Core.Translator.Get($"Node_{Metadata.TypeId}_Desc", Metadata.Description);

    // Helper property to enable direct two-way binding: {Binding Parameters[ms]}
    public object? this[string pinId]
    {
        get => PinValues.TryGetValue(pinId, out var val) ? val : null;
        set
        {
            PinValues[pinId] = value;
            OnPropertyChanged("Item[]");
        }
    }

    public NodeViewModel(FlowNode node, NodeMetadata metadata)
    {
        MacroKids.Core.Translator.TranslationChanged += Translator_TranslationChanged;

        InstanceId = node.InstanceId;
        Metadata   = metadata;
        X          = node.X;
        Y          = node.Y;
        IsDisabled = node.IsDisabled;
        Comment    = node.Comment;

        foreach (var kv in node.PinValues)
            PinValues[kv.Key] = kv.Value;
            
        // Populate default values from pins metadata if not set
        foreach (var pin in metadata.Pins.Where(p => p.Direction == PinDirection.Input))
        {
            if (!PinValues.ContainsKey(pin.Id))
                PinValues[pin.Id] = pin.DefaultValue;

            var pinVm = new NodePinViewModel(this, pin);
            InputPins.Add(pinVm);
            if (pin.IsFlowPin)
                FlowInputPins.Add(pinVm);
            else
                DataInputPins.Add(pinVm);
        }

        // Add a default delay pin to every node instance so users don't need a separate wait block
        if (!PinValues.ContainsKey("delay"))
            PinValues["delay"] = 100; // Default 100ms delay between blocks

        var delayPin = new NodePin { Id = "delay", Label = "Delay (ms)", Direction = PinDirection.Input, DataType = typeof(int), DefaultValue = 100 };
        DataInputPins.Add(new NodePinViewModel(this, delayPin));

        foreach (var pin in metadata.Outputs)
        {
            var pinVm = new NodePinViewModel(this, pin);
            OutputPins.Add(pinVm);
            if (pin.IsFlowPin)
                FlowOutputPins.Add(pinVm);
        }
    }

    /// <summary>Project the current VM state back into a <see cref="FlowNode"/> for serialization.</summary>
    public FlowNode ToFlowNode() => new()
    {
        InstanceId = InstanceId,
        TypeId     = Metadata.TypeId,
        X          = X,
        Y          = Y,
        PinValues  = new Dictionary<string, object?>(PinValues),
        Comment    = Comment,
        IsDisabled = IsDisabled,
    };

    // ── Pin position helpers ──────────────────────────────────────────────────

    /// <summary>
    /// Approximate vertical center of the node block.
    /// Border has Margin=5, Grid inside has Margin="14,10".
    /// Each data row is ~30px. Title row is ~22px.
    /// </summary>
    private double NodeCenterY => Y + 5 + (42.0 + DataInputPins.Count * 30.0) / 2.0;

    public Point GetInputPinPoint(string pinId)
    {
        // Left flow pin center: X+7 (pin projects slightly outside left border at X+5)
        return new Point(X + 7, NodeCenterY);
    }

    public Point GetOutputPinPoint(string pinId)
    {
        // Right flow pin center: X+218 (pin projects slightly outside right border at X+220)
        return new Point(X + 218, NodeCenterY);
    }

    private void Translator_TranslationChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(DisplayDescription));
    }
}
