using MacroKids.Core.Interfaces;
using MacroKids.Core.Plugins;

namespace MacroKids.Plugins;

/// <summary>
/// Discovers and loads plugin assemblies from a directory.
/// Looks for DLLs that implement <see cref="INodePlugin"/> and registers
/// all their node types into the provided <see cref="INodeRegistry"/>.
/// </summary>
public sealed class PluginLoader
{
    private readonly INodeRegistry _registry;

    public PluginLoader(INodeRegistry registry)
    {
        _registry = registry;
    }

    /// <summary>
    /// Scan a directory for plugin DLLs and register all discovered node types.
    /// Non-plugin DLLs and load failures are silently skipped.
    /// </summary>
    /// <param name="pluginsDirectory">
    /// Absolute path to the plugins directory (created if it doesn't exist).
    /// </param>
    /// <returns>List of successfully loaded plugin manifests.</returns>
    public IReadOnlyList<PluginManifest> LoadAll(string pluginsDirectory)
    {
        if (!Directory.Exists(pluginsDirectory))
        {
            Directory.CreateDirectory(pluginsDirectory);
            return [];
        }

        var loaded = new List<PluginManifest>();

        foreach (var dllPath in Directory.EnumerateFiles(pluginsDirectory, "*.dll"))
        {
            try
            {
                var manifest = LoadPlugin(dllPath);
                if (manifest is not null)
                    loaded.Add(manifest);
            }
            catch
            {
                // Silently skip invalid or incompatible DLLs
            }
        }

        return loaded;
    }

    private PluginManifest? LoadPlugin(string dllPath)
    {
        var assembly = System.Reflection.Assembly.LoadFrom(dllPath);

        var pluginTypes = assembly
            .GetExportedTypes()
            .Where(t => !t.IsAbstract && t.IsClass &&
                        t.IsAssignableTo(typeof(INodePlugin)));

        PluginManifest? lastManifest = null;

        foreach (var type in pluginTypes)
        {
            if (Activator.CreateInstance(type) is not INodePlugin plugin)
                continue;

            foreach (var (metadata, executor) in plugin.GetNodes())
            {
                if (!_registry.IsRegistered(metadata.TypeId))
                    _registry.Register(metadata, executor);
            }

            lastManifest = plugin.Manifest;
        }

        return lastManifest;
    }
}
