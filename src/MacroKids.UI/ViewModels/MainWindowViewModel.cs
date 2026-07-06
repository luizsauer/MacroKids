using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacroKids.Core.Commands;
using MacroKids.Core.Interfaces;
using MacroKids.Core.Models;
using MacroKids.NodeEditor.ViewModels;
using MacroKids.Nodes.Flow;
using MacroKids.Nodes.Keyboard;
using MacroKids.Nodes.Mouse;
using MacroKids.Runtime;

namespace MacroKids.UI.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly INodeRegistry _nodeRegistry;
    private readonly CommandHistory _commandHistory;

    [ObservableProperty] private string _windowTitle = "MacroKids - v0.1.1 (Dev)";
    [ObservableProperty] private string _statusMessage = "Pronto";
    
    public NodeCanvasViewModel CanvasViewModel { get; }
    public ObservableCollection<NodeMetadata> AvailableNodes { get; } = [];

    public MainWindowViewModel()
    {
        var registry = new NodeRegistry();
        
        // Register Real Nodes
        registry.Register(MoveMouseMetadata.Instance, new MoveMouseExecutor());
        registry.Register(LeftClickMetadata.Instance, new LeftClickExecutor());
        registry.Register(RightClickMetadata.Instance, new RightClickExecutor());
        registry.Register(PressKeyMetadata.Instance, new PressKeyExecutor());
        registry.Register(TypeTextMetadata.Instance, new TypeTextExecutor());
        registry.Register(WaitMetadata.Instance, new WaitExecutor());

        _nodeRegistry = registry;
        _commandHistory = new CommandHistory();
        
        CanvasViewModel = new NodeCanvasViewModel(_nodeRegistry, _commandHistory);

        // Load nodes into catalog
        foreach (var nodeMeta in _nodeRegistry.GetAll())
        {
            AvailableNodes.Add(nodeMeta);
        }

        // Add start templates to visual editor
        CanvasViewModel.AddNode("mouse.move", 100, 100);
        CanvasViewModel.AddNode("mouse.left_click", 350, 150);
        CanvasViewModel.AddNode("flow.wait", 100, 300);
    }

    [RelayCommand]
    private void AddNodeToCanvas(NodeMetadata metadata)
    {
        if (metadata != null)
        {
            CanvasViewModel.AddNode(metadata.TypeId, 150, 150);
            StatusMessage = $"Adicionado bloco: {metadata.Name}";
        }
    }
}
