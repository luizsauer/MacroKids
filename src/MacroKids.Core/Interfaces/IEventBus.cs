namespace MacroKids.Core.Interfaces;

/// <summary>
/// Lightweight publish-subscribe event bus.
/// Used by the execution engine to broadcast events that the UI, logger,
/// debugger and plugins can all observe independently — without any direct coupling.
///
/// This is the backbone of the debug visualization, undo history and analytics features.
/// </summary>
public interface IEventBus
{
    /// <summary>Publishes an event to all registered subscribers of that event type.</summary>
    void Publish<TEvent>(TEvent @event) where TEvent : IExecutionEvent;

    /// <summary>Subscribes to events of a specific type. Returns a disposable to unsubscribe.</summary>
    IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IExecutionEvent;
}

/// <summary>Marker interface for all execution events.</summary>
public interface IExecutionEvent { }
