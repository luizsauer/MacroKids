using CommunityToolkit.Mvvm.ComponentModel;
using MacroKids.Core.Commands;
using MacroKids.Core.Interfaces;
using MacroKids.NodeEditor.ViewModels;

namespace MacroKids.UI.ViewModels;

public sealed partial class ProjectPageViewModel : ObservableObject
{
    [ObservableProperty] private string _title;
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private bool _canClose = true;

    public NodeCanvasViewModel CanvasViewModel { get; }

    public ProjectPageViewModel(INodeRegistry registry, string title, bool canClose = true)
    {
        _title = title;
        _canClose = canClose;
        CanvasViewModel = new NodeCanvasViewModel(registry, new CommandHistory());
    }
}
