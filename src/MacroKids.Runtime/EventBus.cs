using MacroKids.Core.Interfaces;

namespace MacroKids.Runtime;

/// <summary>
/// Thread-safe publish/subscribe event bus.
/// Subscribers receive events on the thread that calls <see cref="Publish{TEvent}"/>.
/// If you need UI-thread marshalling, wrap the handler with a dispatcher invoke.
/// </summary>
public sealed class EventBus : IEventBus
{
    // Dictionary: event type → list of (handler, type-safe wrapper)
    private readonly Dictionary<Type, List<Delegate>> _handlers = [];
    private readonly object _lock = new();

    // ── Publish ──────────────────────────────────────────────────────────────

    public void Publish<TEvent>(TEvent @event) where TEvent : IExecutionEvent
    {
        ArgumentNullException.ThrowIfNull(@event);

        List<Delegate>? handlers;
        lock (_lock)
        {
            if (!_handlers.TryGetValue(typeof(TEvent), out handlers))
                return;

            // Snapshot to avoid holding the lock while invoking
            handlers = [.. handlers];
        }

        foreach (var handler in handlers)
        {
            if (handler is Action<TEvent> typedHandler)
                typedHandler(@event);
        }
    }

    // ── Subscribe ────────────────────────────────────────────────────────────

    public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        where TEvent : IExecutionEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

        lock (_lock)
        {
            if (!_handlers.TryGetValue(typeof(TEvent), out var list))
            {
                list = [];
                _handlers[typeof(TEvent)] = list;
            }

            list.Add(handler);
        }

        return new Subscription(() =>
        {
            lock (_lock)
            {
                if (_handlers.TryGetValue(typeof(TEvent), out var l))
                    l.Remove(handler);
            }
        });
    }

    // ── Subscription token ────────────────────────────────────────────────────

    private sealed class Subscription : IDisposable
    {
        private readonly Action _unsubscribe;
        private bool _disposed;

        public Subscription(Action unsubscribe) => _unsubscribe = unsubscribe;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _unsubscribe();
        }
    }
}
