using MacroKids.Core.Interfaces;
using MacroKids.Runtime;

namespace MacroKids.Runtime.Tests;

public class EventBusTests
{
    [Fact]
    public void Subscribe_And_Publish_ReceivesEvent()
    {
        var bus = new EventBus();
        TestEvent? received = null;

        using var _ = bus.Subscribe<TestEvent>(e => received = e);

        var expected = new TestEvent("hello");
        bus.Publish(expected);

        Assert.Equal(expected, received);
    }

    [Fact]
    public void Dispose_Subscription_StopsReceivingEvents()
    {
        var bus = new EventBus();
        var count = 0;

        var subscription = bus.Subscribe<TestEvent>(_ => count++);
        bus.Publish(new TestEvent("1"));

        subscription.Dispose();
        bus.Publish(new TestEvent("2"));

        Assert.Equal(1, count); // only the first event was received
    }

    [Fact]
    public void MultipleSubscribers_AllReceiveEvent()
    {
        var bus = new EventBus();
        var received = new List<string>();

        using var s1 = bus.Subscribe<TestEvent>(e => received.Add("handler1: " + e.Message));
        using var s2 = bus.Subscribe<TestEvent>(e => received.Add("handler2: " + e.Message));

        bus.Publish(new TestEvent("ping"));

        Assert.Equal(2, received.Count);
        Assert.Contains("handler1: ping", received);
        Assert.Contains("handler2: ping", received);
    }

    [Fact]
    public void DifferentEventTypes_DoNotCross()
    {
        var bus = new EventBus();
        TestEvent? testReceived = null;
        OtherEvent? otherReceived = null;

        using var s1 = bus.Subscribe<TestEvent>(e => testReceived = e);
        using var s2 = bus.Subscribe<OtherEvent>(e => otherReceived = e);

        bus.Publish(new TestEvent("only-test"));

        Assert.NotNull(testReceived);
        Assert.Null(otherReceived);
    }

    // ── Test event types ──────────────────────────────────────────────────────

    private sealed record TestEvent(string Message) : IExecutionEvent;
    private sealed record OtherEvent(int Value)     : IExecutionEvent;
}
