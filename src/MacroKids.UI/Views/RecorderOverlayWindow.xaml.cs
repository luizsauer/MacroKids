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

    private void PauseButton_Click(object sender, RoutedEventArgs e)
    {
        MacroKids.UI.Services.MacroRecorder.Pause();
        PauseButton.Visibility = Visibility.Collapsed;
        ResumeButton.Visibility = Visibility.Visible;
        StatusText.Text = "Pausado...";
        StatusDot.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(217, 119, 6)); // Laranja
    }

    private void ResumeButton_Click(object sender, RoutedEventArgs e)
    {
        MacroKids.UI.Services.MacroRecorder.Resume();
        PauseButton.Visibility = Visibility.Visible;
        ResumeButton.Visibility = Visibility.Collapsed;
        StatusText.Text = "Gravando...";
        StatusDot.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68)); // Vermelho
    }
}
