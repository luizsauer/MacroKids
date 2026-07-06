using System.Text.Json;

namespace MacroKids.Core.Services;

/// <summary>
/// Helpers to read pin values that may arrive as int, long, double, string, or JsonElement.
/// </summary>
public static class PinValueReader
{
    /// <summary>True when the node has real screen coordinates (not the -1,-1 default).</summary>
    public static bool HasExplicitCoordinates(int x, int y) => x != -1 && y != -1;

    public static int GetInt(
        IReadOnlyDictionary<string, object?> resolvedInputs,
        IReadOnlyDictionary<string, object?> pinValues,
        string key,
        int defaultValue = 0)
    {
        if (TryGetInt(resolvedInputs, key, out int resolved))
            return resolved;

        if (TryGetInt(pinValues, key, out int stored))
            return stored;

        return defaultValue;
    }

    public static bool TryGetInt(IReadOnlyDictionary<string, object?> source, string key, out int value)
    {
        value = default;
        if (!source.TryGetValue(key, out var raw) || raw is null)
            return false;

        return TryConvertToInt(raw, out value);
    }

    public static bool TryConvertToInt(object raw, out int value)
    {
        value = default;

        raw = Unbox(raw);

        switch (raw)
        {
            case int i:
                value = i;
                return true;
            case long l when l >= int.MinValue && l <= int.MaxValue:
                value = (int)l;
                return true;
            case double d:
                value = (int)Math.Round(d);
                return true;
            case float f:
                value = (int)Math.Round(f);
                return true;
            case string s when int.TryParse(s, out int parsed):
                value = parsed;
                return true;
            default:
                return int.TryParse(raw.ToString(), out value);
        }
    }

    public static object? Unbox(object? value)
    {
        if (value is not JsonElement element)
            return value;

        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt32(out int i) => i,
            JsonValueKind.Number when element.TryGetInt64(out long l) => l,
            JsonValueKind.Number when element.TryGetDouble(out double d) => d,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText()
        };
    }
}
