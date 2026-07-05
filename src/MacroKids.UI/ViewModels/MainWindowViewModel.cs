using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacroKids.Core.Commands;
using MacroKids.Core.Interfaces;
using MacroKids.Core.Models;
using MacroKids.NodeEditor.ViewModels;

namespace MacroKids.UI.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly INodeRegistry _nodeRegistry;
    private readonly CommandHistory _commandHistory;

    [ObservableProperty] private string _windowTitle = "MacroKids - v0.1.0 (Dev)";
    [ObservableProperty] private string _statusMessage = "Pronto";
    
    public NodeCanvasViewModel CanvasViewModel { get; }
    public ObservableCollection<NodeMetadata> AvailableNodes { get; } = [];

    public MainWindowViewModel()
    {
        // Setup simple demo configurations
        _nodeRegistry = new DemoNodeRegistry();
        _commandHistory = new CommandHistory();
        
        CanvasViewModel = new NodeCanvasViewModel(_nodeRegistry, _commandHistory);

        // Load dummy node categories in the sidebar
        foreach (var nodeMeta in _nodeRegistry.GetAll())
        {
            AvailableNodes.Add(nodeMeta);
        }

        // Add some startup node samples to canvas so we don't start blank
        CanvasViewModel.AddNode("keyboard.press_key", 100, 100);
        CanvasViewModel.AddNode("mouse.left_click", 350, 150);
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

    // A dummy registry inside UI just to bootstrap visual editor without full Nodes project linked
    private sealed class DemoNodeRegistry : INodeRegistry
    {
        private readonly List<NodeMetadata> _list = [];

        public DemoNodeRegistry()
        {
            _list.Add(new NodeMetadata
            {
                TypeId = "keyboard.press_key",
                Name = "Press Key",
                Description = "Simula o pressionar de uma tecla do teclado",
                Category = NodeCategory.Keyboard,
                IconKey = "Keyboard",
                NodeVersion = new Version(1, 0, 0),
                Pins = [
                    new NodePin { Id = "key", Label = "Tecla", Direction = PinDirection.Input, DataType = typeof(string), DefaultValue = "A" },
                    new NodePin { Id = "done", Label = "Concluído", Direction = PinDirection.Output, DataType = typeof(bool) }
                ]
            });

            _list.Add(new NodeMetadata
            {
                TypeId = "mouse.left_click",
                Name = "Left Click",
                Description = "Simula um clique com o botão esquerdo do mouse",
                Category = NodeCategory.Mouse,
                IconKey = "Mouse",
                NodeVersion = new Version(1, 0, 0),
                Pins = [
                    new NodePin { Id = "x", Label = "Posição X", Direction = PinDirection.Input, DataType = typeof(int), DefaultValue = 0 },
                    new NodePin { Id = "y", Label = "Posição Y", Direction = PinDirection.Input, DataType = typeof(int), DefaultValue = 0 },
                    new NodePin { Id = "done", Label = "Concluído", Direction = PinDirection.Output, DataType = typeof(bool) }
                ]
            });

            _list.Add(new NodeMetadata
            {
                TypeId = "flow.wait",
                Name = "Wait",
                Description = "Aguarda um tempo antes de seguir o fluxo",
                Category = NodeCategory.Loops,
                IconKey = "Timer",
                NodeVersion = new Version(1, 0, 0),
                Pins = [
                    new NodePin { Id = "ms", Label = "Milissegundos", Direction = PinDirection.Input, DataType = typeof(int), DefaultValue = 1000 },
                    new NodePin { Id = "done", Label = "Concluído", Direction = PinDirection.Output, DataType = typeof(bool) }
                ]
            });
        }

        public void Register(NodeMetadata metadata, INodeExecutor executor) { }
        public NodeMetadata? GetMetadata(string typeId) => _list.FirstOrDefault(n => n.TypeId == typeId);
        public INodeExecutor? GetExecutor(string typeId) => null;
        public bool TryGet(string typeId, out NodeMetadata? metadata, out INodeExecutor? executor)
        {
            metadata = GetMetadata(typeId);
            executor = null;
            return metadata != null;
        }
        public IEnumerable<NodeMetadata> GetAll() => _list;
        public IEnumerable<NodeMetadata> GetByCategory(NodeCategory category) => _list.Where(n => n.Category == category);
        public IEnumerable<NodeMetadata> Search(string query) => _list.Where(n => n.Name.Contains(query, StringComparison.OrdinalIgnoreCase));
        public bool IsRegistered(string typeId) => _list.Any(n => n.TypeId == typeId);
    }
}
