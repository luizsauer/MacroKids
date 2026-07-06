namespace MacroKids.Core.Models;

public enum ActionType
{
    Move,
    LeftClick,
    RightClick,
    KeyPress
}

public record RecordedAction(ActionType Type, int X, int Y, int DelayMs, string KeyName = "");
