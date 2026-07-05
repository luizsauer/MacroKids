using MacroKids.Core.Interfaces;
using MacroKids.Core.Models;

namespace MacroKids.Core.Plugins;

/// <summary>
/// Metadata that identifies a plugin DLL.
/// Stored alongside the DLL so MacroKids can show it in the plugin manager UI.
/// </summary>
public class PluginManifest
{
    /// <summary>Reverse-domain unique identifier (e.g., "com.dev.discord-nodes").</summary>
    public required string Id { get; init; }

    public required string Name { get; init; }
    public required Version Version { get; init; }
    public required string Author { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? WebsiteUrl { get; init; }
    public string? IconPath { get; init; }

    /// <summary>Minimum MacroKids engine version required by this plugin.</summary>
    public Version MinimumEngineVersion { get; init; } = new(1, 0, 0);
}

/// <summary>
/// Contract that every plugin DLL must implement.
/// MacroKids scans the plugins/ folder, loads matching types via reflection,
/// and calls <see cref="GetNodes"/> to register all provided node types.
/// </summary>
public interface INodePlugin
{
    PluginManifest Manifest { get; }

    /// <summary>
    /// Return all node types this plugin contributes.
    /// Each tuple is (descriptor, executor) — both are registered in <see cref="INodeRegistry"/>.
    /// </summary>
    IEnumerable<(NodeMetadata Metadata, INodeExecutor Executor)> GetNodes();
}
