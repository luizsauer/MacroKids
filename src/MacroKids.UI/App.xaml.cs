using System.Configuration;
using System.Data;
using System.Windows;
using NLog;

namespace MacroKids.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public App()
    {
        // Capture Win32 thread exceptions
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            LogException(e.ExceptionObject as Exception, "AppDomain.CurrentDomain.UnhandledException");
        };

        // Capture WPF UI dispatcher exceptions
        DispatcherUnhandledException += (s, e) =>
        {
            LogException(e.Exception, "Application.DispatcherUnhandledException");
            e.Handled = true; // Prevent complete silent exit before reporting
        };

        // Capture asynchronous Task exceptions
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            LogException(e.Exception, "TaskScheduler.UnobservedTaskException");
            e.SetObserved();
        };

        Logger.Info("Aplicação MacroKids iniciada.");
    }

    private static void LogException(Exception? ex, string source)
    {
        if (ex == null) return;
        
        Logger.Fatal(ex, $"Crash detectado no {source}");
        
        MessageBox.Show(
            $"Ocorreu um erro crítico na inicialização ({source}):\n\n{ex.Message}\n\nDetalhes gravados em logs/crash.log.",
            "Erro Crítico - MacroKids",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
            
        // Gracefully shutdown the app
        Current.Shutdown(-1);
    }
}

