using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using MacroKids.Core.Models;
using MacroKids.UI.ViewModels;
using MacroKids.UI.Services;

namespace MacroKids.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private Point _paletteDragStart;

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
            vm.StatusMessage = LocalizationManager.Instance.Translations.TryGetValue("StatusConnectHint", out var value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : "Use the pins on the canvas to connect blocks.";
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

    private void PaletteItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is not NodeMetadata)
            return;

        _paletteDragStart = e.GetPosition(this);
    }

    private void PaletteItem_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is not NodeMetadata metadata)
            return;

        if (e.LeftButton != MouseButtonState.Pressed)
            return;

        var current = e.GetPosition(this);
        if (Math.Abs(current.X - _paletteDragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(current.Y - _paletteDragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        var data = new DataObject();
        data.SetData(typeof(NodeMetadata), metadata);
        data.SetData(DataFormats.StringFormat, metadata.TypeId);
        DragDrop.DoDragDrop(element, data, DragDropEffects.Copy);
        e.Handled = true;
    }

}