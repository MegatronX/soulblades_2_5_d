using System;

[Flags]
public enum BattleVisualEventType
{
    None = 0,
    Persistent = 1 << 0,
    StatusApplied = 1 << 1,
    StatusRemoved = 1 << 2,
    TurnStart = 1 << 3,
    TurnEnd = 1 << 4,
    ActionInitiated = 1 << 5,
    AllyActionInitiated = 1 << 6,
    ActionBroadcast = 1 << 7,
    ActionTargeted = 1 << 8,
    ActionPostExecution = 1 << 9,
    AbilityTriggered = 1 << 10,
}
