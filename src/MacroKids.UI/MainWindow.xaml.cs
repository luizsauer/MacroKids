using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using MacroKids.UI.ViewModels;

namespace MacroKids.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        SetWindowIcon();
        DataContext = new ViewModels.MainWindowViewModel();
    }

    private void SetWindowIcon()
    {
        var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "favicon.ico");
        if (!System.IO.File.Exists(iconPath))
            return;

        Icon = BitmapFrame.Create(new Uri(iconPath, UriKind.Absolute));
    }

    private void SearchCanvas_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Focus();
        SearchBox.SelectAll();
    }

    private void ConnectLink_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.MainWindowViewModel vm)
        {
            vm.StatusMessage = "Use os pinos no canvas para conectar blocos.";
        }
    }

    private void Fullscreen_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
            WindowStyle = WindowStyle.SingleBorderWindow;
        }
        else
        {
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
        }
    }

    private void LockCanvas_Click(object sender, RoutedEventArgs e)
    {
        EditorView.IsEnabled = !EditorView.IsEnabled;
    }

    private void LanguageOption_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is not LanguageOption language)
            return;

        if (DataContext is MainWindowViewModel vm)
        {
            vm.SelectLanguageCommand.Execute(language);
        }
    }

}