using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacroKids.Core.Commands;
using MacroKids.Core.Interfaces;
using MacroKids.Core.Models;
using MacroKids.NodeEditor.ViewModels;
using MacroKids.Nodes.Flow;
using MacroKids.Nodes.Keyboard;
using MacroKids.Nodes.Logic;
using MacroKids.Nodes.Mouse;
using MacroKids.Nodes.System;
using MacroKids.Nodes.Variables;
using MacroKids.Runtime;
using MacroKids.UI.Services;
using MacroKids.UI.Views;

namespace MacroKids.UI.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly INodeRegistry _nodeRegistry;
    private FlowExecutor? _activeExecutor;
    private NodeCanvasViewModel? _observedCanvas;

    [ObservableProperty] private string _windowTitle = "MacroKids - v0.1.4-dev";
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private string _selectedModule = "Blocks";
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isDarkTheme = true;
    [ObservableProperty] private LanguageOption? _selectedLanguage;
    [ObservableProperty] private bool _isLanguageMenuOpen;
    [ObservableProperty] private bool _isLogExpanded;
    public ObservableCollection<string> ExecutionLogs { get; } = [];
    public string ExecutionLogsText => string.Join(Environment.NewLine, ExecutionLogs);

    public ObservableCollection<ProjectPageViewModel> Pages { get; } = [];
    public ObservableCollection<NodeMetadata> AvailableNodes { get; } = [];
    public ObservableCollection<LanguageOption> Languages { get; } = [];

    public NodeCanvasViewModel CanvasViewModel => SelectedPage?.CanvasViewModel ?? Pages.First().CanvasViewModel;
    public NodeViewModel? SelectedNode => CanvasViewModel.SelectedNode;

    [ObservableProperty] private ProjectPageViewModel? _selectedPage;

    public IEnumerable<IGrouping<NodeCategory, NodeMetadata>> GroupedNodes => GetGroupedNodes();

    public MainWindowViewModel()
    {
        MacroKids.Core.Translator.GetDeclaredVariables = () =>
        {
            if (CanvasViewModel == null) return Enumerable.Empty<string>();
            return CanvasViewModel.Nodes
                .Where(n => n.Metadata.TypeId == "variables.set")
                .Select(n => n["name"]?.ToString())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .OfType<string>();
        };

        var registry = new NodeRegistry();
        // Mouse
        registry.Register(MoveMouseMetadata.Instance,   new MoveMouseExecutor());
        registry.Register(LeftClickMetadata.Instance,   new LeftClickExecutor());
        registry.Register(RightClickMetadata.Instance,  new RightClickExecutor());
        registry.Register(MouseScrollMetadata.Instance, new MouseScrollExecutor());
        registry.Register(DoubleClickMetadata.Instance, new DoubleClickExecutor());
        // Keyboard
        registry.Register(PressKeyMetadata.Instance,  new PressKeyExecutor());
        registry.Register(TypeTextMetadata.Instance,  new TypeTextExecutor());
        registry.Register(HoldKeyMetadata.Instance,   new HoldKeyExecutor());
        registry.Register(ComboKeyMetadata.Instance,  new ComboKeyExecutor());
        registry.Register(WaitMetadata.Instance,       new WaitExecutor());
        registry.Register(RepeatLoopMetadata.Instance, new RepeatLoopExecutor());
        registry.Register(ForEachMetadata.Instance,    new ForEachExecutor());
        registry.Register(ForLoopMetadata.Instance,    new ForLoopExecutor());
        registry.Register(WhileLoopMetadata.Instance,  new WhileLoopExecutor());
        // Logic
        registry.Register(IfConditionMetadata.Instance, new IfConditionExecutor());
        // Variables
        registry.Register(SetVariableMetadata.Instance,       new SetVariableExecutor());
        registry.Register(GetVariableMetadata.Instance,       new GetVariableExecutor());
        registry.Register(IncrementVariableMetadata.Instance, new IncrementVariableExecutor());

        _nodeRegistry = registry;
        Languages.Add(new LanguageOption("Português", "pt-BR", "brazil.png"));
        Languages.Add(new LanguageOption("English", "en", "usa.png"));
        Languages.Add(new LanguageOption("Español", "es", "spain.png"));
        SelectedLanguage = Languages.FirstOrDefault(l => l.Code == LocalizationManager.Instance.CurrentCulture) ?? Languages[0];

        foreach (var nodeMeta in _nodeRegistry.GetAll())
            AvailableNodes.Add(nodeMeta);

        Pages.Add(new ProjectPageViewModel(_nodeRegistry, GetText("TabMyProject", "My Project"), canClose: true));
        Pages.Add(new ProjectPageViewModel(_nodeRegistry, GetText("TabNewAutomation", "New Automation")));
        SelectedPage = Pages[0];

        CanvasViewModel.AddNode("mouse.move", 100, 100);
        CanvasViewModel.AddNode("mouse.left_click", 350, 150);
        CanvasViewModel.AddNode("flow.wait", 100, 300);

        UpdateWindowTitle();
        ApplyTheme(isDarkTheme: true);
        StatusMessage = GetText("StatusReady", "Ready");
    }

    private static string GetText(string key, string fallback)
    {
        return LocalizationManager.Instance.Translations.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;
    }

    private void UpdateWindowTitle()
    {
        var pageTitle = SelectedPage?.Title ?? GetText("TabMyProject", "My Project");
        WindowTitle = $"MacroKids - {pageTitle} - v0.1.4-dev";
    }

    public ImageSource ThemeIcon => LoadThemeIcon();
    public ImageSource? LogoIcon => LoadLogoIcon();

    public bool CanPickMoveMouseCoordinates => SelectedNode?.Metadata.TypeId == "mouse.move";

    partial void OnSearchTextChanged(string value) => OnPropertyChanged(nameof(GroupedNodes));
    partial void OnSelectedModuleChanged(string value) => OnPropertyChanged(nameof(GroupedNodes));

    partial void OnSelectedPageChanged(ProjectPageViewModel? oldValue, ProjectPageViewModel? newValue)
    {
        if (oldValue is not null)
            oldValue.IsSelected = false;

        if (_observedCanvas is not null)
        {
            _observedCanvas.PropertyChanged -= ObservedCanvas_PropertyChanged;
            _observedCanvas = null;
        }

        if (newValue is not null)
        {
            newValue.IsSelected = true;
            _observedCanvas = newValue.CanvasViewModel;
            _observedCanvas.PropertyChanged += ObservedCanvas_PropertyChanged;
        }

        OnPropertyChanged(nameof(CanvasViewModel));
        OnPropertyChanged(nameof(SelectedNode));
        DeleteSelectedCommand.NotifyCanExecuteChanged();
        DuplicateSelectedCommand.NotifyCanExecuteChanged();
        UpdateWindowTitle();
    }

    private void ObservedCanvas_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NodeCanvasViewModel.SelectedNode))
        {
            OnPropertyChanged(nameof(SelectedNode));
            OnPropertyChanged(nameof(CanPickMoveMouseCoordinates));
            DeleteSelectedCommand.NotifyCanExecuteChanged();
            DuplicateSelectedCommand.NotifyCanExecuteChanged();
        }
    }

    private static ImageSource LoadThemeIcon()
    {
        var fileName = (Application.Current?.Resources.MergedDictionaries.Any(d => d.Source?.OriginalString.Contains("DarkTheme.xaml", StringComparison.OrdinalIgnoreCase) == true) ?? false)
            ? "moon.png"
            : "sun.png";

        return LoadIcon(fileName) ?? new DrawingImage();
    }

    private static ImageSource? LoadLogoIcon()
    {
        var fileName = (Application.Current?.Resources.MergedDictionaries.Any(d => d.Source?.OriginalString.Contains("DarkTheme.xaml", StringComparison.OrdinalIgnoreCase) == true) ?? false)
            ? "MacroKids-LogoDarkTheme.png"
            : "MacroKids-LogoLightTheme.png";

        return LoadIcon(fileName) ?? LoadIcon("MacroKids-Logo.png");
    }

    private static ImageSource? LoadIcon(string fileName)
    {
        try
        {
            var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", fileName);
            if (!System.IO.File.Exists(path))
                return null;

            var bitmap = new System.Windows.Media.Imaging.BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    private IEnumerable<IGrouping<NodeCategory, NodeMetadata>> GetGroupedNodes()
    {
        IEnumerable<NodeMetadata> query = _nodeRegistry.GetAll();

        query = SelectedModule switch
        {
            "Blocks"    => query.Where(n => n.Category is NodeCategory.Mouse or NodeCategory.Keyboard or NodeCategory.Gamepad),
            "Variables" => query.Where(n => n.Category == NodeCategory.Variables),
            "Functions" => query.Where(n => n.Category is NodeCategory.Logic or NodeCategory.Loops),
            "Images"    => query.Where(n => n.Category == NodeCategory.Images),
            "OCR"       => query.Where(n => n.Category == NodeCategory.Ocr),
            "AI"        => query.Where(n => n.Category == NodeCategory.Ai),
            "Events"    => query.Where(n => n.Category == NodeCategory.Events),
            "Settings"  => query.Where(n => n.Category is NodeCategory.System or NodeCategory.Window or NodeCategory.File or NodeCategory.Network),
            _ => query
        };

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            query = query.Where(n =>
                n.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                n.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                n.TypeId.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        return query
            .GroupBy(n => n.Category)
            .OrderBy(g => g.Key);
    }

    [RelayCommand]
    private void AddPage()
    {
        var baseTitle = GetText("TabNewAutomation", "New Automation");
        var index = Pages.Count(p => p.Title.StartsWith(baseTitle, StringComparison.OrdinalIgnoreCase));
        var title = index == 0 ? baseTitle : $"{baseTitle} {index + 1}";

        var page = new ProjectPageViewModel(_nodeRegistry, title);
        Pages.Add(page);
        SelectedPage = page;
        StatusMessage = string.Format(CultureInfo.CurrentCulture, GetText("StatusPageCreated", "{0} created."), title);
    }

    [RelayCommand]
    private void ClosePage(ProjectPageViewModel? page)
    {
        if (page is null || !page.CanClose)
            return;

        var index = Pages.IndexOf(page);
        Pages.Remove(page);

        if (Pages.Count == 0)
        {
            var fallback = new ProjectPageViewModel(_nodeRegistry, GetText("TabNewAutomation", "New Automation"));
            Pages.Add(fallback);
            SelectedPage = fallback;
        }
        else if (SelectedPage == page)
        {
            SelectedPage = Pages[Math.Max(0, index - 1)];
        }

        StatusMessage = string.Format(CultureInfo.CurrentCulture, GetText("StatusPageClosed", "{0} closed."), page.Title);
    }

    [RelayCommand]
    private void SelectPage(ProjectPageViewModel? page)
    {
        if (page is null || SelectedPage == page)
            return;

        SelectedPage = page;
    }

    [RelayCommand]
    private void AddNodeToCanvas(NodeMetadata? metadata)
    {
        if (metadata is null)
            return;

        CanvasViewModel.AddNode(metadata.TypeId, 150, 150);
        StatusMessage = string.Format(CultureInfo.CurrentCulture, GetText("StatusNodeAdded", "Added block: {0}"), metadata.Name);
    }

    [RelayCommand]
    private void SelectModule(string moduleName)
    {
        if (!string.IsNullOrWhiteSpace(moduleName))
            SelectedModule = moduleName;
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        var app = Application.Current;
        if (app is null)
            return;

        ApplyTheme(!IsDarkTheme);
        OnPropertyChanged(nameof(ThemeIcon));
        OnPropertyChanged(nameof(LogoIcon));
        StatusMessage = IsDarkTheme
            ? GetText("StatusThemeDark", "Dark theme enabled.")
            : GetText("StatusThemeLight", "Light theme enabled.");
    }

    [RelayCommand]
    private void ToggleLanguageMenu()
    {
        IsLanguageMenuOpen = !IsLanguageMenuOpen;
    }

    [RelayCommand]
    private void SelectLanguage(LanguageOption? language)
    {
        if (language is null)
            return;

        SelectedLanguage = language;
        ChangeLanguage(language.Code);
        IsLanguageMenuOpen = false;
    }

    private void ApplyTheme(bool isDarkTheme)
    {
        var app = Application.Current;
        if (app is null)
            return;

        var dictionaries = app.Resources.MergedDictionaries;
        var existing = dictionaries.FirstOrDefault(d =>
            d.Source != null &&
            (d.Source.OriginalString.Contains("DarkTheme.xaml", StringComparison.OrdinalIgnoreCase) ||
             d.Source.OriginalString.Contains("LightTheme.xaml", StringComparison.OrdinalIgnoreCase)));

        if (existing is not null)
            dictionaries.Remove(existing);

        dictionaries.Add(new ResourceDictionary
        {
            Source = new Uri(isDarkTheme
                ? "pack://application:,,,/MacroKids.NodeEditor;component/Themes/DarkTheme.xaml"
                : "pack://application:,,,/MacroKids.NodeEditor;component/Themes/LightTheme.xaml")
        });

        IsDarkTheme = isDarkTheme;
        OnPropertyChanged(nameof(ThemeIcon));
        OnPropertyChanged(nameof(LogoIcon));
    }

    [RelayCommand]
    private void ChangeLanguage(string cultureCode)
    {
        if (string.IsNullOrWhiteSpace(cultureCode))
            return;

        LocalizationManager.Instance.LoadCulture(cultureCode);
        SelectedLanguage = Languages.FirstOrDefault(l => l.Code == LocalizationManager.Instance.CurrentCulture) ?? SelectedLanguage;
        OnPropertyChanged(nameof(GroupedNodes));
        UpdateWindowTitle();
        StatusMessage = string.Format(CultureInfo.CurrentCulture, GetText("StatusLanguageChanged", "Language changed: {0}"), cultureCode);
        IsLanguageMenuOpen = false;
    }

    [RelayCommand]
    private void PickMoveMouseCoordinates()
    {
        if (!CanPickMoveMouseCoordinates || SelectedNode is null)
            return;

        var picker = new CoordinatePickerWindow();
        if (picker.ShowDialog() != true)
            return;

        var point = picker.SelectedPoint;
        SelectedNode["x"] = (int)point.X;
        SelectedNode["y"] = (int)point.Y;
        StatusMessage = string.Format(CultureInfo.CurrentCulture, GetText("StatusCoordinatesCaptured", "Coordinates captured: {0:0}, {1:0}"), point.X, point.Y);
    }

    [RelayCommand]
    private async Task SaveProjectAsync()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Projeto MacroKids (*.mkproject)|*.mkproject",
            FileName = "meu_projeto.mkproject"
        };

        if (dialog.ShowDialog() != true)
            return;

        StatusMessage = GetText("StatusSaving", "Saving project...");
        var doc = CanvasViewModel.ToFlowDocument();
        await MacroKids.Core.Serialization.ProjectPackager.PackAsync(doc, dialog.FileName);
        StatusMessage = GetText("StatusProjectSaved", "Project saved successfully!");
    }

    [RelayCommand]
    private async Task LoadProjectAsync()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Projeto MacroKids (*.mkproject)|*.mkproject"
        };

        if (dialog.ShowDialog() != true)
            return;

        StatusMessage = GetText("StatusLoading", "Loading project...");
        var doc = await MacroKids.Core.Serialization.ProjectPackager.UnpackAsync(dialog.FileName);
        
        var newPage = new ProjectPageViewModel(_nodeRegistry, doc.Name, canClose: true);
        newPage.CanvasViewModel.LoadDocument(doc);
        Pages.Add(newPage);
        SelectedPage = newPage;

        StatusMessage = string.Format(CultureInfo.CurrentCulture, GetText("StatusProjectLoaded", "Project '{0}' loaded in a new tab!"), doc.Name);
    }

    [RelayCommand]
    private async Task RunFlowAsync()
    {
        await ExecuteFlowAsync(stepDelayMs: 0);
    }

    [RelayCommand]
    private async Task DebugFlowAsync()
    {
        await ExecuteFlowAsync(stepDelayMs: 200);
    }

    [RelayCommand]
    private void StopFlow()
    {
        _activeExecutor?.Stop();
        StatusMessage = GetText("StatusExecutionStopped", "Execution stopped.");
    }

    [RelayCommand]
    private void RecordMacro()
    {
        StatusMessage = "Iniciando gravador global de macro...";
        MacroKids.UI.Services.MacroRecorder.Start();

        // Minimiza a janela principal
        if (Application.Current.MainWindow != null)
        {
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
        }

        // Abre a janelinha flutuante para controle
        var overlay = new Views.RecorderOverlayWindow(() =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var actions = MacroKids.UI.Services.MacroRecorder.Stop();

                // Restaura a janela principal
                if (Application.Current.MainWindow != null)
                {
                    Application.Current.MainWindow.WindowState = WindowState.Normal;
                    Application.Current.MainWindow.Activate();
                }

                // Importa as acoes gravadas para o canvas da pagina selecionada
                CanvasViewModel.ImportRecordedActions(actions);

                StatusMessage = string.Format(System.Globalization.CultureInfo.CurrentCulture, "Macro gravada com sucesso! {0} blocos gerados.", actions.Count);
            });
        });
        overlay.Show();
    }

    [RelayCommand]
    private void ExportProject()
    {
        SaveProjectCommand.Execute(null);
    }

    [RelayCommand]
    private void ToggleCanvasGrid()
    {
        CanvasViewModel.ToggleGridCommand.Execute(null);
    }

    [RelayCommand]
    private void CloseConsole()
    {
        IsLogExpanded = false;
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void DeleteSelected()
    {
        CanvasViewModel.DeleteSelectedCommand.Execute(null);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void DuplicateSelected()
    {
        CanvasViewModel.DuplicateSelectedCommand.Execute(null);
    }

    private bool HasSelection => SelectedNode is not null;

    private async Task ExecuteFlowAsync(int stepDelayMs)
    {
        // Reset node visual execution states
        foreach (var n in CanvasViewModel.Nodes)
        {
            n.IsExecuting = false;
            n.HasError = false;
        }

        ExecutionLogs.Clear();
        OnPropertyChanged(nameof(ExecutionLogsText));
        IsLogExpanded = true;

        try
        {
            var document = CanvasViewModel.ToFlowDocument();
            var eventBus = new EventBus();
            _activeExecutor = new FlowExecutor(_nodeRegistry, eventBus)
            {
                StepDelayMs = stepDelayMs
            };

            eventBus.Subscribe<MacroKids.Core.Events.NodeStartedEvent>(e =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = string.Format(CultureInfo.CurrentCulture, GetText("StatusExecutingNode", "Executing block: {0}..."), e.TypeId);
                    var node = CanvasViewModel.Nodes.FirstOrDefault(n => n.InstanceId == e.NodeInstanceId);
                    if (node != null)
                    {
                        node.IsExecuting = true;
                        node.HasError = false;
                    }
                });
            });

            eventBus.Subscribe<MacroKids.Core.Events.NodeCompletedEvent>(e =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var node = CanvasViewModel.Nodes.FirstOrDefault(n => n.InstanceId == e.NodeInstanceId);
                    if (node != null)
                    {
                        node.IsExecuting = false;
                    }
                });
            });

            eventBus.Subscribe<MacroKids.Core.Events.NodeErrorEvent>(e =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var node = CanvasViewModel.Nodes.FirstOrDefault(n => n.InstanceId == e.NodeInstanceId);
                    if (node != null)
                    {
                        node.IsExecuting = false;
                        node.HasError = true;
                    }
                });
            });

            eventBus.Subscribe<MacroKids.Core.Events.LogMessageEvent>(e =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var timestamp = e.Timestamp.ToLocalTime().ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
                    ExecutionLogs.Add($"[{timestamp}] [{e.Level}] {e.Message}");
                    OnPropertyChanged(nameof(ExecutionLogsText));
                });
            });

            StatusMessage = stepDelayMs > 0
                ? "Executando em modo debug..."
                : GetText("StatusRunning", "Running automation...");

            await _activeExecutor.RunAsync(document);
            StatusMessage = GetText("StatusSuccess", "Execution finished successfully!");
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(GetText("StatusError", "Execution error: {0}"), ex.Message);
        }
        finally
        {
            _activeExecutor = null;
            foreach (var n in CanvasViewModel.Nodes)
            {
                n.IsExecuting = false;
            }
        }
    }

}
