using System.IO.Compression;
using MacroKids.Core.Models;

namespace MacroKids.Core.Serialization;

/// <summary>
/// Packs and unpacks <c>.mkproject</c> files.
///
/// Current format is plain UTF-8 JSON for readability.
/// Legacy ZIP-based projects are still supported on load.
/// </summary>
public static class ProjectPackager
{
    /// <summary>
    /// Save a <see cref="FlowDocument"/> to a <c>.mkproject</c> file as readable JSON.
    /// Overwrites the file if it already exists.
    /// </summary>
    public static async Task PackAsync(
        FlowDocument document,
        string outputPath,
        CancellationToken ct = default)
    {
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        await using var fileStream = new FileStream(
            outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await FlowSerializer.SerializeAsync(document, fileStream, ct);
    }

    /// <summary>
    /// Load a <see cref="FlowDocument"/> from a <c>.mkproject</c> file.
    /// </summary>
    public static async Task<FlowDocument> UnpackAsync(
        string inputPath,
        CancellationToken ct = default)
    {
        await using var fileStream = new FileStream(
            inputPath, FileMode.Open, FileAccess.Read, FileShare.Read);

        if (IsZipArchive(fileStream))
        {
            using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, leaveOpen: true);
            var jsonEntry = archive.GetEntry("project.json")
                ?? throw new InvalidDataException("Invalid .mkproject file: missing 'project.json'.");

            await using var entryStream = jsonEntry.Open();
            return await FlowSerializer.DeserializeAsync(entryStream, ct);
        }

        fileStream.Position = 0;
        return await FlowSerializer.DeserializeAsync(fileStream, ct);
    }

    public static bool IsValidProjectFile(string path)
    {
        try
        {
            using var fileStream = new FileStream(
                path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (IsZipArchive(fileStream))
            {
                using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, leaveOpen: true);
                return archive.GetEntry("project.json") is not null;
            }

            fileStream.Position = 0;
            using var reader = new StreamReader(fileStream, System.Text.Encoding.UTF8, true, 1024, leaveOpen: true);
            var json = reader.ReadToEnd();
            return !string.IsNullOrWhiteSpace(json);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsZipArchive(Stream stream)
    {
        if (!stream.CanSeek || stream.Length < 2)
            return false;

        var originalPosition = stream.Position;
        Span<byte> header = stackalloc byte[2];
        stream.ReadExactly(header);
        stream.Position = originalPosition;
        return header[0] == 'P' && header[1] == 'K';
    }
}
