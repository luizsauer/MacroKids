using CommunityToolkit.Mvvm.ComponentModel;
using MacroKids.Core.Models;

namespace MacroKids.NodeEditor.ViewModels;

public sealed partial class NodePinViewModel : ObservableObject
{
    private readonly NodeViewModel _node;

    public NodePin Pin { get; }

    public string Id        => Pin.Id;
    public string Label     => MacroKids.Core.Translator.Get($"Pin_{_node.Metadata.TypeId}_{Pin.Id}_Label", Pin.Label);
    public PinDirection Direction => Pin.Direction;
    public bool IsFlowPin   => Pin.IsFlowPin;

    // ── Input type helpers ────────────────────────────────────────────────────
    public PinInputType InputType   => Pin.InputType;
    public IReadOnlyList<string> Options
    {
        get
        {
            if (Pin.InputType == PinInputType.Dropdown && (Pin.Id == "list" || Pin.Id == "name"))
            {
                var vars = MacroKids.Core.Translator.GetDeclaredVariables().ToList();
                var currentVal = Value?.ToString();
                if (!string.IsNullOrEmpty(currentVal) && !vars.Contains(currentVal))
                {
                    vars.Add(currentVal);
                }

                if (vars.Count == 0)
                {
                    return new List<string> { Pin.DefaultValue?.ToString() ?? "myVar" };
                }
                return vars;
            }
            return Pin.Options;
        }
    }

    public bool IsTextInput  => Pin.InputType == PinInputType.Text;
    public bool IsDropdown   => Pin.InputType == PinInputType.Dropdown;
    public bool IsKeyCapture => Pin.InputType == PinInputType.KeyCapture;

    public NodePinViewModel(NodeViewModel node, NodePin pin)
    {
        _node = node;
        Pin = pin;
        _node.PropertyChanged += Node_PropertyChanged;
        MacroKids.Core.Translator.TranslationChanged += Translator_TranslationChanged;
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

    /// <summary>
    /// String representation for TextBox binding — handles int/double/string transparently.
    /// </summary>
    public string ValueText
    {
        get => _node[Pin.Id]?.ToString() ?? string.Empty;
        set
        {
            // Try to preserve the type: int, double, or fall back to string
            if (Pin.DataType == typeof(int) && int.TryParse(value, out int i))
                _node[Pin.Id] = i;
            else if (Pin.DataType == typeof(double) && double.TryParse(value, out double d))
                _node[Pin.Id] = d;
            else
                _node[Pin.Id] = value;

            OnPropertyChanged(nameof(Value));
            OnPropertyChanged();
        }
    }

    private void Node_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "Item[]" || e.PropertyName == nameof(NodeViewModel.PinValues))
        {
            OnPropertyChanged(nameof(Value));
            OnPropertyChanged(nameof(ValueText));
        }
    }

    private void Translator_TranslationChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(Label));
    }
}
