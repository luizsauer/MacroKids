using System.Text.Json;
using System.Text.Json.Serialization;
using MacroKids.Core.Models;

namespace MacroKids.Core.Serialization;

/// <summary>
/// Serializes and deserializes <see cref="FlowDocument"/> to/from JSON.
/// Used internally by <see cref="ProjectPackager"/> to produce the project.json inside .mkproject files.
/// </summary>
public static class FlowSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented        = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    /// <summary>Serialize a <see cref="FlowDocument"/> to a JSON string.</summary>
    public static string Serialize(FlowDocument document) =>
        JsonSerializer.Serialize(document, Options);

    /// <summary>Deserialize a JSON string to a <see cref="FlowDocument"/>.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the JSON is invalid or null.</exception>
    public static FlowDocument Deserialize(string json)
    {
        var document = JsonSerializer.Deserialize<FlowDocument>(json, Options);
        return document ?? throw new InvalidOperationException(
            "Failed to deserialize FlowDocument: result was null.");
    }

    /// <summary>Serialize to a <see cref="Stream"/> (UTF-8, no BOM).</summary>
    public static async Task SerializeAsync(FlowDocument document, Stream stream,
        CancellationToken ct = default) =>
        await JsonSerializer.SerializeAsync(stream, document, Options, ct);

    /// <summary>Deserialize from a <see cref="Stream"/>.</summary>
    public static async Task<FlowDocument> DeserializeAsync(Stream stream,
        CancellationToken ct = default)
    {
        var document = await JsonSerializer.DeserializeAsync<FlowDocument>(stream, Options, ct);
        return document ?? throw new InvalidOperationException(
            "Failed to deserialize FlowDocument: result was null.");
    }
}
