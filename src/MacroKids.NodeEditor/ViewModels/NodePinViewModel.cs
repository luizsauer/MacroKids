using CommunityToolkit.Mvvm.ComponentModel;
using MacroKids.Core.Models;

namespace MacroKids.NodeEditor.ViewModels;

public sealed partial class NodePinViewModel : ObservableObject
{
    private readonly NodeViewModel _node;

    public NodePin Pin { get; }

    public string Id => Pin.Id;
    public string Label => Pin.Label;
    public PinDirection Direction => Pin.Direction;
    public bool IsFlowPin => Pin.IsFlowPin;

    public NodePinViewModel(NodeViewModel node, NodePin pin)
    {
        _node = node;
        Pin = pin;
        _node.PropertyChanged += Node_PropertyChanged;
    }

    public object? Value
    {
        get => _node[Pin.Id];
        set
        {
            _node[Pin.Id] = value;
            OnPropertyChanged();
        }
    }

    private void Node_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "Item[]" || e.PropertyName == nameof(NodeViewModel.PinValues))
            OnPropertyChanged(nameof(Value));
    }
}
