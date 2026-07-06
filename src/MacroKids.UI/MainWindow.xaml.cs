using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MacroKids.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new ViewModels.MainWindowViewModel();
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

    private void Language_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext == null) return;

        if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem item && item.Tag is string cultureCode)
        {
            if (DataContext is ViewModels.MainWindowViewModel vm)
            {
                vm.ChangeLanguageCommand.Execute(cultureCode);
            }
        }
    }
}