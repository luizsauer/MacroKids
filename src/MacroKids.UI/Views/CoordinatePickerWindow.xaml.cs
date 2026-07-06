using System.Windows;
using System.Windows.Input;

namespace MacroKids.UI.Views;

public partial class CoordinatePickerWindow : Window
{
    public Point SelectedPoint { get; private set; }

    public CoordinatePickerWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        SelectedPoint = PointToScreen(e.GetPosition(this));
        DialogResult = true;
        Close();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
        }
    }
}
