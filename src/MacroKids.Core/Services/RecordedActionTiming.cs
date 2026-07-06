using MacroKids.Core.Models;

namespace MacroKids.Core.Services;

public static class RecordedActionTiming
{
    /// <summary>
    /// Moves each recorded delay onto the preceding action so it represents
    /// "wait after this action before the next one starts", matching inline delay pins.
    /// </summary>
    public static List<RecordedAction> ShiftDelaysToPrecedingAction(IReadOnlyList<RecordedAction> actions)
    {
        if (actions.Count == 0)
            return [];

        if (actions.Count == 1)
        {
            var only = actions[0];
            return [new RecordedAction(only.Type, only.X, only.Y, 0, only.KeyName)];
        }

        var result = new List<RecordedAction>(actions.Count);
        for (int i = 0; i < actions.Count - 1; i++)
        {
            var act = actions[i];
            result.Add(new RecordedAction(act.Type, act.X, act.Y, actions[i + 1].DelayMs, act.KeyName));
        }

        var last = actions[^1];
        result.Add(new RecordedAction(last.Type, last.X, last.Y, 0, last.KeyName));
        return result;
    }
}
