using MacroKids.Core.Interfaces;
using MacroKids.Core.Models;

namespace MacroKids.Runtime;

/// <summary>
/// Concrete implementation of <see cref="INodeRegistry"/>.
/// Stores node types in a dictionary keyed by <see cref="NodeMetadata.TypeId"/>.
/// Thread-safe for concurrent reads; registration should only happen at startup.
/// </summary>
public sealed class NodeRegistry : INodeRegistry
{
    private record NodeRegistration(NodeMetadata Metadata, INodeExecutor Executor);

    private readonly Dictionary<string, NodeRegistration> _registrations = [];

    // ── Registration ─────────────────────────────────────────────────────────

    public void Register(NodeMetadata metadata, INodeExecutor executor)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(executor);

        if (_registrations.ContainsKey(metadata.TypeId))
            throw new InvalidOperationException(
                $"Node type '{metadata.TypeId}' is already registered.");

        _registrations[metadata.TypeId] = new NodeRegistration(metadata, executor);
    }

    // ── Lookup ────────────────────────────────────────────────────────────────

    public NodeMetadata? GetMetadata(string typeId) =>
        _registrations.TryGetValue(typeId, out var reg) ? reg.Metadata : null;

    public INodeExecutor? GetExecutor(string typeId) =>
        _registrations.TryGetValue(typeId, out var reg) ? reg.Executor : null;

    public bool TryGet(string typeId, out NodeMetadata? metadata, out INodeExecutor? executor)
    {
        if (_registrations.TryGetValue(typeId, out var reg))
        {
            metadata = reg.Metadata;
            executor = reg.Executor;
            return true;
        }

        metadata = null;
        executor = null;
        return false;
    }

    public bool IsRegistered(string typeId) => _registrations.ContainsKey(typeId);

    // ── Enumeration ──────────────────────────────────────────────────────────

    public IEnumerable<NodeMetadata> GetAll() =>
        _registrations.Values
            .OrderBy(r => r.Metadata.Category)
            .ThenBy(r => r.Metadata.Name)
            .Select(r => r.Metadata);

    public IEnumerable<NodeMetadata> GetByCategory(NodeCategory category) =>
        _registrations.Values
            .Where(r => r.Metadata.Category == category)
            .OrderBy(r => r.Metadata.Name)
            .Select(r => r.Metadata);

    public IEnumerable<NodeMetadata> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return GetAll();

        var q = query.Trim();
        return _registrations.Values
            .Where(r =>
                r.Metadata.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                r.Metadata.Description.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                r.Metadata.TypeId.Contains(q, StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => r.Metadata.Name)
            .Select(r => r.Metadata);
    }
}
