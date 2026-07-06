using System.IO;
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

    [RelayCommand]
    private async Task SaveProjectAsync()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Projeto MacroKids (*.mkproject)|*.mkproject",
            FileName = "meu_projeto.mkproject"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                StatusMessage = "Salvando projeto...";
                var doc = CanvasViewModel.ToFlowDocument();
                
                // Pack directly using Core ProjectPackager file helper
                await MacroKids.Core.Serialization.ProjectPackager.PackAsync(doc, dialog.FileName);
                
                StatusMessage = "Projeto salvo com sucesso!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erro ao salvar projeto: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private async Task LoadProjectAsync()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Projeto MacroKids (*.mkproject)|*.mkproject"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                StatusMessage = "Carregando projeto...";
                
                // Unpack directly using Core ProjectPackager file helper
                var doc = await MacroKids.Core.Serialization.ProjectPackager.UnpackAsync(dialog.FileName);
                
                if (doc != null)
                {
                    CanvasViewModel.LoadDocument(doc);
                    StatusMessage = $"Projeto '{doc.Name}' carregado!";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erro ao carregar projeto: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private async Task RunFlowAsync()
    {
        try
        {
            StatusMessage = "Executando automação...";
            
            // 1. Get graph structure
            var doc = CanvasViewModel.ToFlowDocument();
            
            // 2. Setup execution engine dependencies
            var eventBus = new MacroKids.Runtime.EventBus();
            var execContext = new MacroKids.Runtime.ExecutionContext(eventBus, CancellationToken.None);
            
            // Subscribe to execution log events to display on status bar
            eventBus.Subscribe<MacroKids.Core.Events.NodeStartedEvent>(e =>
            {
                // System.Windows Dispatcher is used to safely update UI thread from execution thread
                App.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = $"Executando bloco: {e.TypeId}...";
                });
            });

            var executor = new MacroKids.Runtime.FlowExecutor(_nodeRegistry, eventBus);
            
            // 3. Start simulation in a background task to keep UI responsive
            await Task.Run(async () =>
            {
                await executor.RunAsync(doc);
            });
            
            StatusMessage = "Execução concluída com sucesso!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro na execução: {ex.Message}";
        }
    }
}
