using System.IO;
using System.Text.Json;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MacroKids.UI.Services;

/// <summary>
/// Manages reactive interface translations loaded from JSON files.
/// </summary>
public sealed partial class LocalizationManager : ObservableObject
{
    private static readonly Lazy<LocalizationManager> _instance = new(() => new LocalizationManager());
    public static LocalizationManager Instance => _instance.Value;

    private Dictionary<string, string> _translations = [];
    [ObservableProperty] private string _currentCulture = "pt-BR";

    private LocalizationManager()
    {
        LoadCulture(System.Globalization.CultureInfo.CurrentUICulture.Name);
    }

    public void LoadCulture(string cultureName)
    {
        // Fallback checks
        string targetCulture = cultureName.StartsWith("pt", StringComparison.OrdinalIgnoreCase) ? "pt-BR" :
                               cultureName.StartsWith("es", StringComparison.OrdinalIgnoreCase) ? "es" : "en";

        string jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Translations", $"{targetCulture}.json");

        // If running in development and translations folder hasn't copied to output folder yet
        if (!File.Exists(jsonPath))
        {
            jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Translations", $"{targetCulture}.json");
        }

        if (File.Exists(jsonPath))
        {
            try
            {
                string jsonText = File.ReadAllText(jsonPath);
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonText);
                if (dict != null)
                {
                    _translations = dict;
                    CurrentCulture = targetCulture;
                    OnPropertyChanged(nameof(Translations));
                }
            }
            catch
            {
                // Silence translation parsing errors, keeping fallback
            }
        }
    }

    // Indexer enabling direct XAML bindings: {Binding Translations[LogoText], Source={x:Static services:LocalizationManager.Instance}}
    public Dictionary<string, string> Translations => _translations;
}
