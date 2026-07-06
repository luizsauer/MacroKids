using MacroKids.Core.Models;
using MacroKids.Core.Services;

namespace MacroKids.Core.Tests;

public class RecordedActionTimingTests
{
    [Fact]
    public void ShiftDelays_moves_gap_after_hold_to_hold_action()
    {
        var actions = new List<RecordedAction>
        {
            new(ActionType.KeyPress, 0, 5000, 387, "W"),
            new(ActionType.LeftClick, 932, 851, 12_000, ""),
        };

        var shifted = RecordedActionTiming.ShiftDelaysToPrecedingAction(actions);

        Assert.Equal(12_000, shifted[0].DelayMs);
        Assert.Equal(0, shifted[1].DelayMs);
    }

    [Fact]
    public void ShiftDelays_clears_delay_on_last_action()
    {
        var actions = new List<RecordedAction>
        {
            new(ActionType.LeftClick, 1, 2, 100, ""),
            new(ActionType.LeftClick, 3, 4, 250, ""),
        };

        var shifted = RecordedActionTiming.ShiftDelaysToPrecedingAction(actions);

        Assert.Equal(250, shifted[0].DelayMs);
        Assert.Equal(0, shifted[1].DelayMs);
    }
}
