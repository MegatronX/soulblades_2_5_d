public enum BattleHookEventType
{
    None = 0,

    StatusApplied,
    StatusRemoved,
    TurnStart,
    TurnEnd,

    ActionInitiated,
    AllyActionInitiated,
    ActionBroadcast,
    ActionTargeted,
    ActionPostExecution,

    AbilityTriggered,
}
