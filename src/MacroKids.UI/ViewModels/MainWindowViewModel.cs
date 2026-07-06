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
using MacroKids.UI.Services;

namespace MacroKids.UI.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly INodeRegistry _nodeRegistry;
    private readonly CommandHistory _commandHistory;

    [ObservableProperty] private string _windowTitle = "MacroKids - v0.1.1 (Dev)";
    [ObservableProperty] private string _statusMessage = "Pronto";
    [ObservableProperty] private string _selectedModule = "Blocks"; // Default module selected
    [ObservableProperty] private string _searchText = string.Empty;  // Filter search text
    
    public NodeCanvasViewModel CanvasViewModel { get; }
    public ObservableCollection<NodeMetadata> AvailableNodes { get; } = [];

    // Filtered list of nodes to display on block list view
    public IEnumerable<IGrouping<NodeCategory, NodeMetadata>> GroupedNodes
    {
        get
        {
            var query = _nodeRegistry.GetAll();
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(n => n.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || 
                                         n.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }
            return query.GroupBy(n => n.Category);
        }
    }

    // Trigger UI updates when SearchText changes
    partial void OnSearchTextChanged(string value)
    {
        OnPropertyChanged(nameof(GroupedNodes));
    }

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
    private void SelectModule(string moduleName)
    {
        if (!string.IsNullOrWhiteSpace(moduleName))
        {
            SelectedModule = moduleName;
        }
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        // WPF Theme switching logic
        var app = System.Windows.Application.Current;
        if (app == null) return;

        // Try to find the active dark theme dictionary
        var darkTheme = app.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("DarkTheme.xaml"));

        if (darkTheme != null)
        {
            // Switch to Light Theme (or create a light colors dictionary override)
            app.Resources.MergedDictionaries.Remove(darkTheme);
            
            // Inject a light overrides dictionary
            var lightTheme = new System.Windows.ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/MacroKids.NodeEditor;component/Themes/DarkTheme.xaml") // Fallback check
            };
            app.Resources.MergedDictionaries.Add(lightTheme);
            
            StatusMessage = "Tema alternado!";
        }
        else
        {
            // Restore Dark Theme
            var dark = new System.Windows.ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/MacroKids.NodeEditor;component/Themes/DarkTheme.xaml")
            };
            app.Resources.MergedDictionaries.Add(dark);
            StatusMessage = "Tema Escuro Ativado!";
        }
    }

    [RelayCommand]
    private void ChangeLanguage(string cultureCode)
    {
        if (!string.IsNullOrWhiteSpace(cultureCode))
        {
            LocalizationManager.Instance.LoadCulture(cultureCode);
            StatusMessage = $"Idioma alterado: {cultureCode}";
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
