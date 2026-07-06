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

    private void LanguageCombo_Selected(object sender, RoutedEventArgs e)
    {
        if (DataContext == null) return;

        if (e.Source is ComboBoxItem item && item.Tag is string cultureCode)
        {
            if (DataContext is ViewModels.MainWindowViewModel vm)
            {
                vm.ChangeLanguageCommand.Execute(cultureCode);
            }
        }
    }
}