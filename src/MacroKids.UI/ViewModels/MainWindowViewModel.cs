using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
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
using MacroKids.Nodes.Mouse;
using MacroKids.Runtime;
using MacroKids.UI.Services;

namespace MacroKids.UI.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly INodeRegistry _nodeRegistry;
    private FlowExecutor? _activeExecutor;
    private NodeCanvasViewModel? _observedCanvas;
    private readonly ImageSource? _logoIcon;

    [ObservableProperty] private string _windowTitle = "MacroKids - v0.1.2-dev";
    [ObservableProperty] private string _statusMessage = "Pronto";
    [ObservableProperty] private string _selectedModule = "Blocks";
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isDarkTheme = true;
    [ObservableProperty] private LanguageOption? _selectedLanguage;

    public ObservableCollection<ProjectPageViewModel> Pages { get; } = [];
    public ObservableCollection<NodeMetadata> AvailableNodes { get; } = [];
    public ObservableCollection<LanguageOption> Languages { get; } = [];

    public NodeCanvasViewModel CanvasViewModel => SelectedPage?.CanvasViewModel ?? Pages.First().CanvasViewModel;
    public NodeViewModel? SelectedNode => CanvasViewModel.SelectedNode;

    [ObservableProperty] private ProjectPageViewModel? _selectedPage;

    public IEnumerable<IGrouping<NodeCategory, NodeMetadata>> GroupedNodes => GetGroupedNodes();

    public MainWindowViewModel()
    {
        var registry = new NodeRegistry();
        registry.Register(MoveMouseMetadata.Instance, new MoveMouseExecutor());
        registry.Register(LeftClickMetadata.Instance, new LeftClickExecutor());
        registry.Register(RightClickMetadata.Instance, new RightClickExecutor());
        registry.Register(PressKeyMetadata.Instance, new PressKeyExecutor());
        registry.Register(TypeTextMetadata.Instance, new TypeTextExecutor());
        registry.Register(WaitMetadata.Instance, new WaitExecutor());

        _nodeRegistry = registry;
        _logoIcon = LoadIcon("MacroKids-MiniLogo.png");

        Languages.Add(new LanguageOption("Português", "pt-BR", "brazil.png"));
        Languages.Add(new LanguageOption("English", "en", "usa.png"));
        Languages.Add(new LanguageOption("Español", "es", "spain.png"));
        SelectedLanguage = Languages.FirstOrDefault(l => l.Code == LocalizationManager.Instance.CurrentCulture) ?? Languages[0];

        foreach (var nodeMeta in _nodeRegistry.GetAll())
            AvailableNodes.Add(nodeMeta);

        Pages.Add(new ProjectPageViewModel(_nodeRegistry, GetText("TabMyProject", "My Project"), canClose: false));
        Pages.Add(new ProjectPageViewModel(_nodeRegistry, GetText("TabNewAutomation", "New Automation")));
        SelectedPage = Pages[0];

        CanvasViewModel.AddNode("mouse.move", 100, 100);
        CanvasViewModel.AddNode("mouse.left_click", 350, 150);
        CanvasViewModel.AddNode("flow.wait", 100, 300);

        UpdateWindowTitle();
        ApplyTheme(isDarkTheme: true);
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
        WindowTitle = $"MacroKids - {pageTitle} - v0.1.2-dev";
    }

    public ImageSource ThemeIcon => LoadThemeIcon();
    public ImageSource? LogoIcon => _logoIcon;

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
        UpdateWindowTitle();
    }

    private void ObservedCanvas_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NodeCanvasViewModel.SelectedNode))
        {
            OnPropertyChanged(nameof(SelectedNode));
            OnPropertyChanged(nameof(CanPickMoveMouseCoordinates));
        }
    }

    private static ImageSource LoadThemeIcon()
    {
        var fileName = (Application.Current?.Resources.MergedDictionaries.Any(d => d.Source?.OriginalString.Contains("DarkTheme.xaml", StringComparison.OrdinalIgnoreCase) == true) ?? false)
            ? "moon.png"
            : "sun.png";

        return LoadIcon(fileName) ?? new DrawingImage();
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
            "Variables" => query.Where(n => n.Category == NodeCategory.Variables),
            "Functions" => query.Where(n => n.Category is NodeCategory.Logic or NodeCategory.Loops),
            "Images" => query.Where(n => n.Category == NodeCategory.Images),
            "OCR" => query.Where(n => n.Category == NodeCategory.Ocr),
            "AI" => query.Where(n => n.Category == NodeCategory.Ai),
            "Events" => query.Where(n => n.Category == NodeCategory.Events),
            "Settings" => query.Where(n => n.Category is NodeCategory.System or NodeCategory.Window or NodeCategory.File or NodeCategory.Network),
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
        var index = Pages.Count(p => p.CanClose) + 1;
        var title = index == 1
            ? GetText("TabNewAutomation", "New Automation")
            : $"{GetText("TabNewAutomation", "New Automation")} {index}";

        var page = new ProjectPageViewModel(_nodeRegistry, title);
        Pages.Add(page);
        SelectedPage = page;
        StatusMessage = $"{title} criado.";
    }

    [RelayCommand]
    private void ClosePage(ProjectPageViewModel? page)
    {
        if (page is null || !page.CanClose || Pages.Count == 1)
            return;

        var index = Pages.IndexOf(page);
        Pages.Remove(page);

        if (SelectedPage == page)
            SelectedPage = Pages[Math.Max(0, index - 1)];

        StatusMessage = $"{page.Title} fechado.";
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
        StatusMessage = $"Adicionado bloco: {metadata.Name}";
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
        StatusMessage = IsDarkTheme ? "Tema escuro ativado." : "Tema claro ativado.";
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
        StatusMessage = $"Idioma alterado: {cultureCode}";
    }

    [RelayCommand]
    private void PickMoveMouseCoordinates()
    {
        if (!CanPickMoveMouseCoordinates || SelectedNode is null)
            return;

        if (!TryGetCursorPosition(out var point))
            return;

        SelectedNode["x"] = (int)point.X;
        SelectedNode["y"] = (int)point.Y;
        StatusMessage = $"Coordenadas capturadas: {point.X:0}, {point.Y:0}";
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
        StatusMessage = "Projeto salvo com sucesso!";
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
        CanvasViewModel.LoadDocument(doc);
        StatusMessage = $"Projeto '{doc.Name}' carregado!";
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
        StatusMessage = "Execução interrompida.";
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

    private async Task ExecuteFlowAsync(int stepDelayMs)
    {
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
                    StatusMessage = $"Executando bloco: {e.TypeId}...";
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
        }
    }

    private static bool TryGetCursorPosition(out System.Windows.Point point)
    {
        var p = new POINT();
        if (GetCursorPos(ref p))
        {
            point = new System.Windows.Point(p.X, p.Y);
            return true;
        }

        point = default;
        return false;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(ref POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }
}
