using System.IO.Compression;
using MacroKids.Core.Models;

namespace MacroKids.Core.Serialization;

/// <summary>
/// Packs and unpacks <c>.mkproject</c> files.
///
/// A <c>.mkproject</c> is a ZIP archive (renamed) with this structure:
/// <code>
/// meufluxo.mkproject
/// ├── project.json     ← FlowDocument (UTF-8)
/// ├── preview.png      ← 400×225 thumbnail (optional)
/// └── assets/
///     ├── images/
///     └── sounds/
/// </code>
/// This design is identical to .docx, .unitypackage and .xlsx:
/// self-contained, human-inspectable and extensible.
/// </summary>
public static class ProjectPackager
{
    private const string ProjectJsonEntry = "project.json";
    private const string PreviewEntry     = "preview.png";
    private const string AssetsFolder     = "assets/";

    // ── Pack ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Save a <see cref="FlowDocument"/> to a <c>.mkproject</c> file.
    /// Overwrites the file if it already exists.
    /// </summary>
    public static async Task PackAsync(
        FlowDocument document,
        string outputPath,
        Stream? previewImageStream = null,
        CancellationToken ct = default)
    {
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        await using var fileStream = new FileStream(
            outputPath, FileMode.Create, FileAccess.Write, FileShare.None);

        using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create, leaveOpen: true);

        // Write project.json
        var jsonEntry = archive.CreateEntry(ProjectJsonEntry, CompressionLevel.Optimal);
        await using (var entryStream = jsonEntry.Open())
            await FlowSerializer.SerializeAsync(document, entryStream, ct);

        // Write preview.png if provided
        if (previewImageStream != null)
        {
            var previewEntry = archive.CreateEntry(PreviewEntry, CompressionLevel.Optimal);
            await using var previewOut = previewEntry.Open();
            await previewImageStream.CopyToAsync(previewOut, ct);
        }
    }

    // ── Unpack ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Load a <see cref="FlowDocument"/> from a <c>.mkproject</c> file.
    /// </summary>
    /// <exception cref="InvalidDataException">
    /// Thrown if the file is not a valid .mkproject archive.
    /// </exception>
    public static async Task<FlowDocument> UnpackAsync(
        string inputPath,
        CancellationToken ct = default)
    {
        await using var fileStream = new FileStream(
            inputPath, FileMode.Open, FileAccess.Read, FileShare.Read);

        using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, leaveOpen: true);

        var jsonEntry = archive.GetEntry(ProjectJsonEntry)
            ?? throw new InvalidDataException(
                $"Invalid .mkproject file: missing '{ProjectJsonEntry}'.");

        await using var entryStream = jsonEntry.Open();
        return await FlowSerializer.DeserializeAsync(entryStream, ct);
    }

    /// <summary>
    /// Extract the preview thumbnail from a <c>.mkproject</c> file.
    /// Returns null if no preview is embedded.
    /// </summary>
    public static async Task<byte[]?> ExtractPreviewAsync(
        string inputPath,
        CancellationToken ct = default)
    {
        await using var fileStream = new FileStream(
            inputPath, FileMode.Open, FileAccess.Read, FileShare.Read);

        using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, leaveOpen: true);

        var previewEntry = archive.GetEntry(PreviewEntry);
        if (previewEntry is null)
            return null;

        using var ms = new MemoryStream();
        await using var entryStream = previewEntry.Open();
        await entryStream.CopyToAsync(ms, ct);
        return ms.ToArray();
    }

    // ── Validation ────────────────────────────────────────────────────────────

    /// <summary>
    /// Quick validation: check if a file looks like a valid .mkproject archive
    /// without fully deserializing the document.
    /// </summary>
    public static bool IsValidProjectFile(string path)
    {
        try
        {
            using var fileStream = new FileStream(
                path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read);
            return archive.GetEntry(ProjectJsonEntry) is not null;
        }
        catch
        {
            return false;
        }
    }
}
