using MacroKids.Core.Models;

namespace MacroKids.Core.Interfaces;

/// <summary>
/// Central registry of all node types available in the system.
/// Maps a <see cref="NodeMetadata.TypeId"/> to its descriptor and executor.
/// Built-in nodes and plugin nodes are both registered here at startup.
/// </summary>
public interface INodeRegistry
{
    /// <summary>
    /// Register a node type. Called once per type during application initialization.
    /// </summary>
    void Register(NodeMetadata metadata, INodeExecutor executor);

    /// <summary>
    /// Retrieve the metadata for a given type id.
    /// Returns null if the type is not registered (e.g., missing plugin).
    /// </summary>
    NodeMetadata? GetMetadata(string typeId);

    /// <summary>
    /// Retrieve the executor for a given type id.
    /// Returns null if the type is not registered.
    /// </summary>
    INodeExecutor? GetExecutor(string typeId);

    /// <summary>
    /// Try to get both metadata and executor in a single lookup.
    /// </summary>
    bool TryGet(string typeId, out NodeMetadata? metadata, out INodeExecutor? executor);

    /// <summary>
    /// All registered node types, ordered by category then name.
    /// Used to populate the sidebar palette.
    /// </summary>
    IEnumerable<NodeMetadata> GetAll();

    /// <summary>
    /// All registered node types filtered by category.
    /// </summary>
    IEnumerable<NodeMetadata> GetByCategory(NodeCategory category);

    /// <summary>
    /// Search registered node types by name or description (case-insensitive).
    /// </summary>
    IEnumerable<NodeMetadata> Search(string query);

    /// <summary>Whether a type id is currently registered.</summary>
    bool IsRegistered(string typeId);
}
