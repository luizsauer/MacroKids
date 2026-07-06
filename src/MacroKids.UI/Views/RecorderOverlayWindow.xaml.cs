using System;
using System.Windows;
using System.Windows.Input;

namespace MacroKids.UI.Views;

public partial class RecorderOverlayWindow : Window
{
    private readonly Action _onStop;

    public RecorderOverlayWindow(Action onStop)
    {
        InitializeComponent();
        _onStop = onStop;

        // Permite arrastar a janelinha flutuante clicando e arrastando qualquer parte dela
        MouseDown += (s, e) =>
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        };

        // Posiciona a janela no canto superior direito do monitor principal
        var workingArea = SystemParameters.WorkArea;
        Left = workingArea.Right - Width - 30;
        Top = 30;
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
        _onStop();
    }
}
