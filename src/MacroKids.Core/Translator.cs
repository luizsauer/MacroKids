namespace MacroKids.Core;

/// <summary>
/// Provedor global e estático de tradução para evitar dependências circulares.
/// Configurado pelo aplicativo UI no startup.
/// </summary>
public static class Translator
{
    /// <summary>
    /// Delegate de tradução. Recebe (chave, valorPadrao) e retorna o texto traduzido.
    /// </summary>
    public static Func<string, string, string> Translate { get; set; } = (key, def) => def;

    /// <summary>
    /// Delegate para buscar a lista de variáveis declaradas no Canvas.
    /// </summary>
    public static Func<IEnumerable<string>> GetDeclaredVariables { get; set; } = () => Enumerable.Empty<string>();

    /// <summary>
    /// Evento disparado quando o idioma é alterado no aplicativo.
    /// Os ViewModels do canvas escutam este evento para recarregar seus rótulos traduzidos.
    /// </summary>
    public static event EventHandler? TranslationChanged;

    /// <summary>
    /// Traduz uma chave usando o delegate configurado.
    /// </summary>
    public static string Get(string key, string defaultValue) => Translate(key, defaultValue);

    /// <summary>
    /// Notifica que as traduções foram alteradas.
    /// </summary>
    public static void RaiseTranslationChanged() => TranslationChanged?.Invoke(null, EventArgs.Empty);
}
